// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// One floating move of the Move tool (ADRs 0066, 0070): the selected cells of the active layer, or the whole layer when
/// nothing is selected, taken from where they were when the move began. Each gesture, such as a drag or a run of arrow
/// presses, is a Move step of its own, from where the gesture found the cells to where it leaves them. The cells float:
/// each step puts back what lay under them before they go on, so while the move lasts, also across undos and redos of
/// its own steps, they pass over other art without harming it. The selection moves with them, so moving the whole layer
/// selects it (ADR 0069), and it can go past the canvas's edges into the zone around it (ADR 0071). A paste floats the
/// same way, with cells it brings, from a Paste step of its own (ADR 0067).
/// </summary>
internal sealed class FloatingMove
{
    private readonly EditingSession _session;
    private readonly bool _isPaste;

    // The canvas and the zone around it, where the selection can go (ADR 0071).
    private readonly CellRect _zone;

    // The cells that move, where they were, which the selection becomes wherever they go: the whole canvas when
    // nothing was selected. A paste's shape is the clip's, so it's placed at the paste's corner.
    private readonly SelectionMask _mask;
    private readonly CellPoint _maskCorner;

    // The cells with something in them, where they were. Empty ones don't cover what they land on, and the
    // selection's cells past the canvas's edges hold nothing.
    private readonly MovingCell[] _cells;

    // Where the cells are, and what lies under them there, after each of the move's steps, oldest first. A move's first
    // is where they began, after no step of its own; a paste's is after its Paste step. Undos and redos go back and
    // forth among them.
    private readonly List<FloatState> _states = [];
    private int _stateIndex;

    // The step of the gesture under way, once it has moved the cells, and what it leaves under them.
    private IDocumentCommand? _gestureStep;
    private Cell[] _gestureCovered = [];

    /// <summary>
    /// Takes the selected cells of <paramref name="session"/>'s active layer, or all of its cells when nothing is
    /// selected. A copy leaves the cells where they were as well. The selection can go anywhere in
    /// <paramref name="zone"/>.
    /// </summary>
    public FloatingMove(EditingSession session, bool isCopy, CellRect zone)
    {
        _session = session;
        _zone = zone;
        LayerId = session.ActiveLayer.Id;
        _mask = session.Selection.IsEmpty ? SelectionMask.All(session.Document.CanvasSize) : session.Selection.Mask;
        _cells = TakeCells(session.ActiveLayer, _mask.CutToCanvas());

        // A cell that moves leaves an empty one where it was; under a copied cell lies the cell itself.
        var under = new Cell[_cells.Length];
        if (isCopy)
        {
            for (var index = 0; index < _cells.Length; index++)
                under[index] = _cells[index].Cell;
        }

        _states.Add(new FloatState(session.History.NewestStep, default, under));
    }

    private FloatingMove(EditingSession session, CellClip clip, CellPoint topLeft, CellRect zone)
    {
        _session = session;
        _isPaste = true;
        _zone = zone;
        LayerId = session.ActiveLayer.Id;
        var fitted = clip.FittedTo(session.Document.CharacterSet, session.Document.ColorMode);
        _mask = fitted.Shape;
        _maskCorner = topLeft;
        _cells = BringCells(fitted, topLeft);
    }

    /// <summary>
    /// Pastes <paramref name="clip"/> with its top-left corner at <paramref name="topLeft"/>: its cells, fitted to
    /// the document, float on the active layer with its shape selected, as far as <paramref name="zone"/> reaches, as a
    /// Paste step. Cells off the canvas are kept while the paste floats, as a move's are.
    /// </summary>
    public static FloatingMove Paste(EditingSession session, CellClip clip, CellPoint topLeft, CellRect zone)
    {
        var paste = new FloatingMove(session, clip, topLeft, zone);
        paste.Change(() =>
        {
            var (step, covered) = paste.BuildStep(null, default, UndoStepNames.Paste);
            session.History.Execute(step);
            paste._states.Add(new FloatState(step, default, covered));
        });
        return paste;
    }

