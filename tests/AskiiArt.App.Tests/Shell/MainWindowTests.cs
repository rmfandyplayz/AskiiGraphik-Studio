// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Tools;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Shell;

public sealed class MainWindowTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly TestCanvas _canvas = new();
    private readonly List<Window> _windows = [];
    private readonly ActionRegistry _actions;

    public MainWindowTests()
    {
        // With the macOS-only actions too, so the macOS menu layout can be built as well.
        _actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs, isMacOS: true));
    }

    public void Dispose()
    {
        // Windows first: a window still showing would keep drawing the canvas. Tests that typed
        // leave unsaved changes, and closing asks about them first.
        _dialogs.SaveChangesAnswer = SaveChangesChoice.DontSave;
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();

        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void MainWindow_CanBeShown()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        window.Show();

        Assert.True(window.IsVisible);
        Assert.Equal(new WindowTitle(_canvas.ActiveDocument).Text, window.Title);
    }

    [AvaloniaFact]
    public void ClosedWindow_StopsFollowingTheTitle()
    {
        // On macOS later windows follow the same title, so a closed one mustn't keep listening.
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        var titleWhenClosed = window.Title;
        window.Close();

        var file = new DocumentFile(Path.Combine("folder", "art.asciiart"), DocumentFileVariant.Plain);
        _canvas.ActiveDocument.SetFile(file);

        Assert.Equal(titleWhenClosed, window.Title);
    }

    [AvaloniaFact]
    public void WindowMenu_HasItsMenus_WithTheirActions()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        var menuBar = NativeMenu.GetMenu(window);

        Assert.NotNull(menuBar);
        var topLevelItems = menuBar.Items.Cast<NativeMenuItem>().ToArray();
        Assert.Equal([Strings.MenuFile, Strings.MenuEdit, Strings.MenuDocument, Strings.MenuView, Strings.MenuHelp],
            topLevelItems.Select(item => item.Header));

        var quit = Assert.IsType<NativeMenuItem>(topLevelItems[0].Menu!.Items[^1]);
        Assert.Equal(Strings.ActionQuit, quit.Header);
        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Control), quit.Gesture);
        Assert.Same(_actions.Get(ShellActionIds.Quit).Command, quit.Command);

        var helpItems = topLevelItems[4].Menu!.Items;
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
    public void ZoomShortcuts_InTheMainWindow_ZoomTheCanvas()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();

        window.KeyPress(Key.OemPlus, RawInputModifiers.Control, PhysicalKey.Equal, "=");
        window.KeyRelease(Key.OemPlus, RawInputModifiers.Control, PhysicalKey.Equal, "=");
        Assert.Equal(1.25, _canvas.ViewModel.ZoomFactor);

        window.KeyPress(Key.D0, RawInputModifiers.Control, PhysicalKey.Digit0, "0");
        window.KeyRelease(Key.D0, RawInputModifiers.Control, PhysicalKey.Digit0, "0");
        Assert.Equal(1, _canvas.ViewModel.ZoomFactor);
    }

    [AvaloniaFact]
    public void UndoAndRedoShortcuts_InTheMainWindow_UndoAndRedoTyping()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered("a");
        var cell = new CellPoint(0, 0);

        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        window.KeyRelease(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        Assert.True(_canvas.Document.ActiveFrame.Layers[0].GetCell(cell).IsEmpty);

        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        window.KeyRelease(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        Assert.True(_canvas.Document.ActiveFrame.Layers[0].GetCell(cell).IsOccupied);
    }

    [AvaloniaFact]
    public void Closing_WithUnsavedChanges_AsksFirst_AndCancelKeepsTheWindowOpen()
    {
        var window = ShowWindowWithUnsavedChanges();
        _dialogs.SaveChangesAnswer = SaveChangesChoice.Cancel;

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([Strings.UntitledDocumentName], _dialogs.SaveChangesAskedFor);
        Assert.True(window.IsVisible);
    }

    [AvaloniaFact]
    public void Closing_WithUnsavedChanges_DontSave_Closes()
    {
        var window = ShowWindowWithUnsavedChanges();
        _dialogs.SaveChangesAnswer = SaveChangesChoice.DontSave;

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void Closing_WithoutUnsavedChanges_ClosesWithoutAsking()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();

        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Empty(_dialogs.SaveChangesAskedFor);
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void Title_FollowsTheDocument()
    {
        var window = ShowWindowWithUnsavedChanges();

        var unsavedTitle = string.Format(CultureInfo.CurrentCulture, Strings.WindowTitleUnsavedFormat,
            Strings.UntitledDocumentName, Strings.AppDisplayName);
        Assert.Equal(unsavedTitle, window.Title);
    }

    [AvaloniaFact]
    public void ShowGrid_IsACheckBoxMenuItemThatFollowsTheGrid()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        var viewMenu = NativeMenu.GetMenu(window)!.Items.Cast<NativeMenuItem>()
            .Single(item => item.Header == Strings.MenuView);
        var showGrid = viewMenu.Menu!.Items.OfType<NativeMenuItem>()
            .Single(item => item.Header == Strings.ActionShowGrid);
        Assert.Equal(MenuItemToggleType.CheckBox, showGrid.ToggleType);
        Assert.False(showGrid.IsChecked);

        showGrid.Command!.Execute(null);

        Assert.True(_canvas.ViewModel.IsGridVisible);
        Assert.True(showGrid.IsChecked);
    }

    [AvaloniaFact]
    public void Toolbar_HasAButtonPerAction_ThatRunsItAndDoesntTakeFocus()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();

        var buttons = ToolbarButtons(window);
        var zoomIn = buttons.Single(button => Equals(button.Content, UiText.WithoutAccessKey(Strings.ActionZoomIn)));
        Assert.Equal(ToolbarLayout.CreateDefault().ActionIds().Count(), buttons.Length);
        Assert.All(buttons, button => Assert.False(button.Focusable));

        zoomIn.Command!.Execute(null);
        Assert.Equal(1.25, _canvas.ViewModel.ZoomFactor);
    }

    [AvaloniaFact]
    public void ToolbarShowGrid_IsAToggleThatFollowsTheGrid()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        var showGrid = ToolbarButtons(window).OfType<ToggleButton>().Single();

        _canvas.ViewModel.ToggleGrid();

        Assert.True(showGrid.IsChecked);
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

        Assert.DoesNotContain(window.KeyBindings,
            binding => binding.Gesture == new KeyGesture(Key.Q, KeyModifiers.Meta));
    }

    [AvaloniaFact]
    public void InTheSystemMenuBar_AMenuActionsOtherShortcuts_GetWindowKeyBindings()
    {
        // The menu item only shows (and triggers) Zoom In's first shortcut, Cmd+=.
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false), KeyModifiers.Meta);

        Assert.DoesNotContain(window.KeyBindings,
            binding => binding.Gesture == new KeyGesture(Key.OemPlus, KeyModifiers.Meta));
        Assert.Contains(window.KeyBindings, binding => binding.Gesture == new KeyGesture(Key.Add, KeyModifiers.Meta));
    }

    [AvaloniaFact]
    public void InTheSystemMenuBar_ActionsNotInAnyMenu_StillGetAWindowKeyBinding()
    {
        var noMenus = new MenuLayout(ApplicationMenu: [], WindowMenus: [], IsSystemMenuBar: true);

        var window = BuildWindow(noMenus, KeyModifiers.Meta);

        Assert.Contains(window.KeyBindings, binding => binding.Gesture == new KeyGesture(Key.Q, KeyModifiers.Meta));
    }

    [AvaloniaFact]
    public void ApplicationMenu_OnMacOS_HasAboutWithTheAppName()
    {
        var builder = CreateBuilder(MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false));

        var applicationMenu = builder.BuildApplicationMenu(KeyModifiers.Meta);

        Assert.NotNull(applicationMenu);
        var about = Assert.IsType<NativeMenuItem>(Assert.Single(applicationMenu.Items));
        Assert.Equal(UiText.GetWithAppName(nameof(Strings.MenuAboutAppFormat)), about.Header);
        Assert.Contains(Strings.AppDisplayName, about.Header, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void ApplicationMenu_OnWindowsAndLinux_IsNotCreated()
    {
        var builder = CreateBuilder(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        Assert.Null(builder.BuildApplicationMenu(KeyModifiers.Control));
    }

    private MainWindow ShowWindowWithUnsavedChanges()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        var tool = _canvas.ViewModel.ActiveTool;
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        tool.TextEntered("a");
        return window;
    }

    private static Button[] ToolbarButtons(MainWindow window) =>
    [
        .. window.GetVisualDescendants().OfType<ContentControl>().Single(host => host.Name == "ToolbarHost")
            .GetVisualDescendants().OfType<Button>()
    ];

    private MainWindow BuildWindow(MenuLayout layout, KeyModifiers commandModifiers = KeyModifiers.Control)
    {
        var window = CreateBuilder(layout).Build(commandModifiers);
        _windows.Add(window);
        return window;
    }

    private MainWindowBuilder CreateBuilder(MenuLayout layout) =>
        new(_actions, layout, ToolbarLayout.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell);
}
