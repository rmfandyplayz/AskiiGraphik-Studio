// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The FG Brush and the BG Brush (ADR 0043, ADR 0049): two tools of the Brush group, each painting one of the current
/// colours on the cells under its tip on the active layer. The FG Brush colours the text of cells that have a glyph,
/// and the BG Brush colours the background. A colour of none takes the colour away. Their tip is the Brush group's
/// (ADR 0047), and [ and ] change its size. The moves of one stroke coalesce into one undo step, as the eraser's do.
/// Alt+click (Option+click) picks the brush's colour up from what the canvas shows instead.
/// </summary>
public sealed class ColorBrushTool : ITool
{
    /// <summary>The FG Brush's id.</summary>
    public const string ForegroundId = "askiiart.brush.foreground";

    /// <summary>The BG Brush's id.</summary>
    public const string BackgroundId = "askiiart.brush.background";

    /// <summary>The FG Brush's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ForegroundDescriptor { get; } =
        new(ForegroundId, "ToolForegroundBrush", "tool.brush.foreground", "paint");

    /// <summary>The BG Brush's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor BackgroundDescriptor { get; } =
        new(BackgroundId, "ToolBackgroundBrush", "tool.brush.background", "paint");

    /// <summary>Registers the FG Brush, as DI does.</summary>
    public static ToolDefinition ForegroundDefinition { get; } = CreateDefinition(PaintColorTarget.Foreground);

    /// <summary>Registers the BG Brush, as DI does.</summary>
    public static ToolDefinition BackgroundDefinition { get; } = CreateDefinition(PaintColorTarget.Background);

    private readonly PaintColorTarget _target;
    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly BrushTipSource _tip;
    private ToolOverlay _overlay = ToolOverlay.None;
    private Gesture _gesture;

    // The cell a pick last read.
    private CellPoint _lastPickedCell;

    // The stroke keeps the colour and tip it starts with.
    private FootprintStroke? _stroke;

    // The colour a pick replaces, which Esc gives back.
    private CellColor _colorBeforePick;

    /// <summary>
    /// Creates a brush for <paramref name="session"/> that paints the <paramref name="target"/> colour of
    /// <paramref name="colors"/> with <paramref name="tip"/>'s footprint. Call <see cref="Deactivate"/> when done with
    /// it, so it stops following the tip's settings.
    /// </summary>
    public ColorBrushTool(PaintColorTarget target, EditingSession session, PaintColors colors, BrushTipSource tip)
    {
        if (!Enum.IsDefined(target))
            throw new ArgumentOutOfRangeException(nameof(target), target, null);

        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(tip);

        _target = target;
        _session = session;
        _colors = colors;
        _tip = tip;
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
    public ToolDescriptor Descriptor =>
        _target == PaintColorTarget.Foreground ? ForegroundDescriptor : BackgroundDescriptor;

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
    /// With Alt, picks the brush's colour up from the cell. Otherwise starts a stroke, unless the active layer is
    /// locked or hidden, or the document is monochrome and so holds no colours. A stroke may start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        if (input.Modifiers.HasFlag(ToolModifiers.Alt))
        {
            StartPicking(input.Cell);
            return;
        }

        var colorMode = _session.Document.ColorMode;
        if (!_session.CanEditActiveLayer || colorMode == ColorMode.Monochrome)
            return;

        _gesture = Gesture.Painting;
        var color = _target == PaintColorTarget.Foreground
            ? _colors.ForegroundIn(colorMode)
            : _colors.BackgroundIn(colorMode);
        _stroke = new FootprintStroke(_session, _tip.Footprint, cell => Paint(cell, color), UndoStepNames.Paint,
            input);
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
    /// Esc during a stroke takes it back as if it never happened, and Esc while picking gives back the colour from
    /// before. Otherwise Esc isn't the brush's, so the App switches to the Hand tool (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (key != ToolKey.Escape || _gesture == Gesture.None)
            return false;

        if (_gesture == Gesture.Painting)
            _stroke?.Cancel();
        else if (_gesture == Gesture.Picking)
            SetColor(_colorBeforePick);

        EndGesture();
        return true;
    }

    /// <summary>
    /// [ makes the tip a cell smaller and ] a cell bigger. They're typed text rather than keys, so they work with any
    /// keyboard layout that types them.
    /// </summary>
    public bool TextEntered(string text) => BrushTipOptions.TryUseSizeKey(_tip.Values, text);

    /// <summary>Keeps what the stroke has painted, or the colour picked, so far, and stops following the tip.</summary>
    public void Deactivate()
    {
        EndGesture();
        _tip.Values.Changed -= OnTipChanged;
    }

    private static ToolDefinition CreateDefinition(PaintColorTarget target)
    {
        var descriptor = target == PaintColorTarget.Foreground ? ForegroundDescriptor : BackgroundDescriptor;
        return new ToolDefinition(descriptor, context => new ColorBrushTool(target, context.Session, context.Colors,
            new BrushTipSource(context.Settings.For(BrushTool.TipOptions), context.CellWidthToHeight)))
        {
            OptionGroups = [BrushTool.TipOptions]
        };
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

    // Picking works on any layer, locked or hidden, because it takes what the canvas shows. A monochrome document
    // shows no colours, and picking there would only lose the one chosen, so it does nothing.
    private void StartPicking(CellPoint point)
    {
        if (_session.Document.ColorMode == ColorMode.Monochrome)
            return;

        _gesture = Gesture.Picking;
        _colorBeforePick = _target == PaintColorTarget.Foreground ? _colors.Foreground : _colors.Background;
        Pick(point);
    }

    private void Pick(CellPoint point)
    {
        _lastPickedCell = point;
        var document = _session.Document;
        if (!document.CanvasSize.Contains(point))
            return;

        // A cell with no glyph shows no text colour, so it gives none, as does one in the canvas's own ink.
        var shown = Compositor.CompositeCell(document.ActiveFrame, point);
        SetColor(_target == PaintColorTarget.Foreground ? shown.Foreground : shown.Background);
    }

    private void SetColor(CellColor color)
    {
        if (_target == PaintColorTarget.Foreground)
            _colors.Foreground = color;
        else
            _colors.Background = color;
    }

    private Cell Paint(Cell cell, CellColor color)
    {
        if (_target == PaintColorTarget.Background)
            return cell.WithBackground(color);

        return cell.HasGlyph ? cell.WithForeground(color) : cell;
    }
}
