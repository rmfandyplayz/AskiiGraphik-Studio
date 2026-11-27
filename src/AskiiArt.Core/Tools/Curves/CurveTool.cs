// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Shapes;

namespace AskiiArt.Core.Tools.Curves;

/// <summary>
/// The Curve tool (ADR 0083), the Shapes group's second, which works like a pen tool. Each click places an anchor in
/// the middle of a cell, and the curve runs smoothly through them. Dragging as an anchor is placed pulls out its
/// handles, each the other's mirror image; Alt+click places a corner. Shift keeps a new anchor in line with the last,
/// as the Line tool's constraints do, and a dragged handle to 45° steps. Until the curve is finished, an anchor can be
/// dragged to move it and a handle to reshape the curve, Alt unlinking it from the other. The curve shows in the
/// document as it's built, with a piece following the pointer to where the next anchor would go, and its anchors and
/// handles show over it. Enter or a double-click finishes it, and a click on the first anchor closes it into a loop.
/// Backspace or Undo takes the last anchor away, and Esc throws the whole curve away; switching tools keeps it. A
/// finished curve is one undo step. It draws in the shared Style, follows Include joins and clips to the selection, as
/// the Line tool does, with the options and colours as they are while it's drawn.
/// </summary>
public sealed class CurveTool : ITool
{
    /// <summary>The tool's id.</summary>
    public const string Id = "askiiart.shapes.curve";

    // A curve needs this many anchors to draw anything.
    private const int FewestAnchorsOfACurve = 2;

    private static readonly CurveHandleSide[] _sides = [CurveHandleSide.In, CurveHandleSide.Out];

    private readonly EditingSession _session;
    private readonly PaintColors _colors;
    private readonly ToolOptionValues _style;
    private readonly ToolOptionValues _joins;
    private readonly ToolOptionValues _character;
    private readonly RecentGlyphs _recent;
    private readonly Func<double> _cellWidthToHeight;
    private readonly Func<IGlyphDensityProvider> _glyphs;
    private readonly List<CurveAnchor> _anchors = [];
    private LiveShape? _shape;
    private Drawing? _drawn;
    private Press? _press;
    private int _selected;
    private CanvasPoint _pointer;
    private CellPoint _pointerCell;
    private ToolModifiers _modifiers;
    private ToolMarkerReach _reach;
    private ToolOverlay _overlay = ToolOverlay.None;

    /// <summary>Creates the tool for <paramref name="context"/>'s session.</summary>
    public CurveTool(ToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _session = context.Session;
        _colors = context.Colors;
        _style = context.Settings.For(LineStyleOptions.Group);
        _joins = context.Settings.For(LineStyleOptions.JoinsGroup);
        _character = context.Settings.For(CharacterBrushOptions.Group);
        _recent = context.Settings.RecentGlyphs;
        _cellWidthToHeight = context.CellWidthToHeight;
        _glyphs = context.Densities;
    }

    /// <summary>Raised when the anchors, the handles, or which of them are strong change.</summary>
    public event EventHandler? OverlayChanged;

    /// <summary>The tool's identity. The name key is the App's resource key.</summary>
    public static ToolDescriptor ToolDescriptor { get; } = new(Id, "ToolCurve", "tool.shapes.curve", "shapes");

    /// <summary>Registers the tool, as DI does.</summary>
    public static ToolDefinition Definition { get; } = new(ToolDescriptor, context => new CurveTool(context))
    {
        OptionGroups = [LineStyleOptions.Group, LineStyleOptions.JoinsGroup, CharacterBrushOptions.Group],
        WarningFor = LineStyleOptions.WarningFor,
        CharacterChosen = LineStyleOptions.ChooseSingleCharacter
    };

    /// <inheritdoc/>
    public ToolDescriptor Descriptor => ToolDescriptor;

    /// <summary>True: Space + drag pans, between clicks too.</summary>
    public bool AllowsSpacePanning => true;

    /// <summary>False: clicking places anchors.</summary>
    public bool PansWithPrimaryButton => false;

    /// <summary>False: the tool takes no text.</summary>
    public bool IsTakingText => false;

    /// <summary>True while a curve is being drawn, so Enter, Backspace and Esc are the tool's.</summary>
    public bool IsDrawing => _anchors.Count > 0;