    /// <summary>The layer whose cells move.</summary>
    public Guid LayerId { get; }

    /// <summary>How far the cells are from where they began, in cells.</summary>
    public CellPoint Offset { get; private set; }

    /// <summary>
    /// True when the document shows a step of the move's own: the cells are somewhere other than where they began, or
    /// they're a paste.
    /// </summary>
    public bool HasStep => _isPaste || _stateIndex > 0 || _gestureStep is not null;

    /// <summary>True while a gesture is under way, whose step <see cref="MoveTo"/> changes.</summary>
    public bool IsInGesture { get; private set; }

    /// <summary>True while the move is changing the history itself.</summary>
    public bool IsChanging { get; private set; }

    private FloatState Current => _states[_stateIndex];

    /// <summary>
    /// <paramref name="offset"/>, kept to where the selection stays in the zone (ADR 0071): each side of the cells'
    /// shape stops at the zone's outer edge, so a drag past it slides along it. A shape too big for the zone can only
    /// go as far as it still covers it. Where the gesture began is always within reach, so nothing jumps.
    /// </summary>
    public CellPoint Limit(CellPoint offset)
    {
        var shape = _mask.Bounds.Offset(_maskCorner);
        if (shape.IsEmpty)
            return offset;

        var start = Current.Offset;
        return new CellPoint(
            LimitAlong(offset.X, start.X, _zone.Left - shape.Left, _zone.Right - shape.Right),
            LimitAlong(offset.Y, start.Y, _zone.Top - shape.Top, _zone.Bottom - shape.Bottom));
    }

    /// <summary>Starts a gesture from where the cells are. Its moves make one step.</summary>
    public void BeginGesture()
    {
        EndGesture();
        IsInGesture = true;
    }

    /// <summary>
    /// During a gesture, shows the cells <paramref name="offset"/> from where they began, as the gesture's step. Cells
    /// that go off the canvas aren't written, but come back if a later offset brings them back on. Back where the
    /// gesture began, it has no step.
    /// </summary>
    public void MoveTo(CellPoint offset)
    {
        if (!IsInGesture || offset == Offset)
            return;

        Change(() =>
        {
            TakeBackGestureStep();
            Offset = offset;
            if (offset == Current.Offset)
                return;

            var (step, covered) = BuildStep(Current, offset, UndoStepNames.Move);
            _session.History.Execute(step);
            _gestureStep = step;
            _gestureCovered = covered;
        });
    }

    /// <summary>Ends the gesture under way, keeping its step.</summary>
    public void EndGesture()
    {
        IsInGesture = false;
        if (_gestureStep is null)
            return;

        // Steps undone and then replaced by this one can't come back.
        _states.RemoveRange(_stateIndex + 1, _states.Count - _stateIndex - 1);
        _states.Add(new FloatState(_gestureStep, Offset, _gestureCovered));
        _stateIndex = _states.Count - 1;
        _gestureStep = null;
        _gestureCovered = [];
    }

    /// <summary>Ends the gesture under way, putting the cells back where it found them, and leaving no step.</summary>
    public void CancelGesture()
    {
        IsInGesture = false;
        Change(TakeBackGestureStep);
        Offset = Current.Offset;
    }

    /// <summary>
    /// After a change to the history that the move didn't make, carries the move on where the history has put its
    /// cells, as after an undo or a redo of one of its own steps. A gesture under way ends, keeping its step, unless
    /// the change left its step as it was, as a save does.
    /// </summary>
    /// <returns>
    /// False when the history is at none of the move's steps, so the move is over: something else changed the document
    /// or the selection, or the step before the move began was undone.
    /// </returns>
    public bool FollowHistory()
    {
        var newest = _session.History.NewestStep;
        if (ReferenceEquals(newest, _gestureStep ?? Current.Step))
            return true;

        EndGesture();
        var index = _states.FindLastIndex(state => ReferenceEquals(state.Step, newest));
        if (index < 0)
            return false;

        _stateIndex = index;
        Offset = Current.Offset;
        return true;
    }

