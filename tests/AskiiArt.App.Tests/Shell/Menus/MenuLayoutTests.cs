// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Tests.Fakes;

namespace AskiiArt.App.Tests.Shell.Menus;

public sealed class MenuLayoutTests
{
    [Fact]
    public void WindowsAndLinux_HaveFileQuit_View_AndHelpWithOpenLogFolderAndAbout()
    {
        var layout = MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false);

        Assert.False(layout.IsSystemMenuBar);
        Assert.Empty(layout.ApplicationMenu);
        Assert.Collection(
            layout.WindowMenus,
            file =>
            {
                Assert.Equal(nameof(Strings.MenuFile), file.LabelKey);
                Assert.Equal([ShellActionIds.Quit], ActionIdsIn(file.Entries));
            },
            AssertIsViewMenu,
            help =>
            {
                Assert.Equal(nameof(Strings.MenuHelp), help.LabelKey);
                Assert.Equal([ShellActionIds.OpenLogFolder, ShellActionIds.About], ActionIdsIn(help.Entries));
            });
    }

    [Fact]
    public void MacOS_PutsAboutInTheApplicationMenu_AndHasNoFileMenu()
    {
        var layout = MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false);

        Assert.True(layout.IsSystemMenuBar);
        var about = Assert.IsType<ActionMenuEntry>(Assert.Single(layout.ApplicationMenu));
        Assert.Equal(ShellActionIds.About, about.ActionId);
        Assert.Equal(nameof(Strings.MenuAboutAppFormat), about.LabelWithAppNameKey);

        Assert.Collection(
            layout.WindowMenus,
            AssertIsViewMenu,
            help =>
            {
                Assert.Equal(nameof(Strings.MenuHelp), help.LabelKey);
                Assert.Equal([ShellActionIds.OpenLogFolder], ActionIdsIn(help.Entries));
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DebugMenu_IsLastAndOnlyThereWhenRequested(bool isMacOS)
    {
        var withDebug = MenuLayout.CreateDefault(isMacOS, includeDebugMenu: true);
        var withoutDebug = MenuLayout.CreateDefault(isMacOS, includeDebugMenu: false);

        Assert.Equal(nameof(Strings.MenuDebug), withDebug.WindowMenus[^1].LabelKey);
        Assert.Equal([ShellActionIds.ThrowTestException], ActionIdsIn(withDebug.WindowMenus[^1].Entries));
        Assert.DoesNotContain(withoutDebug.WindowMenus, menu => menu.LabelKey == nameof(Strings.MenuDebug));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EveryMenuEntry_RefersToARegisteredAction_AndHasALabel(bool isMacOS, bool includeDebugMenu)
    {
        var layout = MenuLayout.CreateDefault(isMacOS, includeDebugMenu);
        using var canvas = new TestCanvas();
        var registry = new ActionRegistry([
            new ShellActions(new FakeAppShell(), new AppDataLocations("root"), includeDebugActions: includeDebugMenu),
            new CanvasActions(canvas.ViewModel)
        ]);

        foreach (var actionId in layout.ActionIds())
            Assert.True(registry.Contains(actionId), $"The menu refers to the unregistered action '{actionId}'.");

        foreach (var menu in layout.WindowMenus)
            Assert.False(string.IsNullOrWhiteSpace(UiText.Get(menu.LabelKey)));
    }

    private static void AssertIsViewMenu(MenuDefinition view)
    {
        Assert.Equal(nameof(Strings.MenuView), view.LabelKey);
        Assert.Equal(
            [CanvasActionIds.ZoomIn, CanvasActionIds.ZoomOut, CanvasActionIds.ActualSize, CanvasActionIds.ShowGrid],
            ActionIdsIn(view.Entries));
    }

    private static string[] ActionIdsIn(IEnumerable<MenuEntry> entries)
    {
        return entries.OfType<ActionMenuEntry>().Select(entry => entry.ActionId).ToArray();
    }
}
