// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// An undoable change to a document (ARCHITECTURE §9). Every document change is a command
/// (principle 3); run commands through <see cref="CommandHistory.Execute"/>.
/// </summary>
/// <remarks>
/// <para>
/// A command stores only what it touches. Some commands capture the "before" state when they're
/// applied, so <see cref="Revert"/> is only valid right after <see cref="Apply"/>, on the same
/// document. The history guarantees that order.
/// </para>
/// <para>
/// <see cref="Apply"/> checks everything it needs before changing anything, so a command that
/// throws leaves the document unchanged.
/// </para>
/// </remarks>
public interface IDocumentCommand
{
    /// <summary>
    /// Roughly how much memory the command holds while it's in the history, in bytes. The history
    /// reads it after <see cref="Apply"/>, so a command may compute it then.
    /// </summary>
    long EstimatedMemoryInBytes { get; }

    /// <summary>Makes the change.</summary>
    void Apply(Document document);

    /// <summary>Undoes the change, putting the document back exactly as it was before <see cref="Apply"/>.</summary>
    void Revert(Document document);
}
