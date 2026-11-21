// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.Core.Selection;

/// <summary>
/// Changes a session's selection, each change an undo step (ADR 0055). A change that wouldn't change anything makes
/// no step.
/// </summary>
public static class SelectionEdits
{
    /// <summary>Makes <paramref name="mask"/> the selection, as a step named <paramref name="stepNameKey"/>.</summary>
    /// <returns>True when the selection changed; false when it already was <paramref name="mask"/>.</returns>
    /// <exception cref="ArgumentException">The mask belongs to a canvas of another size.</exception>
    public static bool Select(EditingSession session, SelectionMask mask, string stepNameKey)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(mask);

        if (mask.Equals(session.Selection.Mask))
            return false;

        session.History.Execute(new SetSelectionCommand(session.Selection, mask, stepNameKey));
        return true;
    }

    /// <summary>Selects every cell of the canvas.</summary>
    /// <returns>True when the selection changed.</returns>
    public static bool SelectAll(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, SelectionMask.All(session.Document.CanvasSize), UndoStepNames.SelectAll);
    }

    /// <summary>Selects nothing.</summary>
    /// <returns>True when the selection changed; false when nothing was selected.</returns>
    public static bool Deselect(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Selection.IsEmpty)
            return false;

        return Select(session, SelectionMask.Empty(session.Document.CanvasSize), UndoStepNames.Deselect);
    }

    /// <summary>Selects every cell that isn't selected, and none that is.</summary>
    /// <returns>True; inverting always changes the selection.</returns>
    public static bool Invert(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, session.Selection.Mask.Inverted(), UndoStepNames.InvertSelection);
    }

    /// <summary>Grows the selection by one cell all round, corners included (ADR 0063).</summary>
    /// <returns>
    /// True when the selection changed; false when nothing is selected or it already fills the canvas.
    /// </returns>
    public static bool Grow(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, session.Selection.Mask.Grown(), UndoStepNames.GrowSelection);
    }

    /// <summary>
    /// Shrinks the selection by one cell all round, corners included, leaving cells along the canvas's edges
    /// (ADR 0063).
    /// </summary>
    /// <returns>True when the selection changed; false when nothing is selected or nothing would be left out.</returns>
    public static bool Shrink(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, session.Selection.Mask.Shrunk(), UndoStepNames.ShrinkSelection);
    }

    /// <summary>
    /// Selects the active layer's cells that hold a character other than a space, in place of the selection.
    /// </summary>
    /// <returns>True when the selection changed.</returns>
    public static bool SelectCellsWithGlyphs(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, CellsOfActiveLayer(session, HasInk), UndoStepNames.SelectCellsWithGlyphs);
    }

    /// <summary>Selects the active layer's cells that have a background colour, in place of the selection.</summary>
    /// <returns>True when the selection changed.</returns>
    public static bool SelectCellsWithBackground(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return Select(session, CellsOfActiveLayer(session, cell => cell.HasBackground),
            UndoStepNames.SelectCellsWithBackground);
    }

    private static bool HasInk(Cell cell) => cell.HasGlyph && cell.Glyph.Value != ' ';

    private static SelectionMask CellsOfActiveLayer(EditingSession session, Func<Cell, bool> isWanted)
    {
        var canvasSize = session.Document.CanvasSize;
        var row = new Cell[canvasSize.Width];
        var builder = new SelectionMaskBuilder(canvasSize);
        for (var y = 0; y < canvasSize.Height; y++)
        {
            session.ActiveLayer.CopyCells(new CellRect(0, y, canvasSize.Width, 1), row);
            for (var x = 0; x < row.Length; x++)
            {
                if (isWanted(row[x]))
                    builder.Add(new CellPoint(x, y));
            }
        }

        return builder.Build();
    }
}
