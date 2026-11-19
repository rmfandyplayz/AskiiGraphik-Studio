// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>The Character Brush's own option: the character it paints (ADR 0053).</summary>
public static class CharacterBrushOptions
{
    /// <summary>The App's resource key for the warning shown while the document can't hold the character.</summary>
    public const string CharacterDoesNotFitWarningKey = "ToolWarningCharacterDoesNotFit";

    /// <summary>The character the brush paints, chosen in the Characters window.</summary>
    public static GlyphToolOption Character { get; } = new("character", "ToolOptionCharacter", new Rune('#'));

    /// <summary>The group holding just the character.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.brush.character", [Character]);

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/>, or null when its character set
    /// holds the character.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(document);

        var glyph = settings.For(Group).Get(Character);
        return document.CharacterSet.Allows(glyph) ? null : CharacterDoesNotFitWarningKey;
    }
}

/// <summary>
/// The Character Brush (ADR 0053), the Brush group's fourth tool: it puts the chosen character in every cell under
/// its tip, hard-edged, in the current colours, as typing gives them. An unset background keeps the cell's own. Its
/// tip is the Brush group's (ADR 0047), and [ and ] change its size. The moves of one stroke coalesce into one undo
/// step, and Esc takes the stroke back. It doesn't paint while the document's character set can't hold the
/// character. Each stroke makes its character the newest of the recent ones. Alt+click (Option+click) picks the
/// character and both colours up from what the canvas shows instead.
/// </summary>
public sealed class CharacterBrushTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.brush.character";

    /// <summary>The brush's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } =
        new(Id, "ToolCharacterBrush", "tool.brush.character", "paint");

    /// <summary>Registers the brush, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new CharacterBrushTool(
        context.Session, context.Colors,
        new BrushTipSource(context.Settings.For(BrushTool.TipOptions), context.CellWidthToHeight),
        context.Settings.For(CharacterBrushOptions.Group), context.Settings.RecentGlyphs))
    {
        OptionGroups = [BrushTool.TipOptions, CharacterBrushOptions.Group],
        WarningFor = CharacterBrushOptions.WarningFor
    };

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly BrushTipSource _tip;
    private readonly ToolOptionValues _options;
    private readonly RecentGlyphs _recent;
    private ToolOverlay _overlay = ToolOverlay.None;
    private Gesture _gesture;
    private FootprintStroke? _stroke;

    // The cell a pick last read.
    private CellPoint _lastPickedCell;

    // What a pick replaces, which Esc gives back.
    private (Rune Glyph, CellColor Foreground, CellColor Background) _beforePick;

    /// <summary>
    /// Creates a brush for <paramref name="session"/> that paints the character <paramref name="options"/> choose
    /// with <paramref name="tip"/>'s footprint and <paramref name="colors"/>. Call <see cref="Deactivate"/> when done
    /// with it, so it stops following the tip's settings.
    /// </summary>
    /// <param name="session">The editing session the brush changes.</param>
    /// <param name="colors">The current colours, which painted cells get.</param>
    /// <param name="tip">The Brush group's tip.</param>
    /// <param name="options">The values of <see cref="CharacterBrushOptions.Group"/>.</param>
    /// <param name="recent">The recent characters, which each stroke's character joins.</param>
    public CharacterBrushTool(EditingSession session, PaintColors colors, BrushTipSource tip,
        ToolOptionValues options, RecentGlyphs recent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(tip);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(recent);
        if (options.Group != CharacterBrushOptions.Group)
            throw new ArgumentException("These aren't the Character Brush's own options.", nameof(options));

        _session = session;
        _colors = colors;
        _tip = tip;
        _options = options;
        _recent = recent;
        _tip.Values.Changed += OnTipChanged;
    }

    private enum Gesture
    {
        None,
        Painting,
        Picking
    }

    /// <summary>Raised when the tip's size or shape changes, so the canvas outlines the new one.</summary>
    public event EventHandler? OverlayChanged;

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging paints.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the brush takes no text, apart from its keys.</summary>
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
    /// With Alt, picks the character and both colours up from the cell. Otherwise starts a stroke, unless the active
    /// layer is locked or hidden, or the document's character set can't hold the character. A stroke may start
    /// outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (input.Modifiers.HasFlag(ToolModifiers.Alt))
        {
            StartPicking(input.Cell);
            return;
        }

        var document = _session.Document;
        var glyph = _options.Get(CharacterBrushOptions.Character);
        if (!_session.CanEditActiveLayer || !document.CharacterSet.Allows(glyph))
            return;

        // The stroke keeps the character and colours it starts with.
        var foreground = _colors.ForegroundIn(document.ColorMode);
        var background = _colors.BackgroundIn(document.ColorMode);
        _gesture = Gesture.Painting;
        _recent.Use(glyph);
        _stroke = new FootprintStroke(_session, _tip.Footprint,
            cell => Cell.FromGlyph(glyph, foreground, background.IsUnset ? cell.Background : background),
            UndoStepNames.Paint, input);
    }

    /// <summary>
    /// Continues the stroke in a straight line, because a fast drag reports cells far apart. While picking, picks
    /// from each cell the pointer reaches.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_gesture == Gesture.Picking && input.Cell != _lastPickedCell)
        {
            Pick(input.Cell);
        }
        else if (_gesture == Gesture.Painting)
        {
            _stroke?.MoveTo(input);
        }
    }

    /// <inheritdoc/>
    public void PointerReleased(ToolPointerInput input) => EndGesture();

    /// <summary>
    /// Esc during a stroke takes it back as if it never happened, and Esc while picking gives back the character and
    /// colours from before. Otherwise Esc isn't the brush's, so the App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _gesture == Gesture.None)
            return false;

        if (_gesture == Gesture.Painting)
        {
            _stroke?.Cancel();
        }
        else if (_gesture == Gesture.Picking)
        {
            _options.Set(CharacterBrushOptions.Character, _beforePick.Glyph);
            _colors.Set(_beforePick.Foreground, _beforePick.Background);
        }

        EndGesture();
        return true;
    }

    /// <summary>
    /// [ makes the tip a cell smaller and ] a cell bigger. They're typed text rather than keys, so they work with any
    /// keyboard layout that types them.
    /// </summary>
    public bool TextEntered(string text) => BrushTipOptions.TryUseSizeKey(_tip.Values, text);

    /// <summary>Keeps what the stroke has painted or what was picked so far, and stops following the tip.</summary>
    public void Deactivate()
    {
        EndGesture();
        _tip.Values.Changed -= OnTipChanged;
    }

    private void OnTipChanged(object? sender, EventArgs e) => OverlayChanged?.Invoke(this, EventArgs.Empty);

    private void EndGesture()
    {
        var hadStroke = _stroke is not null;
        _gesture = Gesture.None;
        _stroke = null;

        // A size changed during the stroke shows only now.
        if (hadStroke && _overlay.Footprint != _tip.Footprint)
            OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    // Picking works on any layer, locked or hidden, because it takes what the canvas shows.
    private void StartPicking(CellPoint point)
    {
        _gesture = Gesture.Picking;
        _beforePick = (_options.Get(CharacterBrushOptions.Character), _colors.Foreground, _colors.Background);
        Pick(point);
    }

    // A cell with no character to paint, such as an empty one or a space, gives nothing, not even its colours, so a
    // stray pick doesn't lose them. A monochrome document shows no colours, so it gives only the character.
    private void Pick(CellPoint point)
    {
        _lastPickedCell = point;
        var document = _session.Document;
        if (!document.CanvasSize.Contains(point))
            return;

        var shown = Compositor.CompositeCell(document.ActiveFrame, point);
        if (!shown.HasGlyph || !GlyphToolOption.Allows(shown.Glyph))
            return;

        _options.Set(CharacterBrushOptions.Character, shown.Glyph);
        if (document.ColorMode != ColorMode.Monochrome)
            _colors.Set(shown.Foreground, shown.Background);
    }
}
