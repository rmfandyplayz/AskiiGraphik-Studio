// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// One stroke of a tool that changes every cell its tip covers in the same way, whatever the tip's weight there, such
/// as the FG Brush or the Character Brush. The tip moves in straight lines between the cells the pointer reports, so a
/// fast drag leaves no gaps. Its edits show as it goes and coalesce into one undo step, which Esc can take back.
/// </summary>
internal sealed class FootprintStroke
{
    private readonly EditingSession _session;
    private readonly Func<Cell, Cell> _paint;
    private readonly string _stepNameKey;
    private readonly CoalescingGroup _undo = new();
    private CellPoint _lastTopLeft;

    // Where the history stood after this stroke's last edit, or null before its first. Esc only takes back the newest
    // step if it's still this stroke's.
    private int? _undoCountAfterLastEdit;

    /// <summary>Starts a stroke at <paramref name="start"/>, and paints the cells under the tip there.</summary>
    /// <param name="session">The session whose active layer the stroke changes.</param>
    /// <param name="footprint">The tip, which the stroke keeps throughout.</param>
    /// <param name="paint">Gives each cell the tip covers. Returning the cell as it is leaves it.</param>
    /// <param name="stepNameKey">Names the undo step: a key from <see cref="UndoStepNames"/>.</param>
    /// <param name="start">Where the pointer was pressed.</param>
    public FootprintStroke(EditingSession session, BrushFootprint footprint, Func<Cell, Cell> paint,
        string stepNameKey, ToolPointerInput start)
    {
        _session = session;
        Footprint = footprint;
        _paint = paint;
        _stepNameKey = stepNameKey;

        _lastTopLeft = footprint.TopLeftAt(start);
        Paint(_lastTopLeft, _lastTopLeft);
    }

    /// <summary>The cells the tip covers.</summary>
    public BrushFootprint Footprint { get; }

    /// <summary>Continues the stroke in a straight line, because a fast drag reports cells far apart.</summary>
    public void MoveTo(ToolPointerInput input)
    {
        var topLeft = Footprint.TopLeftAt(input);
        if (topLeft == _lastTopLeft)
            return;

        Paint(_lastTopLeft, topLeft);
        _lastTopLeft = topLeft;
    }

    /// <summary>Takes the stroke back as if it never happened, unless another step has followed it.</summary>
    public void Cancel()
    {
        if (_undoCountAfterLastEdit == _session.History.UndoCount)
            _session.History.CancelNewestStep();
    }

    private void Paint(CellPoint fromTopLeft, CellPoint toTopLeft)
    {
        // Undoing during a stroke can take away the layer it started on, or lock it.
        if (!_session.CanEditActiveLayer)
            return;

        var document = _session.Document;
        var layer = _session.ActiveLayer;
        var cells = _session.EditCellsInSelection(layer.Id);
        foreach (var point in FootprintSweep.Cover(Footprint, fromTopLeft, toTopLeft, document.CanvasSize))
        {
            var cell = layer.GetCell(point);
            var painted = _paint(cell);
            if (painted != cell)
                cells.Set(point, painted);
        }

        if (cells.Build(_stepNameKey) is not { } command)
            return;

        _session.History.Execute(command, _undo);
        _undoCountAfterLastEdit = _session.History.UndoCount;
    }
}
