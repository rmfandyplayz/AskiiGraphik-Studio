// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell;

/// <summary>Ids of the actions that belong to the app shell itself.</summary>
internal static class ShellActionIds
{
    public const string Quit = "askiiart.quit";

    /// <summary>macOS only, where closing the window doesn't quit the app.</summary>
    public const string CloseWindow = "askiiart.closeWindow";
    public const string OpenLogFolder = "askiiart.openLogFolder";
    public const string About = "askiiart.about";

    /// <summary>Debug builds only: raises an exception so the error handler can be tested by hand.</summary>
    public const string ThrowTestException = "askiiart.debug.throwTestException";
}
