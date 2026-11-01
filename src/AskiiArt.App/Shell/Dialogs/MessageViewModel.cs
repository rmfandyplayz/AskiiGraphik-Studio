// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>The text of a dialog that tells the user something. Every string comes from Strings.resx.</summary>
/// <param name="Title">The window title.</param>
/// <param name="Message">What happened, and what the user can do about it.</param>
internal sealed record MessageViewModel(string Title, string Message);
