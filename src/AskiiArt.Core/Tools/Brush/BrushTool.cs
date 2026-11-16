// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The colour brush (ADR 0043). It's an FG brush or a BG brush, as <see cref="PaintColors.Target"/> says, and keys
/// 1 and 2 choose which. Dragging paints the chosen colour on the cells under its tip on the active layer: the FG
/// brush colours the text of cells that have a glyph, and the BG brush colours the background. A colour of none
/// takes the colour away. The tip's size and shape are the Brush group's options (ADR 0047), and [ and ] change its
/// size. The moves of one stroke coalesce into one undo step, as the eraser's do. Alt+click (Option+click) picks the
/// chosen colour up from what the canvas shows instead.
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

    /// <summary>The Brush group's options: the tip's size, shape and proportions.</summary>
    public static ToolOptionGroup OptionGroup { get; } = BrushTipOptions.CreateGroup(Id);

    /// <summary>Registers the brush, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new BrushTool(context.Session,
        context.Colors, new BrushTipSource(context.Settings.For(OptionGroup), context.CellWidthToHeight)))
    {
        Options = OptionGroup
    };

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly BrushTipSource _tip;
    private ToolOverlay _overlay = ToolOverlay.None;
    private Gesture _gesture;

    // The cell a pick last read.
    private CellPoint _lastPickedCell;

    // A stroke's settings, taken when it starts, so the stroke paints one colour with one tip throughout.
    private CoalescingGroup? _stroke;
    private PaintColorTarget _strokeTarget;
    private CellColor _strokeColor;
    private BrushFootprint? _strokeFootprint;
    private CellPoint _lastTopLeft;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes
    // back the newest step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    // The colour a pick replaces, which Esc gives back.
    private CellColor _colorBeforePick;

    /// <summary>
    /// Creates a brush for <paramref name="session"/> that paints with <paramref name="colors"/> and
    /// <paramref name="tip"/>'s footprint. Call <see cref="Deactivate"/> when done with it, so it stops following the
    /// tip's settings.
    /// </summary>
    public BrushTool(EditingSession session, PaintColors colors, BrushTipSource tip)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(tip);

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
            var footprint = _strokeFootprint ?? _tip.Footprint;
            if (_overlay.Footprint != footprint)
                _overlay = new ToolOverlay(Caret: null, footprint);

            return _overlay;
        }
    }

    /// <summary>
    /// With Alt, picks the chosen colour up from the cell. Otherwise starts a stroke, unless the active layer is
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
        _stroke = new CoalescingGroup();
        _strokeTarget = _colors.Target;
        _strokeColor = _colors.TargetColorIn(colorMode);
        _strokeFootprint = _tip.Footprint;
        _undoCountAfterLastEdit = null;
        _lastTopLeft = _strokeFootprint.TopLeftAt(input);
        Paint(_lastTopLeft, _lastTopLeft);
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
        else if (_gesture == Gesture.Painting && _strokeFootprint is not null)
        {
            var topLeft = _strokeFootprint.TopLeftAt(input);
            if (topLeft == _lastTopLeft)
                return;

            Paint(_lastTopLeft, topLeft);
            _lastTopLeft = topLeft;
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

        if (_gesture == Gesture.Painting && _undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
        else if (_gesture == Gesture.Picking)
            _colors.TargetColor = _colorBeforePick;

        EndGesture();
        return true;
    }

    /// <summary>
    /// Key 1 chooses the FG brush and key 2 the BG brush, and [ makes the tip a cell smaller and ] a cell bigger.
    /// They're typed text rather than keys, so they work with any keyboard layout that types them.
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
                return BrushTipOptions.TryUseSizeKey(_tip.Values, text);
        }
    }

    /// <summary>Keeps what the stroke has painted, or the colour picked, so far, and stops following the tip.</summary>
    public void Deactivate()
    {
        EndGesture();
        _tip.Values.Changed -= OnTipChanged;
    }

    private void OnTipChanged(object? sender, EventArgs e) => OverlayChanged?.Invoke(this, EventArgs.Empty);

    private void EndGesture()
    {
        var hadStroke = _strokeFootprint is not null;
        _gesture = Gesture.None;
        _stroke = null;
        _strokeFootprint = null;

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
        _colorBeforePick = _colors.TargetColor;
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
        _colors.TargetColor = _colors.Target == PaintColorTarget.Foreground ? shown.Foreground : shown.Background;
    }

    private void Paint(CellPoint fromTopLeft, CellPoint toTopLeft)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer || _strokeFootprint is null)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = new CellEditBuilder(document, layer.Id);
        foreach (var point in FootprintSweep.Cover(_strokeFootprint, fromTopLeft, toTopLeft, document.CanvasSize))
        {
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