    /// <summary>True while a curve is being drawn, when Undo takes the last anchor away, as Backspace does.</summary>
    public bool TakesUndo => IsDrawing;

    /// <summary>The anchors and their handles, while a curve is being drawn.</summary>
    public ToolOverlay Overlay => _overlay;

    /// <summary>
    /// Starts a curve with an anchor in the pressed cell, unless the active layer is locked or hidden, or the style is
    /// Single character and the document can't hold the character. During a curve, the second click of a
    /// double-click finishes it; a press on an anchor or a handle takes hold of it, and anywhere else places the next
    /// anchor.
    /// </summary>
    public void PointerPressed(ToolPointerInput input)
    {
        Follow(input);

        // Something else changed the history since the curve was drawn, such as a click in the History window, so
        // the curve ends where it is.
        if (_shape is { IsInterrupted: true })
            Clear();

        if (!IsDrawing)
        {
            Start();
            return;
        }

        if (input.ClickCount >= 2)
        {
            Finish(isClosed: false);
            return;
        }

        var aim = AimAtPointer();
        var target = aim.Target;
        if (target.Kind == TargetKind.Handle)
        {
            // A smooth anchor keeps the handles it shows, should the drag make it mirrored.
            _anchors[target.Index] = aim.Shown[target.Index];
        }
        else if (target.Kind == TargetKind.NewAnchor)
        {
            _anchors.Add(NewAnchorAt(target.Position));
            target = target with { Index = _anchors.Count - 1 };
        }

        _selected = target.Index;
        _press = new Press(target, _pointer);
        Redraw();
    }

    /// <summary>
    /// Pulls out the handles of an anchor being placed, moves an anchor or a handle being dragged, or moves the piece
    /// of curve that shows where the next anchor would go.
    /// </summary>
    public void PointerMoved(ToolPointerInput input)
    {
        Follow(input);
        if (!IsDrawing)
            return;

        if (_press is { } press)
            FollowPress(press);

        Redraw();
    }

    /// <summary>Lets go of what the press took hold of. A click on the first anchor closes the curve.</summary>
    public void PointerReleased(ToolPointerInput input)
    {
        Follow(input);
        if (_press is not { } press || !IsDrawing)
            return;

        FollowPress(press);
        _press = null;
        if (press.Target is { Kind: TargetKind.Anchor, Index: 0 } && !press.HasMoved &&
            _anchors.Count >= FewestAnchorsOfACurve)
        {
            Finish(isClosed: true);
            return;
        }

        Redraw();
    }

    /// <summary>Follows Shift and Alt at once while a curve is drawn, though the pointer hasn't moved.</summary>
    public void ModifiersChanged(ToolModifiers modifiers)
    {
        if (modifiers == _modifiers)
            return;

        _modifiers = modifiers;
        if (!IsDrawing)
            return;

        if (_press is { } press)
            FollowPress(press);

        Redraw();
    }

