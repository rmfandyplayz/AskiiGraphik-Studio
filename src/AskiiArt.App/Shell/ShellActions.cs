// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell;

/// <summary>
/// The shell's own actions: Quit, Close Window (macOS only), Open Log Folder, About, and (Debug
/// builds only) Throw Test Exception.
/// </summary>
internal sealed class ShellActions(IAppShell shell, AppDataLocations locations, bool includeDebugActions,
    bool isMacOS) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return new AppAction(
            ShellActionIds.Quit,
            nameof(Strings.ActionQuit),
            new RelayCommand(shell.RequestShutdown),
            [new ActionShortcut(Key.Q, ShortcutModifiers.Command)]);

        // Elsewhere closing the window quits the app, which Quit already does.
        if (isMacOS)
        {
            yield return new AppAction(
                ShellActionIds.CloseWindow,
                nameof(Strings.ActionCloseWindow),
                new RelayCommand(shell.CloseMainWindow),
                [new ActionShortcut(Key.W, ShortcutModifiers.Command)]);
        }

        yield return new AppAction(
            ShellActionIds.OpenLogFolder,
            nameof(Strings.ActionOpenLogFolder),
            new AsyncRelayCommand(() => shell.OpenFolderAsync(locations.LogFolder)));

        yield return new AppAction(
            ShellActionIds.About,
            nameof(Strings.ActionAbout),
            new AsyncRelayCommand(shell.ShowAboutAsync));

        if (includeDebugActions)
        {
            yield return new AppAction(
                ShellActionIds.ThrowTestException,
                nameof(Strings.ActionThrowTestException),
                new RelayCommand(PostTestException));
        }
    }

    // Thrown from a dispatcher job rather than from the command itself, so it takes the same path
    // as a typical UI-thread exception no matter how the menu delivered the click (an in-window
    // menu, or the macOS menu bar, which calls in from native code).
    private static void PostTestException()
    {
        Dispatcher.UIThread.Post(() => throw new InvalidOperationException("Test exception raised from the Debug menu."));
    }
}
