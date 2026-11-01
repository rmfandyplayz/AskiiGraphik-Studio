// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;

namespace AskiiArt.App.Tests.Shell.Menus;

public sealed class MenuLayoutTests
{
    private static string[] FileActionIds { get; } =
    [
        DocumentActionIds.New, DocumentActionIds.Open, DocumentActionIds.Save, DocumentActionIds.SaveAs,
        DocumentActionIds.ExportAsText
    ];

    [Fact]
    public void WindowsAndLinux_HaveFile_Edit_Document_View_AndHelpWithOpenLogFolderAndAbout()
    {
        var layout = MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false);

        Assert.False(layout.IsSystemMenuBar);
        Assert.Empty(layout.ApplicationMenu);
        Assert.Collection(
            layout.WindowMenus,
            file =>
            {
                Assert.Equal(nameof(Strings.MenuFile), file.LabelKey);
                Assert.Equal([.. FileActionIds, ShellActionIds.Quit], ActionIdsIn(file.Entries));
            },
            AssertIsEditMenu,
            AssertIsDocumentMenu,
            AssertIsViewMenu,
            help =>
            {
                Assert.Equal(nameof(Strings.MenuHelp), help.LabelKey);
                Assert.Equal([ShellActionIds.OpenLogFolder, ShellActionIds.About], ActionIdsIn(help.Entries));
            });
    }

    [Fact]
    public void MacOS_PutsAboutInTheApplicationMenu_AndNoQuitInTheFileMenu()
    {
        var layout = MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false);

        Assert.True(layout.IsSystemMenuBar);
        var about = Assert.IsType<ActionMenuEntry>(Assert.Single(layout.ApplicationMenu));
        Assert.Equal(ShellActionIds.About, about.ActionId);
        Assert.Equal(nameof(Strings.MenuAboutAppFormat), about.LabelWithAppNameKey);

        Assert.Collection(
            layout.WindowMenus,
            file =>
            {
                Assert.Equal(nameof(Strings.MenuFile), file.LabelKey);
                Assert.Equal(
                [
                    DocumentActionIds.New, DocumentActionIds.Open, ShellActionIds.CloseWindow, DocumentActionIds.Save,
                    DocumentActionIds.SaveAs, DocumentActionIds.ExportAsText
                ], ActionIdsIn(file.Entries));
            },
            AssertIsEditMenu,
            AssertIsDocumentMenu,
            AssertIsViewMenu,
            help =>
            {
                Assert.Equal(nameof(Strings.MenuHelp), help.LabelKey);
                Assert.Equal([ShellActionIds.OpenLogFolder], ActionIdsIn(help.Entries));
            });
    }

    [Fact]
    public void OnlyMacOS_HasADockMenu_WithNewAndOpen()
    {
        var mac = MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false);
        var windows = MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false);

        Assert.Equal([DocumentActionIds.New, DocumentActionIds.Open], ActionIdsIn(mac.DockMenu));
        Assert.Empty(windows.DockMenu);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DebugMenu_IsLastAndOnlyThereWhenRequested(bool isMacOS)
    {
        var withDebug = MenuLayout.CreateDefault(isMacOS, includeDebugMenu: true);
        var withoutDebug = MenuLayout.CreateDefault(isMacOS, includeDebugMenu: false);

        Assert.Equal(nameof(Strings.MenuDebug), withDebug.WindowMenus[^1].LabelKey);
        Assert.Equal([DocumentActionIds.OpenSampleDocument, ShellActionIds.ThrowTestException],
            ActionIdsIn(withDebug.WindowMenus[^1].Entries));
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
        var registry = new ActionRegistry(TestActions.CreateProviders(canvas, new FakeAppShell(),
            includeDebugActions: includeDebugMenu, isMacOS: isMacOS));

        foreach (var actionId in layout.ActionIds().Concat(ToolbarLayout.CreateDefault().ActionIds()))
        {
            Assert.True(registry.Contains(actionId),
                $"A menu or the toolbar uses the unregistered action '{actionId}'.");
        }

        foreach (var menu in layout.WindowMenus)
            Assert.False(string.IsNullOrWhiteSpace(UiText.Get(menu.LabelKey)));
    }

    private static void AssertIsEditMenu(MenuDefinition edit)
    {
        Assert.Equal(nameof(Strings.MenuEdit), edit.LabelKey);
        Assert.Equal([EditActionIds.Undo, EditActionIds.Redo, EditActionIds.CopyAsText], ActionIdsIn(edit.Entries));
    }

    private static void AssertIsDocumentMenu(MenuDefinition document)
    {
        Assert.Equal(nameof(Strings.MenuDocument), document.LabelKey);
        Assert.Equal([DocumentActionIds.ResizeCanvas, DocumentActionIds.Font, DocumentActionIds.AsciiOnly],
            ActionIdsIn(document.Entries));
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
