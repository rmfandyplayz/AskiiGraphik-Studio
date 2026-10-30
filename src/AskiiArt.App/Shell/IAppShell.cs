// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell;

/// <summary>
/// The shell operations that need the running desktop app: quitting, opening a folder in the OS
/// file manager, and showing dialogs. Actions and the error handler depend on this interface
/// rather than on Avalonia's application lifetime, so tests can replace it with a fake.
/// </summary>
internal interface IAppShell
{
    /// <summary>True once the main window is open, so dialogs have a window to belong to.</summary>
    bool CanShowDialogs { get; }

    /// <summary>Asks the app to quit. Windows get the chance to cancel (unsaved changes, later).</summary>
    void RequestShutdown();

    Task OpenFolderAsync(string folderPath);

    Task ShowAboutAsync();

    Task<UnexpectedErrorChoice> ShowUnexpectedErrorAsync();
}

/// <summary>What the user chose in the unexpected-error dialog.</summary>
internal enum UnexpectedErrorChoice
{
    /// <summary>Keep working. Also the result when the dialog is closed with its close button.</summary>
    Continue = 0,

    Quit = 1,
}
