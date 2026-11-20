// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// The selection of one editing session (ARCHITECTURE §10). It isn't part of the document and isn't saved, but
/// changing it is an undo step, so it changes only through <see cref="SetSelectionCommand"/> (ADR 0055).
/// </summary>
public sealed class SessionSelection
{
    internal SessionSelection(CanvasSize canvasSize) => Mask = SelectionMask.Empty(canvasSize);

    /// <summary>Raised after <see cref="Mask"/> changes.</summary>
    public event EventHandler<SessionSelectionChangedEventArgs>? Changed;

    /// <summary>
    /// The selected cells. Its canvas size is the document's, as long as the canvas is resized through
    /// <see cref="Editing.EditingSession.ResizeCanvas"/>.
    /// </summary>
    public SelectionMask Mask { get; private set; }

    /// <summary>True when nothing is selected.</summary>
    public bool IsEmpty => Mask.IsEmpty;

    internal void Set(SelectionMask mask)
    {
        ArgumentNullException.ThrowIfNull(mask);
        if (mask.Equals(Mask))
            return;

        var oldMask = Mask;
        Mask = mask;
        Changed?.Invoke(this, new SessionSelectionChangedEventArgs(oldMask, mask));
    }
}

/// <summary>What a selection change replaced, and with what.</summary>
public sealed class SessionSelectionChangedEventArgs(SelectionMask oldMask, SelectionMask newMask) : EventArgs
{
    /// <summary>The selection before the change.</summary>
    public SelectionMask OldMask { get; } = oldMask;

    /// <summary>The selection after the change.</summary>
    public SelectionMask NewMask { get; } = newMask;
}
