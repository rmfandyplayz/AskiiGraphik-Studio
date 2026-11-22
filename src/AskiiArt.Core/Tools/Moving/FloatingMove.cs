// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// One move of the Move tool (ADR 0066): the selected cells of the active layer, or the whole layer when nothing is
/// selected, taken from where they were when the move began. The selection moves with them, so moving the whole layer
/// selects it, as Select All would, as part of the step (ADR 0069). The document always shows the move. Each new place takes
/// back the step the last one made and makes a new one from the cells as they were before the move, so the cells pass
/// over other art without harming it until the move ends, and the whole move is one undo step. A paste floats the same
/// way, with cells it brings rather than ones it takes, and makes its step at once (ADR 0067).
/// </summary>
internal sealed class FloatingMove
{
    private readonly EditingSession _session;

    // The cells that move, where they were, which the selection becomes wherever they go: the whole canvas when
    // nothing was selected. A paste's shape is the clip's, so it's placed at the paste's corner, onto the canvas.
    private readonly SelectionMask _mask;
    private readonly CellPoint _maskCorner;
    private readonly bool _isPaste;

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
        _mask = session.Selection.IsEmpty ? SelectionMask.All(session.Document.CanvasSize) : session.Selection.Mask;
        _cells = TakeCells(session.ActiveLayer, _mask);
        _historyAsLeft = HistoryNow();
    }

    private FloatingMove(EditingSession session, CellClip clip, CellPoint topLeft)
    {
        _session = session;
        _isPaste = true;
        IsCopy = true;
        LayerId = session.ActiveLayer.Id;
        var fitted = clip.FittedTo(session.Document.CharacterSet, session.Document.ColorMode);
        _mask = fitted.Shape;
        _maskCorner = topLeft;
        _cells = BringCells(fitted, topLeft);
        _historyAsLeft = HistoryNow();
    }

    /// <summary>
    /// Pastes <paramref name="clip"/> with its top-left corner at <paramref name="topLeft"/>: its cells, fitted to
    /// the document, float on the active layer with its shape selected, as one Paste step from the start. Cells off
    /// the canvas are kept while the paste floats, as a move's are.
    /// </summary>
    public static FloatingMove Paste(EditingSession session, CellClip clip, CellPoint topLeft)
    {
        var paste = new FloatingMove(session, clip, topLeft);
        paste.Change(paste.ShowStep);
        return paste;
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
            if (offset != default || _isPaste)
                ShowStep();
        });
    }

    /// <summary>Puts the cells back where they began, leaving no step. A paste goes altogether.</summary>
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

    private void ShowStep()
    {
        var step = BuildStep(Offset);
        _session.History.Execute(step);
        _step = step;
    }

    private void TakeBackStep()
    {
        if (_step is null)
            return;

        _session.History.CancelNewestStep();
        _step = null;
    }

    private CompositeCommand BuildStep(CellPoint offset)
    {
        var name = _isPaste ? UndoStepNames.Paste : UndoStepNames.Move;
        List<IDocumentCommand> commands = [];
        if (_cells.Length > 0)
            commands.Add(new MoveCellsCommand(LayerId, _cells, Landings(offset), IsCopy, name));

        var corner = new CellPoint(_maskCorner.X + offset.X, _maskCorner.Y + offset.Y);
        var selection = _mask.MovedOnto(_session.Document.CanvasSize, corner);
        commands.Add(new SetSelectionCommand(_session.Selection, selection, name));
        return new CompositeCommand(commands, name);
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
}
