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

    /// <summary>Asks the app to quit. The window gets the chance to cancel (unsaved changes).</summary>
    void RequestShutdown();

    /// <summary>
    /// Closes the main window, which asks about unsaved changes first. On macOS the app keeps running
    /// (ADR 0023); elsewhere closing the window quits it.
    /// </summary>
    void CloseMainWindow();

    /// <summary>Puts <paramref name="text"/> on the clipboard. Does nothing when no window is open.</summary>
    Task CopyTextAsync(string text);

    Task OpenFolderAsync(string folderPath);

    Task ShowAboutAsync();

    Task<UnexpectedErrorChoice> ShowUnexpectedErrorAsync();

    /// <summary>
    /// Asks the user to confirm something that removes content. True only when they confirm;
    /// false when they cancel or no window is open to ask from.
    /// </summary>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel);

    /// <summary>Tells the user something, with an OK button. Does nothing when no window is open.</summary>
    Task ShowMessageAsync(string title, string message);
}

/// <summary>What the user chose in the unexpected-error dialog.</summary>
internal enum UnexpectedErrorChoice
{
    /// <summary>Keep working. Also the result when the dialog is closed with its close button.</summary>
    Continue = 0,

    Quit = 1,
}