    private void Change(Action change)
    {
        IsChanging = true;
        try
        {
            change();
        }
        finally
        {
            IsChanging = false;
        }
    }

    private void TakeBackGestureStep()
    {
        if (_gestureStep is null)
            return;

        _session.History.CancelNewestStep();
        _gestureStep = null;
        _gestureCovered = [];
    }

    // A step that takes the cells from where they are in from, or from off the layer, to offset, with the selection.
    private (CompositeCommand Step, Cell[] Covered) BuildStep(FloatState? from, CellPoint offset, string name)
    {
        List<IDocumentCommand> commands = [];
        Cell[] covered = [];
        if (_cells.Length > 0)
        {
            var liftedFrom = from is null ? null : Landings(from.Offset);
            var cells = new MoveCellsCommand(LayerId, _cells, liftedFrom, from?.Under, Landings(offset), name);
            commands.Add(cells);
            covered = cells.Covered;
        }

        var corner = new CellPoint(_maskCorner.X + offset.X, _maskCorner.Y + offset.Y);
        var selection = _mask.MovedWithin(_session.Document.CanvasSize, corner, _zone);
        commands.Add(new SetSelectionCommand(_session.Selection, selection, name));
        return (new CompositeCommand(commands, name), covered);
    }

    // Where each cell lands, as a cell index, or -1 off the canvas.
    private int[] Landings(CellPoint offset)
    {
        var canvas = _session.Document.CanvasSize;
        var landings = new int[_cells.Length];
        for (var index = 0; index < _cells.Length; index++)
        {
            var point = _cells[index].Point;
            var landing = new CellPoint(point.X + offset.X, point.Y + offset.Y);
            landings[index] = canvas.Contains(landing) ? (landing.Y * canvas.Width) + landing.X : -1;
        }

        return landings;
    }

    // The offset along one axis kept between the two that put the shape's sides at the zone's, and never further from
    // where the gesture began than it already is.
    private static int LimitAlong(int offset, int start, int toFirstSide, int toSecondSide)
    {
        var lowest = Math.Min(Math.Min(toFirstSide, toSecondSide), start);
        var highest = Math.Max(Math.Max(toFirstSide, toSecondSide), start);
        return Math.Clamp(offset, lowest, highest);
    }

    private static MovingCell[] TakeCells(Layer layer, SelectionMask mask)
    {
        List<MovingCell> cells = [];
        for (var row = mask.Bounds.Top; row < mask.Bounds.Bottom; row++)
        {
            foreach (var (column, length) in mask.RunsInRow(row))
            {
                for (var x = column; x < column + length; x++)
                {
                    var point = new CellPoint(x, row);
                    var index = layer.IndexOf(point);
                    var cell = layer.GetCell(index);
                    if (!cell.IsEmpty)
                        cells.Add(new MovingCell(index, point, cell));
                }
            }
        }

        return [.. cells];
    }

    // The clip's cells with something in them, where they land at the paste's corner. They come from no cell of the
    // layer, so they have no index.
    private static MovingCell[] BringCells(CellClip clip, CellPoint topLeft)
    {
        List<MovingCell> cells = [];
        for (var row = clip.Shape.Bounds.Top; row < clip.Shape.Bounds.Bottom; row++)
        {
            foreach (var (column, length) in clip.Shape.RunsInRow(row))
            {
                for (var x = column; x < column + length; x++)
                {
                    var cell = clip.GetCell(new CellPoint(x, row));
                    var landing = new CellPoint(topLeft.X + x, topLeft.Y + row);
                    if (!cell.IsEmpty)
                        cells.Add(new MovingCell(MovingCell.NoIndex, landing, cell));
                }
            }
        }

        return [.. cells];
    }

    // Where the cells are after a step, and what lies under each of them there. The step is the history's newest
    // applied one while the document is in this state: one of the move's own, or the one before the move began.
    private sealed record FloatState(IDocumentCommand? Step, CellPoint Offset, Cell[] Under);
}
