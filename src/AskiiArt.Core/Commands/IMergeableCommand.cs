// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;

namespace AskiiArt.Core.Commands;

/// <summary>
/// A command that can absorb the command that follows it, so both become one undo step
/// (coalescing, ARCHITECTURE §9). The history only asks when both were executed with the same
/// <see cref="CoalescingGroup"/>.
/// </summary>
public interface IMergeableCommand : IDocumentCommand
{
    /// <summary>
    /// Tries to combine this command, already applied, with <paramref name="following"/>, applied right
    /// after it. On success, reverting <paramref name="merged"/> undoes both.
    /// </summary>
    bool TryMergeWith(IDocumentCommand following, [NotNullWhen(true)] out IDocumentCommand? merged);
}