    /// <summary>
    /// While a curve is drawn, Enter finishes it, Backspace takes the last anchor away (the first, too, which throws
    /// the curve away) and Esc throws it away. Otherwise none of them is the tool's, so Esc switches to the Hand tool
    /// (ADR 0041).
    /// </summary>
    public bool KeyPressed(ToolKey key, ToolModifiers modifiers)
    {
        if (!IsDrawing)
            return false;

        switch (key)
        {
            case ToolKey.Enter:
                Finish(isClosed: false);
                return true;
            case ToolKey.Backspace:
                RemoveLastAnchor();
                return true;
            case ToolKey.Escape:
                Cancel();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Takes no text.</summary>
    public bool TextEntered(string text) => false;

    /// <summary>Takes the last anchor away, as Backspace does.</summary>
    public void Undo()
    {
        if (IsDrawing)
            RemoveLastAnchor();
    }

    /// <summary>Finishes the curve as it is, so switching tools keeps it.</summary>
    public void Deactivate() => Finish(isClosed: false);

    private void Follow(ToolPointerInput input)
    {
        _pointer = input.Position;
        _pointerCell = input.Cell;
        _modifiers = input.Modifiers;
        _reach = input.MarkerReach;
    }

    private void Start()
    {
        var document = _session.Document;
        var glyph = _character.Get(CharacterBrushOptions.Character);
        if (!_session.CanEditActiveLayer)
            return;

        if (LineStyleOptions.ReadStyle(_style) == LineStyle.SingleCharacter && !document.CharacterSet.Allows(glyph))
            return;

        _shape = new LiveShape(_session, UndoStepNames.Curve);
        _anchors.Add(NewAnchorAt(MiddleOf(_pointerCell)));
        _selected = 0;
        _press = new Press(new Target(TargetKind.NewAnchor, 0), _pointer);
        Redraw();
    }

    private CurveAnchor NewAnchorAt(CanvasPoint position) =>
        IsAltHeld ? CurveAnchor.Corner(position) : CurveAnchor.Smooth(position);

    private bool IsAltHeld => _modifiers.HasFlag(ToolModifiers.Alt);

    // An anchor being placed gets its handles once the pointer leaves the marker's reach of where the press was, which
    // Shift may have kept off the anchor. A handle moves as far as the pointer has, so it doesn't jump to where the
    // press was, and an anchor goes to the middle of the pointer's cell.
    private void FollowPress(Press press)
    {
        var index = press.Target.Index;
        var anchor = _anchors[index];
        switch (press.Target.Kind)
        {
            case TargetKind.NewAnchor:
                if (!press.HasMoved && _reach.Covers(press.PressedAt, _pointer))
                    return;

                press.HasMoved = true;
                _anchors[index] = anchor.WithHandle(CurveHandleSide.Out, HandleEnd(anchor.Position, _pointer),
                    unlinks: IsAltHeld);
                return;
            case TargetKind.Handle:
                if (!press.HasMoved && _pointer == press.PressedAt)
                    return;

                press.HasMoved = true;
                var pressedEnd = press.Target.Position;
                var end = new CanvasPoint(pressedEnd.X + _pointer.X - press.PressedAt.X,
                    pressedEnd.Y + _pointer.Y - press.PressedAt.Y);
                _anchors[index] = anchor.WithHandle(press.Target.Side, HandleEnd(anchor.Position, end),
                    unlinks: IsAltHeld);
                return;
            default:
                if (_pointerCell == anchor.Position.Cell)
                    return;

                press.HasMoved = true;
                _anchors[index] = anchor.MovedTo(MiddleOf(_pointerCell));
                return;
        }
    }

    // With Shift held, a handle keeps to the nearest of the directions 45° apart, as they look on screen.
    private CanvasPoint HandleEnd(CanvasPoint anchor, CanvasPoint end)
    {
        if (!_modifiers.HasFlag(ToolModifiers.Shift))
            return end;

        var cellWidthToHeight = _cellWidthToHeight();
        var across = (end.X - anchor.X) * cellWidthToHeight;
        var down = end.Y - anchor.Y;
        var length = Math.Sqrt(across * across + down * down);
        if (length == 0)
            return end;

        const double step = Math.PI / 4;
        var angle = Math.Round(Math.Atan2(down, across) / step) * step;
        return new CanvasPoint(anchor.X + length * Math.Cos(angle) / cellWidthToHeight,
            anchor.Y + length * Math.Sin(angle));
    }

    private void RemoveLastAnchor()
    {
        _press = null;
        _anchors.RemoveAt(_anchors.Count - 1);
        if (!IsDrawing)
        {
            Cancel();
            return;
        }

        _selected = Math.Min(_selected, _anchors.Count - 1);
        Redraw();
    }

    private void Finish(bool isClosed)
    {
        if (_shape is null || _shape.IsInterrupted)
        {
            Clear();
            return;
        }

        if (_anchors.Count < FewestAnchorsOfACurve)
        {
            Cancel();
            return;
        }

        Draw(_anchors, isClosed);
        if (LineStyleOptions.ReadStyle(_style) == LineStyle.SingleCharacter)
            _recent.Use(_character.Get(CharacterBrushOptions.Character));

        Clear();
    }

    private void Cancel()
    {
        _shape?.TakeBack();
        Clear();
    }

    // Ends the curve, leaving what it drew in the document.
    private void Clear()
    {
        _anchors.Clear();
        _shape = null;
        _drawn = null;
        _press = null;
        SetOverlay(ToolOverlay.None);
    }

    // Draws the curve with the piece to where the next anchor would go, unless something else changed the history
    // since it was drawn, which ends it where it is.
    private void Redraw()
    {
        if (_shape is null)
            return;

        if (_shape.IsInterrupted)
        {
            Clear();
            return;
        }

        var aim = AimAtPointer();
        if (aim.Next is { } next)
            Draw([.. _anchors, next], isClosed: false);
        else
            Draw(_anchors, isClosed: false);

        SetOverlay(OverlayOf(aim));
    }

    // Takes back what the curve drew last and draws it through these anchors, unless nothing would change. A curve
    // of one anchor draws nothing, and nor does one the layer or the document can't take now.
    private void Draw(List<CurveAnchor> anchors, bool isClosed)
    {
        var document = _session.Document;
        var style = LineStyleOptions.ReadStyle(_style);
        var glyph = _character.Get(CharacterBrushOptions.Character);
        var drawing = new Drawing([.. anchors], isClosed, style, LineStyleOptions.ReadJoins(_joins, style), glyph,
            _colors.ForegroundIn(document.ColorMode), _colors.BackgroundIn(document.ColorMode));
        if (drawing.IsSameAs(_drawn))
            return;

        _shape!.TakeBack();
        _drawn = drawing;
        if (anchors.Count < FewestAnchorsOfACurve || !_session.CanEditActiveLayer)
            return;

        if (style == LineStyle.SingleCharacter && !document.CharacterSet.Allows(glyph))
            return;

        var picker = new LineGlyphPicker(_cellWidthToHeight(), _glyphs(), document.CharacterSet);
        var path = CurveShape.PathOf(CurveShape.Resolve(anchors, isClosed), isClosed);
        var cells = _shape.Collect(drawing.Foreground, drawing.Background);
        cells.PutLine(picker.Pick(path, style, glyph), path.IsClosed, drawing.Joins);
        _shape.Show(cells);
    }

    // What a press where the pointer is would take hold of, and the anchors as they show: with the next anchor where a
    // click would place it, unless the pointer is on an anchor or a press is under way. An anchor's marker comes
    // before a handle's, which comes before the rest of an anchor's cell.
    private Aim AimAtPointer()
    {
        if (_press is { } press)
            return new Aim(press.Target, CurveShape.Resolve(_anchors, isClosed: false), Next: null);

        if (NearestWithin(_anchors.Select(anchor => anchor.Position)) is { } onMarker)
        {
            return new Aim(new Target(TargetKind.Anchor, onMarker), CurveShape.Resolve(_anchors, isClosed: false),
                Next: null);
        }

        var nextPosition = MiddleOf(NextAnchorCell());
        var inCell = _anchors.FindIndex(anchor => anchor.Position == nextPosition);
        CurveAnchor? next = inCell < 0 ? NewAnchorAt(nextPosition) : null;
        var shown = CurveShape.Resolve(next is { } shownNext ? [.. _anchors, shownNext] : _anchors, isClosed: false);
        var handles = HandlesShown(shown);
        if (NearestWithin(handles.Select(handle => handle.End)) is { } onHandle)
        {
            var (index, side, end) = handles[onHandle];
            return new Aim(new Target(TargetKind.Handle, index, side, end), shown, next);
        }

        if (inCell >= 0)
            return new Aim(new Target(TargetKind.Anchor, inCell), shown, Next: null);

        return new Aim(new Target(TargetKind.NewAnchor, _anchors.Count, Position: nextPosition), shown, next);
    }

    // The pointer's cell, kept in line with the last anchor by Shift and Ctrl as the Line tool's end is.
    private CellPoint NextAnchorCell()
    {
        var last = _anchors[^1].Position.Cell;
        var distance = new CellPoint(_pointerCell.X - last.X, _pointerCell.Y - last.Y);
        var kept = MoveConstraint.Apply(distance, _modifiers, _cellWidthToHeight());
        return new CellPoint(last.X + kept.X, last.Y + kept.Y);
    }

    // The index of the point whose marker the pointer is on, the nearest where several are, or null.
    private int? NearestWithin(IEnumerable<CanvasPoint> points)
    {
        int? nearest = null;
        var nearestDistance = double.MaxValue;
        var index = 0;
        foreach (var point in points)
        {
            if (_reach.Covers(point, _pointer))
            {
                var distance = (point.X - _pointer.X) * (point.X - _pointer.X) / (_reach.Across * _reach.Across) +
                               (point.Y - _pointer.Y) * (point.Y - _pointer.Y) / (_reach.Down * _reach.Down);
                if (distance < nearestDistance)
                {
                    nearest = index;
                    nearestDistance = distance;
                }
            }

            index++;
        }

        return nearest;
    }

    // Each placed anchor's handles that shape the curve. A handle at its anchor shapes nothing, and nor does a smooth
    // anchor's handle past the end of the curve.
    private List<(int Index, CurveHandleSide Side, CanvasPoint End)> HandlesShown(CurveAnchor[] shown)
    {
        var handles = new List<(int, CurveHandleSide, CanvasPoint)>();
        for (var index = 0; index < _anchors.Count; index++)
        {
            var anchor = shown[index];
            foreach (var side in _sides)
            {
                var end = anchor.HandleOn(side);
                var isPastTheEnd = side == CurveHandleSide.In ? index == 0 : index == shown.Length - 1;
                if (end == anchor.Position || (anchor.Kind == CurveAnchorKind.Smooth && isPastTheEnd))
                    continue;

                handles.Add((index, side, end));
            }
        }

        return handles;
    }

    // A marker on each anchor, the first one's showing that a click there closes the curve once there's one to close,
    // and the handles, strong for the selected anchor and for the one the pointer is on.
    private ToolOverlay OverlayOf(Aim aim)
    {
        var pointedAt = aim.Target.Kind == TargetKind.NewAnchor ? -1 : aim.Target.Index;
        var markers = new ToolMarker[_anchors.Count];
        for (var index = 0; index < markers.Length; index++)
            markers[index] = new ToolMarker(_anchors[index].Position, ToolMarkerKind.Corner);

        if (_anchors.Count >= FewestAnchorsOfACurve)
        {
            var isClosing = _press is null && aim.Target is { Kind: TargetKind.Anchor, Index: 0 };
            markers[0] = markers[0] with
            {
                Kind = isClosing ? ToolMarkerKind.FinishUnderPointer : ToolMarkerKind.Finish
            };
        }

        var handles = new List<ToolHandle>();
        foreach (var (index, _, end) in HandlesShown(aim.Shown))
            handles.Add(new ToolHandle(aim.Shown[index].Position, end, index == _selected || index == pointedAt));

        return new ToolOverlay(Caret: null, Markers: markers, Handles: handles);
    }

    private void SetOverlay(ToolOverlay overlay)
    {
        if (overlay == _overlay)
            return;

        _overlay = overlay;
        OverlayChanged?.Invoke(this, EventArgs.Empty);
    }

    private static CanvasPoint MiddleOf(CellPoint cell) => new(cell.X + 0.5, cell.Y + 0.5);

    private enum TargetKind
    {
        // Empty canvas, where a press places the next anchor.
        NewAnchor,
        Anchor,
        Handle
    }

    // What a press takes hold of: the anchor at Index, or its handle on Side. Position is where a new anchor goes, or
    // where the handle ends.
    private readonly record struct Target(
        TargetKind Kind,
        int Index,
        CurveHandleSide Side = CurveHandleSide.Out,
        CanvasPoint Position = default);

    // What a press would take hold of, and the anchors as they show, with their handles. Next is the anchor a click
    // would place, which shows as the piece of curve to it.
    private sealed record Aim(Target Target, CurveAnchor[] Shown, CurveAnchor? Next);

    // A press under way: what it holds, where the pointer was then, and whether it has moved it yet.
    private sealed class Press(Target target, CanvasPoint pressedAt)
    {
        public Target Target { get; } = target;

        public CanvasPoint PressedAt { get; } = pressedAt;

        public bool HasMoved { get; set; }
    }

    // What the document shows of the curve now, so a pointer move that changes none of it draws nothing new.
    private sealed record Drawing(
        CurveAnchor[] Anchors,
        bool IsClosed,
        LineStyle Style,
        bool Joins,
        Rune Character,
        CellColor Foreground,
        CellColor Background)
    {
        public bool IsSameAs(Drawing? other) =>
            other is not null && Anchors.AsSpan().SequenceEqual(other.Anchors) && IsClosed == other.IsClosed &&
            Style == other.Style && Joins == other.Joins && Character == other.Character &&
            Foreground == other.Foreground && Background == other.Background;
    }
}
