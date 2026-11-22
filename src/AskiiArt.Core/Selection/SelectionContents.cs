// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Selection;

/// <summary>Changes what the selected cells of the active layer hold (ADR 0065).</summary>
public static class SelectionContents
{
    /// <summary>
    /// True when there are selected cells to clear: something is selected, and the active layer is neither locked
    /// nor hidden.
    /// </summary>
    public static bool CanClear(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return !session.Selection.IsEmpty && session.CanEditActiveLayer;
    }

    /// <summary>
    /// Empties the active layer's selected cells, glyph, colours and background alike, as one step, a Delete unless
    /// <paramref name="stepNameKey"/> names another, as Cut does. The selection stays.
    /// </summary>
    /// <returns>
    /// True when a cell changed; false when they were all empty already, or <see cref="CanClear"/> is false.
    /// </returns>
    public static bool Clear(EditingSession session, string stepNameKey = UndoStepNames.DeleteSelectedCells)
    {
        if (!CanClear(session))
            return false;

        var mask = session.Selection.Mask;
        var layer = session.ActiveLayer;
        var cells = session.EditCellsInSelection(layer.Id);
        for (var row = mask.Bounds.Top; row < mask.Bounds.Bottom; row++)
        {
            foreach (var (column, length) in mask.RunsInRow(row))
            {
                for (var x = column; x < column + length; x++)
                {
                    var point = new CellPoint(x, row);
                    if (!layer.GetCell(point).IsEmpty)
                        cells.Set(point, Cell.Empty);
                }
            }
        }

        if (cells.Build(stepNameKey) is not { } command)
            return false;

        session.History.Execute(command);
        return true;
    }
}
