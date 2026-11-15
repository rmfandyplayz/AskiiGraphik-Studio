// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The colour brush (ADR 0043). It's an FG brush or a BG brush, as <see cref="PaintColors.Target"/> says, and keys
/// 1 and 2 choose which. Dragging paints the chosen colour on the cells under the pointer on the active layer, one
/// cell wide: the FG brush colours the text of cells that have a glyph, and the BG brush colours the background.
/// A colour of none takes the colour away. The moves of one stroke coalesce into one undo step, as the eraser's do.
/// Alt+click (Option+click) picks the chosen colour up from what the canvas shows instead.
/// </summary>
public sealed class BrushTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.brush";

    /// <summary>The key that chooses the FG brush.</summary>
    public const string ForegroundBrushKey = "1";

    /// <summary>The key that chooses the BG brush.</summary>
    public const string BackgroundBrushKey = "2";

    /// <summary>The brush's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolBrush", "tool.brush", "paint", 'B');

    /// <summary>Registers the brush, as DI does.</summary>
    public static ToolDefinition Definition { get; } =
        new(ToolDescriptor, context => new BrushTool(context.Session, context.Colors));

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private Gesture _gesture;
    private CellPoint _lastCell;

    // A stroke's settings, taken when it starts, so the stroke paints one colour throughout.
    private CoalescingGroup? _stroke;
    private PaintColorTarget _strokeTarget;
    private CellColor _strokeColor;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes
    // back the newest step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    // The colour a pick replaces, which Esc gives back.
    private CellColor _colorBeforePick;

    /// <summary>Creates a brush for <paramref name="session"/> that paints with <paramref name="colors"/>.</summary>
    public BrushTool(EditingSession session, PaintColors colors)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);

        _session = session;
        _colors = colors;
    }

    private enum Gesture
    {
        None,
        Painting,
        Picking
    }

    /// <summary>The brush never draws anything over the document, so this never happens.</summary>
    public event EventHandler? OverlayChanged
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: dragging paints.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the brush takes no text, apart from its two keys.</summary>
    public bool IsTakingText => false;

    /// <inheritdoc/>
    public ToolOverlay Overlay => ToolOverlay.None;

    /// <summary>
    /// With Alt, picks the chosen colour up from the cell. Otherwise starts a stroke, unless the active layer is
    /// locked or hidden, or the document is monochrome and so holds no colours. A stroke may start outside the canvas.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        _lastCell = input.Cell;
        if (input.Modifiers.HasFlag(ToolModifiers.Alt))
        {
            StartPicking(input.Cell);
            return;
        }

        var colorMode = _session.Document.ColorMode;
        if (!_session.CanEditActiveLayer || colorMode == ColorMode.Monochrome)
            return;

        _gesture = Gesture.Painting;
        _stroke = new CoalescingGroup();
        _strokeTarget = _colors.Target;
        _strokeColor = _colors.TargetColorIn(colorMode);
        _undoCountAfterLastEdit = null;
        Paint(input.Cell, input.Cell);
    }

    /// <summary>
    /// Continues the stroke in a straight line, because a fast drag reports cells far apart. While picking, picks
    /// from each cell the pointer reaches.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        if (_gesture == Gesture.None || input.Cell == _lastCell)
            return;

        if (_gesture == Gesture.Painting)
            Paint(_lastCell, input.Cell);
        else
            Pick(input.Cell);

        _lastCell = input.Cell;
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

        if (_gesture == Gesture.Painting && _undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
        else if (_gesture == Gesture.Picking)
            _colors.TargetColor = _colorBeforePick;

        EndGesture();
        return true;
    }

    /// <summary>
    /// Key 1 chooses the FG brush and key 2 the BG brush. They're typed text rather than keys, so they work with
    /// any keyboard layout that types the digits.
    /// </summary>
    public bool TextEntered(string text)
    {
        switch (text)
        {
            case ForegroundBrushKey:
                _colors.Target = PaintColorTarget.Foreground;
                return true;
            case BackgroundBrushKey:
                _colors.Target = PaintColorTarget.Background;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Keeps what the stroke has painted, or the colour picked, so far.</summary>
    public void Deactivate() => EndGesture();

    private void EndGesture()
    {
        _gesture = Gesture.None;
        _stroke = null;
    }

    // Picking works on any layer, locked or hidden, because it takes what the canvas shows. A monochrome document
    // shows no colours, and picking there would only lose the one chosen, so it does nothing.
    private void StartPicking(CellPoint point)
    {
        if (_session.Document.ColorMode == ColorMode.Monochrome)
            return;

        _gesture = Gesture.Picking;
        _colorBeforePick = _colors.TargetColor;
        Pick(point);
    }

    private void Pick(CellPoint point)
    {
        var document = _session.Document;
        if (!document.CanvasSize.Contains(point))
            return;

        // A cell with no glyph shows no text colour, so it gives none, as does one in the canvas's own ink.
        var shown = Compositor.CompositeCell(document.ActiveFrame, point);
        _colors.TargetColor = _colors.Target == PaintColorTarget.Foreground ? shown.Foreground : shown.Background;
    }

    private void Paint(CellPoint from, CellPoint to)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = new CellEditBuilder(document, layer.Id);
        foreach (var point in CellLine.Between(from, to))
        {
            if (!document.CanvasSize.Contains(point))
                continue;

            var cell = layer.GetCell(point);
            if (_strokeTarget == PaintColorTarget.Background)
                cells.Set(point, cell.WithBackground(_strokeColor));
            else if (cell.HasGlyph)
                cells.Set(point, cell.WithForeground(_strokeColor));
        }

        if (cells.Build(UndoStepNames.Paint) is not { } command)
            return;

        _session.History.Execute(command, _stroke);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }
}
