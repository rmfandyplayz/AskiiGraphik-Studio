// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;

namespace AskiiArt.App.Tests.Shell;

public sealed class MainWindowTests
{
    private readonly FakeAppShell _shell = new();
    private readonly ActionRegistry _actions;

    public MainWindowTests()
    {
        _actions = new ActionRegistry([
            new ShellActions(_shell, new AppDataLocations("root"), includeDebugActions: false)
        ]);
    }

    [AvaloniaFact]
    public void MainWindow_CanBeShown()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        window.Show();

        Assert.True(window.IsVisible);
        Assert.Equal(Strings.AppDisplayName, window.Title);
    }

    [AvaloniaFact]
    public void WindowMenu_HasFileAndHelp_WithTheirActions()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        var menuBar = NativeMenu.GetMenu(window);

        Assert.NotNull(menuBar);
        var topLevelItems = menuBar.Items.Cast<NativeMenuItem>().ToArray();
        Assert.Equal([Strings.MenuFile, Strings.MenuHelp], topLevelItems.Select(item => item.Header));

        var quit = Assert.IsType<NativeMenuItem>(Assert.Single(topLevelItems[0].Menu!.Items));
        Assert.Equal(Strings.ActionQuit, quit.Header);
        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Control), quit.Gesture);
        Assert.Same(_actions.Get(ShellActionIds.Quit).Command, quit.Command);

        var helpItems = topLevelItems[1].Menu!.Items;
        Assert.Equal(3, helpItems.Count);
        Assert.IsType<NativeMenuItemSeparator>(helpItems[1]);
    }

    [AvaloniaFact]
    public void PressingCtrlQ_InTheMainWindow_QuitsOnWindowsAndLinux()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();

        window.KeyPress(Key.Q, RawInputModifiers.Control, PhysicalKey.Q, "q");
        window.KeyRelease(Key.Q, RawInputModifiers.Control, PhysicalKey.Q, "q");

        Assert.Equal(1, _shell.ShutdownRequestCount);
    }

    [AvaloniaFact]
    public void InTheSystemMenuBar_ActionsInAMenu_GetNoExtraWindowKeyBinding()
    {
        // macOS: the system menu bar triggers a menu item's shortcut itself, so a window key
        // binding would run the action a second time.
        var quitInMenu = new MenuLayout(
            ApplicationMenu: [],
            WindowMenus: [new MenuDefinition(nameof(Strings.MenuFile), [new ActionMenuEntry(ShellActionIds.Quit)])],
            IsSystemMenuBar: true);

        var window = BuildWindow(quitInMenu, KeyModifiers.Meta);

        Assert.Empty(window.KeyBindings);
    }

    [AvaloniaFact]
    public void InTheSystemMenuBar_ActionsNotInAnyMenu_StillGetAWindowKeyBinding()
    {
        var noMenus = new MenuLayout(ApplicationMenu: [], WindowMenus: [], IsSystemMenuBar: true);

        var window = BuildWindow(noMenus, KeyModifiers.Meta);

        var binding = Assert.Single(window.KeyBindings);
        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Meta), binding.Gesture);
    }

    [AvaloniaFact]
    public void ApplicationMenu_OnMacOS_HasAboutWithTheAppName()
    {
        var builder = new MainWindowBuilder(_actions, MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false));

        var applicationMenu = builder.BuildApplicationMenu(KeyModifiers.Meta);

        Assert.NotNull(applicationMenu);
        var about = Assert.IsType<NativeMenuItem>(Assert.Single(applicationMenu.Items));
        Assert.Equal(UiText.GetWithAppName(nameof(Strings.MenuAboutAppFormat)), about.Header);
        Assert.Contains(Strings.AppDisplayName, about.Header, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void ApplicationMenu_OnWindowsAndLinux_IsNotCreated()
    {
        var builder =
            new MainWindowBuilder(_actions, MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        Assert.Null(builder.BuildApplicationMenu(KeyModifiers.Control));
    }

    private MainWindow BuildWindow(MenuLayout layout, KeyModifiers commandModifiers = KeyModifiers.Control)
    {
        return new MainWindowBuilder(_actions, layout).Build(commandModifiers);
    }
}
