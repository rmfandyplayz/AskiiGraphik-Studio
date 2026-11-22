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

    /// <summary>Lifts the main window's minimum size until it closes, for testing. Does nothing when no window is open.</summary>
    void AllowTinyMainWindow();

    /// <summary>Puts <paramref name="text"/> on the clipboard. Does nothing when no window is open.</summary>
    Task CopyTextAsync(string text);

    /// <summary>
    /// Puts copied cells on the clipboard twice over: as <paramref name="text"/> for other apps, and as
    /// <paramref name="cells"/>, the app's own format, which keeps their colours, shape and place (ADR 0067). Does
    /// nothing when no window is open.
    /// </summary>
    Task CopyCellsAsync(string text, string cells);

    /// <summary>What's on the clipboard. Nothing when no window is open.</summary>
    Task<ClipboardContents> ReadClipboardAsync();

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

/// <summary>What's on the clipboard, each part null when it isn't there.</summary>
/// <param name="Cells">Cells in the app's own format (ADR 0067).</param>
/// <param name="Text">Plain text.</param>
internal sealed record ClipboardContents(string? Cells, string? Text)
{
    /// <summary>An empty clipboard.</summary>
    public static ClipboardContents Nothing { get; } = new(null, null);
}
