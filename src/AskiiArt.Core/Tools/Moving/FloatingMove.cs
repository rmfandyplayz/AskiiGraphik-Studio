// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// One move of the Move tool (ADR 0066): the selected cells of the active layer, or the whole layer when nothing is
/// selected, taken from where they were when the move began. The document always shows the move. Each new place takes
/// back the step the last one made and makes a new one from the cells as they were before the move, so the cells pass
/// over other art without harming it until the move ends, and the whole move is one undo step.
/// </summary>
internal sealed class FloatingMove
{
    private readonly EditingSession _session;

    // The cells that move, where they were; the selection moves with them unless nothing was selected.
    private readonly SelectionMask _mask;
    private readonly bool _movesSelection;

    // The cells with something in them, where they were. Empty ones don't cover what they land on.
    private readonly MovingCell[] _cells;

    // The step showing the move, or null while the cells are where they began.
    private IDocumentCommand? _step;
    private (int UndoCount, int StepCount) _historyAsLeft;

    /// <summary>
    /// Takes the selected cells of <paramref name="session"/>'s active layer, or all of its cells when nothing is
    /// selected. A copy leaves the cells where they were as well.
    /// </summary>
    public FloatingMove(EditingSession session, bool isCopy)
    {
        _session = session;
        IsCopy = isCopy;
        LayerId = session.ActiveLayer.Id;
        _movesSelection = !session.Selection.IsEmpty;
        _mask = _movesSelection ? session.Selection.Mask : SelectionMask.All(session.Document.CanvasSize);
        _cells = TakeCells(session.ActiveLayer, _mask);
        _historyAsLeft = HistoryNow();
    }

    /// <summary>The layer whose cells move.</summary>
    public Guid LayerId { get; }

    /// <summary>True when the cells are also left where they were.</summary>
    public bool IsCopy { get; }

    /// <summary>How far the cells are from where they began, in cells.</summary>
    public CellPoint Offset { get; private set; }

    /// <summary>True when the move has made a step: the cells are somewhere other than where they began.</summary>
    public bool HasStep => _step is not null;

    /// <summary>True while the move is changing the history itself.</summary>
    public bool IsChanging { get; private set; }

    /// <summary>
    /// True when the history is as the move left it, so nothing else has changed the document or the selection since.
    /// A save doesn't count.
    /// </summary>
    public bool IsHistoryAsLeft => HistoryNow() == _historyAsLeft;

    /// <summary>
    /// Shows the cells <paramref name="offset"/> from where they began. Cells that go off the canvas aren't written,
    /// but come back if a later offset brings them back on.
    /// </summary>
    public void MoveTo(CellPoint offset)
    {
        if (offset == Offset)
            return;

        Change(() =>
        {
            TakeBackStep();
            Offset = offset;
            if (offset == default || BuildStep(offset) is not { } step)
                return;

            _session.History.Execute(step);
            _step = step;
        });
    }

    /// <summary>Puts the cells back where they began, leaving no step.</summary>
    public void Cancel()
    {
        Change(TakeBackStep);
        Offset = default;
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
            _historyAsLeft = HistoryNow();
        }
    }

    private void TakeBackStep()
    {
        if (_step is null)
            return;

        _session.History.CancelNewestStep();
        _step = null;
    }

    private CompositeCommand? BuildStep(CellPoint offset)
    {
        List<IDocumentCommand> commands = [];
        if (_cells.Length > 0)
            commands.Add(new MoveCellsCommand(LayerId, _cells, Landings(offset), IsCopy));

        if (_movesSelection)
            commands.Add(new SetSelectionCommand(_session.Selection, _mask.Moved(offset), UndoStepNames.Move));

        return commands.Count == 0 ? null : new CompositeCommand(commands, UndoStepNames.Move);
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

    private (int UndoCount, int StepCount) HistoryNow() => (_session.History.UndoCount, _session.History.StepCount);

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
}
