// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;

namespace AskiiArt.App.Shell.Dialogs;

internal sealed class UnexpectedErrorViewModel(ICommand openLogFolderCommand)
{
    /// <summary>Opens the log folder without closing the dialog, so the user can still choose.</summary>
    public ICommand OpenLogFolderCommand { get; } = openLogFolderCommand;
}
