// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Commands;

/// <summary>
/// A state of a <see cref="CommandHistory"/> that's being saved, from
/// <see cref="CommandHistory.BeginSave"/>. Saving writes the file in the background, so the user
/// may edit before it's done; marking this state as saved, rather than the history's state at
/// that moment, keeps those edits unsaved.
/// </summary>
public sealed class SavePoint
{
    internal SavePoint(long position, object? lastStep)
    {
        Position = position;
        LastStep = lastStep;
    }

    /// <summary>How many steps had ever been applied, counting the ones dropped to save memory.</summary>
    internal long Position { get; }

    /// <summary>The step that led to the state, or null for the history's starting state.</summary>
    internal object? LastStep { get; }
}
