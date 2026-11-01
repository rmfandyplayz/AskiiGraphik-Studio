// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Hosting;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Tests.Shell;

public sealed class ShellActionsTests
{
    private readonly FakeAppShell _shell = new();
    private readonly AppDataLocations _locations = new(Path.Combine("root", "app"));

    [Fact]
    public void Quit_AsksTheShellToShutDown()
    {
        Registry().Get(ShellActionIds.Quit).Command.Execute(null);

        Assert.Equal(1, _shell.ShutdownRequestCount);
    }

    [Fact]
    public void Quit_HasCommandQAsItsShortcut()
    {
        var quit = Registry().Get(ShellActionIds.Quit);

        Assert.Equal([new ActionShortcut(Key.Q, ShortcutModifiers.Command)], quit.DefaultShortcuts);
    }

    [Fact]
    public async Task OpenLogFolder_OpensTheLogFolder()
    {
        var command = (IAsyncRelayCommand)Registry().Get(ShellActionIds.OpenLogFolder).Command;

        await command.ExecuteAsync(null);

        Assert.Equal([_locations.LogFolder], _shell.OpenedFolders);
    }

    [Fact]
    public async Task About_ShowsTheAboutDialog()
    {
        var command = (IAsyncRelayCommand)Registry().Get(ShellActionIds.About).Command;

        await command.ExecuteAsync(null);

        Assert.Equal(1, _shell.AboutShownCount);
    }

    [Fact]
    public void ThrowTestException_ExistsOnlyWhenDebugActionsAreIncluded()
    {
        Assert.True(Registry(includeDebugActions: true).Contains(ShellActionIds.ThrowTestException));
        Assert.False(Registry(includeDebugActions: false).Contains(ShellActionIds.ThrowTestException));
    }

    [Fact]
    public void CloseWindow_ExistsOnlyOnMacOS_WithCommandW_AndClosesTheWindow()
    {
        Assert.False(Registry(isMacOS: false).Contains(ShellActionIds.CloseWindow));

        var closeWindow = Registry(isMacOS: true).Get(ShellActionIds.CloseWindow);
        closeWindow.Command.Execute(null);

        Assert.Equal(1, _shell.CloseMainWindowCount);
        Assert.Equal([new ActionShortcut(Key.W, ShortcutModifiers.Command)], closeWindow.DefaultShortcuts);
    }

    private ActionRegistry Registry(bool includeDebugActions = false, bool isMacOS = false)
    {
        return new ActionRegistry([new ShellActions(_shell, _locations, includeDebugActions, isMacOS)]);
    }
}
