// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tools.Eraser;

/// <summary>
/// The Soft Eraser (ARCHITECTURE §8.3, ADR 0050), which lightens text along the ramp it shares with the Brush. Each
/// glyph under the tip moves lighter by the strength times the tip's weight there, once per stroke
/// (<see cref="RampStroke"/>), and one lightened to nothing goes. A glyph keeps its own colours, and backgrounds stay.
/// It doesn't erase while the ramp has characters the document's character set can't hold. The tip is the Eraser
/// group's (ADR 0047), and [ and ] change its size. The moves of one stroke coalesce into one undo step.
/// </summary>
public sealed class SoftEraserTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.eraser.soft";

    /// <summary>The Soft Eraser's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolSoftEraser", "tool.eraser.soft", "paint");

    /// <summary>Registers the Soft Eraser, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new SoftEraserTool(
        context.Session, new BrushTipSource(context.Settings.For(EraserTool.OptionGroup), context.CellWidthToHeight),
        context.Settings.For(RampOptions.Group), context.Settings.For(SoftEraserOptions.Group), context.Densities))
    {
        OptionGroups = [EraserTool.OptionGroup, RampOptions.Group, SoftEraserOptions.Group],
        WarningFor = RampOptions.WarningFor
    };

    private readonly EditingSession _session;
    private readonly BrushTipSource _tip;
    private readonly ToolOptionValues _ramp;
    private readonly ToolOptionValues _options;
    private readonly Func<IGlyphDensityProvider> _densities;
    private ToolOverlay _overlay = ToolOverlay.None;
    private RampStroke? _stroke;

    /// <summary>
    /// Creates a soft eraser for <paramref name="session"/> that erases with <paramref name="tip"/>'s footprint, along
    /// the ramp <paramref name="ramp"/> chooses, as strongly as <paramref name="options"/> say. Call
    /// <see cref="Deactivate"/> when done with it, so it stops following the tip's settings.
    /// </summary>
    /// <param name="session">The editing session the eraser changes.</param>
    /// <param name="tip">The Eraser group's tip.</param>
    /// <param name="ramp">The values of <see cref="RampOptions.Group"/>.</param>
    /// <param name="options">The values of <see cref="SoftEraserOptions.Group"/>.</param>
    /// <param name="densities">How dark glyphs look in the document's font, now.</param>
    public SoftEraserTool(EditingSession session, BrushTipSource tip, ToolOptionValues ramp, ToolOptionValues options,
        Func<IGlyphDensityProvider> densities)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(tip);
        ArgumentNullException.ThrowIfNull(ramp);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(densities);
        if (ramp.Group != RampOptions.Group)
            throw new ArgumentException("These aren't the ramp's options.", nameof(ramp));

        if (options.Group != SoftEraserOptions.Group)
            throw new ArgumentException("These aren't the Soft Eraser's own options.", nameof(options));

        _session = session;
        _tip = tip;
        _ramp = ramp;
        _options = options;
        _densities = densities;
        _tip.Values.Changed += OnTipChanged;
    }

    /// <summary>Raised when the tip's size or shape changes, so the canvas outlines the new one.</summary>
    public event EventHandler? OverlayChanged;

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging erases.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the eraser takes no text, apart from its keys.</summary>
    public bool IsTakingText => false;

    /// <summary>The tip's footprint, for the canvas to outline under the pointer.</summary>
    public ToolOverlay Overlay
    {
        get
        {
            var footprint = _stroke?.Footprint ?? _tip.Footprint;
            if (_overlay.Footprint != footprint)
                _overlay = new ToolOverlay(Caret: null, footprint);

            return _overlay;
        }
    }

    /// <summary>
    /// Starts a stroke, unless the active layer is locked or hidden, or the document's character set can't hold the
    /// ramp. It may start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (!_session.CanEditActiveLayer || !RampOptions.FitsIn(_ramp, _session.Document))
            return;

        // The stroke keeps the settings it starts with, so it erases along one ramp with one tip and one strength.
        var ramp = RampOptions.ReadRamp(_ramp).Ramp;
        _stroke = new RampStroke(_session, ramp,
            BrushFalloff.Create(_tip.Footprint, _options.Get(SoftEraserOptions.Softness) / 100.0),
            target: 0, strength: _options.Get(SoftEraserOptions.Strength) / 100.0, _densities,
            (before, levelBefore, level) => Lighten(ramp, before, levelBefore, level),
            UndoStepNames.Erase, input);
    }

    /// <summary>Continues the stroke in a straight line, because a fast drag reports cells far apart.</summary>
    public void PointerMoved(ToolPointerInput input) => _stroke?.MoveTo(input);

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input) => EndStroke();

    /// <summary>
    /// Esc during a stroke takes it back as if it never happened. Otherwise Esc isn't the eraser's, so the App
    /// switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _stroke is null)
            return false;

        _stroke.Cancel();
        EndStroke();
        return true;
    }

    /// <summary>[ makes the tip a cell smaller, and ] a cell bigger.</summary>
    public bool TextEntered(string text) => BrushTipOptions.TryUseSizeKey(_tip.Values, text);

    /// <summary>Keeps what the stroke has erased so far, and stops following the tip's settings.</summary>
    public void Deactivate()
    {
        EndStroke();
        _tip.Values.Changed -= OnTipChanged;
    }

    // A glyph it doesn't move a whole level stays, even one the ramp doesn't hold. One it moves keeps its colours,
    // and one it empties keeps the cell's background.
    private static Cell Lighten(Ramp ramp, Cell before, int levelBefore, int level)
    {
        if (level == levelBefore)
            return before;

        if (ramp.GlyphOf(level) is not { } glyph)
            return before.WithoutGlyph();

        return Cell.FromGlyph(glyph, before.Foreground, before.Background);
    }

    private void OnTipChanged(object? sender, EventArgs e) => OverlayChanged?.Invoke(this, EventArgs.Empty);

    private void EndStroke()
    {
        var hadStroke = _stroke is not null;
        _stroke = null;

        // A size changed during the stroke shows only now.
        if (hadStroke && _overlay.Footprint != _tip.Footprint)
            OverlayChanged?.Invoke(this, EventArgs.Empty);
    }
}
