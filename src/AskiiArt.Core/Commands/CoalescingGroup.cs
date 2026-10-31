// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Commands;

/// <summary>
/// Marks commands that may merge into one undo step, such as the keystrokes of one typing burst
/// (ARCHITECTURE §9). Groups compare by identity: the caller makes a new group whenever merging
/// should stop (the caret jumps, the tool changes, the user pauses).
/// </summary>
/// <remarks>
/// The history also stops merging on its own after an undo, a redo, a save, or a command from
/// another group.
/// </remarks>
public sealed class CoalescingGroup;
