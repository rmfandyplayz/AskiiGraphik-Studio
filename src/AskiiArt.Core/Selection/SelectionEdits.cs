// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
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
}
