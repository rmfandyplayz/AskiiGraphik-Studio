// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>The text of a yes-or-cancel dialog. Every string comes from Strings.resx.</summary>
/// <param name="Title">The window title.</param>
/// <param name="Message">What will happen, and whether it can be undone.</param>
/// <param name="ConfirmLabel">The button that goes ahead, named after what it does.</param>
internal sealed record ConfirmViewModel(string Title, string Message, string ConfirmLabel);
