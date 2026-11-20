// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Characters;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Layers;
using AskiiArt.App.Painting;
using AskiiArt.App.Ramps;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell;

public sealed class MainWindowTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly FakeAppSettings _settings = new();
    private readonly TestCanvas _canvas = new();
    private readonly List<Window> _windows = [];
    private readonly ActionRegistry _actions;
    private readonly RampsPanelViewModel _ramps;
    private readonly CharactersPanelViewModel _characters;

    public MainWindowTests()
    {
        // With the macOS-only actions too, so the macOS menu layout can be built as well.
        _ramps = _canvas.CreateRampsPanelViewModel(_shell);
        _characters = _canvas.CreateCharactersPanelViewModel(_shell);
        _actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs, isMacOS: true,
            ramps: _ramps, characters: _characters));
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
    public void DamagedSettings_AreReportedOnce_WhenTheWindowOpens()
    {
        _settings.HasDamageNotice = true;
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([Strings.SettingsDamagedMessage], _shell.Messages.Select(message => message.Message));
    }

    [AvaloniaFact]
    public void ClosedWindow_IgnoresLatePositionReports()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        window.Close();
        Dispatcher.UIThread.RunJobs();

        window.KeepMinimumSizeOnScreen();
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
        Assert.Equal(
            [
                Strings.MenuFile, Strings.MenuEdit, Strings.MenuSelect, Strings.MenuDocument, Strings.MenuLayer,
                Strings.MenuTools, Strings.MenuView, Strings.MenuWindow, Strings.MenuHelp
            ],
            topLevelItems.Select(item => item.Header));

        var quit = Assert.IsType<NativeMenuItem>(topLevelItems[0].Menu!.Items[^1]);
        Assert.Equal(Strings.ActionQuit, quit.Header);
        Assert.Equal(new KeyGesture(Key.Q, KeyModifiers.Control), quit.Gesture);
        Assert.Same(_actions.Get(ShellActionIds.Quit).Command, quit.Command);

        var helpItems = topLevelItems[^1].Menu!.Items;
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
        Assert.True(_canvas.Document.ActiveFrame.Layers[0].GetCell(cell).HasGlyph);
    }

    [AvaloniaFact]
    public void SelectAllAndDeselectShortcuts_InTheMainWindow_SelectAndDeselectTheCanvas()
    {
        var window = ShowWindowWithTheCanvasFocused();

        PressCommandShortcut(window, Key.A, PhysicalKey.A, "a");
        Assert.Equal(SelectionMask.All(_canvas.Document.CanvasSize), _canvas.Session.Selection.Mask);

        PressCommandShortcut(window, Key.D, PhysicalKey.D, "d");
        Assert.True(_canvas.Session.Selection.IsEmpty);
        Assert.False(_canvas.Session.History.HasUnsavedChanges);
    }

    [AvaloniaFact]
    public void SelectAllShortcut_InATextBox_SelectsItsText_AndLeavesTheCanvasAlone()
    {
        var (window, textBox) = ShowWindowWithATextBoxFocused();

        PressCommandShortcut(window, Key.A, PhysicalKey.A, "a");

        Assert.True(_canvas.Session.Selection.IsEmpty);
        Assert.Equal(textBox.Text!.Length, Math.Abs(textBox.SelectionEnd - textBox.SelectionStart));
    }

    [AvaloniaFact]
    public void UndoShortcut_InATextBox_LeavesTheDocumentAlone()
    {
        var (window, _) = ShowWindowWithATextBoxFocused();
        SelectionEdits.SelectAll(_canvas.Session);

        PressCommandShortcut(window, Key.Z, PhysicalKey.Z, "z");

        Assert.False(_canvas.Session.Selection.IsEmpty);
        Assert.True(_canvas.Session.History.CanUndo);
    }

    [AvaloniaFact]
    public void TheMenusSelectAll_WithATextBoxFocused_SelectsItsText_AndLeavesTheCanvasAlone()
    {
        // In the macOS menu bar, Cmd+A runs the menu item before the window sees the key.
        var (window, textBox) = ShowWindowWithATextBoxFocused(isMacOS: true);

        MenuItem(window, Strings.MenuSelect, Strings.ActionSelectAll).Command!.Execute(null);

        Assert.True(_canvas.Session.Selection.IsEmpty);
        Assert.Equal(textBox.Text!.Length, Math.Abs(textBox.SelectionEnd - textBox.SelectionStart));
    }

    [AvaloniaFact]
    public void TheMenusUndoAndRedo_WithATextBoxFocused_UndoAndRedoItsTyping_AndLeaveTheDocumentAlone()
    {
        var (window, textBox) = ShowWindowWithATextBoxFocused(isMacOS: true);
        var before = textBox.Text;
        SelectionEdits.SelectAll(_canvas.Session);
        textBox.CaretIndex = before!.Length;
        window.KeyTextInput("x");
        window.KeyTextInput("y");
        Dispatcher.UIThread.RunJobs();

        MenuItem(window, Strings.MenuEdit, Strings.ActionUndo).Command!.Execute(null);

        Assert.Equal(before, textBox.Text);
        Assert.False(_canvas.Session.Selection.IsEmpty);

        MenuItem(window, Strings.MenuEdit, Strings.ActionRedo).Command!.Execute(null);

        Assert.Equal(before + "xy", textBox.Text);
        Assert.False(_canvas.Session.Selection.IsEmpty);
    }

    [AvaloniaFact]
    public void ClickingEditUndo_InTheWindowsMenuBar_WithATextBoxFocused_UndoesItsTyping_AndLeavesTheDocumentAlone()
    {
        // Clicking Edit in the menu bar inside the window takes the keyboard from the text box until the menu closes.
        var (window, textBox) = ShowWindowWithATextBoxFocused();
        var before = textBox.Text;
        SelectionEdits.SelectAll(_canvas.Session);
        textBox.CaretIndex = before!.Length;
        window.KeyTextInput("x");
        Dispatcher.UIThread.RunJobs();

        ChooseFromTheMenuBar(window, Strings.MenuEdit, Strings.ActionUndo);

        Assert.Equal(before, textBox.Text);
        Assert.False(_canvas.Session.Selection.IsEmpty);
    }

    [AvaloniaFact]
    public void ClickingSelectAll_InTheWindowsMenuBar_WithATextBoxFocused_SelectsItsText_AndLeavesTheCanvasAlone()
    {
        var (window, textBox) = ShowWindowWithATextBoxFocused();

        ChooseFromTheMenuBar(window, Strings.MenuSelect, Strings.ActionSelectAll);

        Assert.True(_canvas.Session.Selection.IsEmpty);
        Assert.Equal(textBox.Text!.Length, Math.Abs(textBox.SelectionEnd - textBox.SelectionStart));
        Assert.Same(textBox, window.FocusManager.GetFocusedElement());
    }

    private static void ChooseFromTheMenuBar(MainWindow window, string menuHeader, string itemHeader)
    {
        var menu = MenuBarOf(window).GetLogicalChildren().OfType<MenuItem>()
            .Single(item => Equals(item.Header, menuHeader));
        Click(window, menu);
        var item = menu.GetLogicalDescendants().OfType<MenuItem>().Single(entry => Equals(entry.Header, itemHeader));
        Click(TopLevel.GetTopLevel(item)!, item);
    }

    private static void Click(TopLevel topLevel, Control control)
    {
        var centre = new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
        var point = control.TranslatePoint(centre, topLevel)!.Value;
        topLevel.MouseMove(point);
        topLevel.MouseDown(point, MouseButton.Left);
        topLevel.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheMenusUndo_IsAvailable_WhileATextBoxHasTheKeyboard_EvenWithNothingToUndoInTheDocument()
    {
        var (window, _) = ShowWindowWithATextBoxFocused(isMacOS: true);
        var undo = MenuItem(window, Strings.MenuEdit, Strings.ActionUndo).Command!;
        var changes = 0;
        undo.CanExecuteChanged += (_, _) => changes++;
        Assert.True(undo.CanExecute(null));

        window.GetVisualDescendants().OfType<CanvasControl>().Single().Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.False(undo.CanExecute(null));
        Assert.True(changes > 0);
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
        Assert.Equal(ShellToolbars.CreateDefault().ActionIds().Count(), buttons.Length);
        Assert.All(buttons, button => Assert.False(button.Focusable));

        zoomIn.Command!.Execute(null);
        Assert.Equal(1.25, _canvas.ViewModel.ZoomFactor);
    }

    [AvaloniaFact]
    public void ToolbarShowGrid_IsAToggleThatFollowsTheGrid()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        var showGrid = ToolbarButtons(window).OfType<ToggleButton>()
            .Single(button => (string?)button.Content == UiText.WithoutAccessKey(Strings.ActionShowGrid));

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

    [AvaloniaTheory]
    [InlineData(ShellDockingIds.ColorPanel, typeof(ColorPanelView))]
    [InlineData(ShellDockingIds.SwatchesPanel, typeof(SwatchesView))]
    [InlineData(ShellDockingIds.RampsPanel, typeof(RampsPanelView))]
    public void TheColorAndRampsWindows_StartClosed_AndOpenWithTheirViews(string windowId, Type viewType)
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = CreateBuilder(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false), docking)
            .Build(KeyModifiers.Control);
        _windows.Add(window);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.False(docking.IsWindowOpen(windowId));

        docking.ToggleWindow(windowId);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        Assert.True(docking.IsWindowOpen(windowId));
        Assert.Contains(window.GetVisualDescendants(), visual => visual.GetType() == viewType);
    }

    [AvaloniaFact]
    public void TheRampsWindow_ListsTheRamps_WithItsButtonsBelow()
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = CreateBuilder(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false), docking)
            .Build(KeyModifiers.Control);
        _windows.Add(window);
        window.Show();
        docking.ToggleWindow(ShellDockingIds.RampsPanel);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        var view = window.GetVisualDescendants().OfType<RampsPanelView>().Single();
        Assert.Equal(BuiltInRamps.All.Count, view.GetVisualDescendants().OfType<ListBox>().Single().ItemCount);
        var buttons = view.Buttons!.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(ToolbarLayout.CreateRampsPanel().ActionIds().Count(), buttons.Count);
    }

    [AvaloniaFact]
    public void TheToolState_IsRestoredWhenTheWindowIsBuilt_AndSavedWhenItCloses()
    {
        _settings.SetText(RememberedToolState.ColorsKey, """{ "foreground": "red", "background": null }""");
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        Assert.Equal(CellColor.FromAnsi(AnsiColor.Red), _canvas.Colors.Foreground);

        _canvas.Colors.Set(CellColor.FromAnsi(AnsiColor.Blue), CellColor.Unset);
        window.Show();
        window.Close();
        Dispatcher.UIThread.RunJobs();

        Assert.Contains("blue", _settings.GetText(RememberedToolState.ColorsKey), StringComparison.Ordinal);
        Assert.NotNull(_settings.GetText(RememberedToolState.ToolOptionsKey));
    }

    [AvaloniaTheory]
    [InlineData(ShellDockingIds.ColorsToolbar, typeof(ColorsToolbar))]
    [InlineData(ShellDockingIds.ToolOptionsToolbar, typeof(ToolOptionsToolbar))]
    public void TheColorsAndToolOptionsToolbars_ShowByDefault_AndHide(string toolbarId, Type viewType)
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = CreateBuilder(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false), docking)
            .Build(KeyModifiers.Control);
        _windows.Add(window);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(docking.IsToolbarVisible(toolbarId));
        Assert.Contains(window.GetVisualDescendants(), visual => visual.GetType() == viewType);

        docking.ToggleToolbar(toolbarId);
        Dispatcher.UIThread.RunJobs();

        Assert.False(docking.IsToolbarVisible(toolbarId));
    }

    [AvaloniaFact]
    public void ChoosingABrush_ShowsTheBrushGroupsSubToolbar_AndChoosingAnotherToolHidesIt()
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = ShowWindow(docking);
        Assert.Null(docking.SubToolbarParent);

        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ShellDockingIds.ToolsToolbar, docking.SubToolbarParent);
        var buttons = SubToolbarButtons(window, Strings.ToolForegroundBrush);
        Assert.Equal(
            [Strings.ToolBrush, Strings.ToolForegroundBrush, Strings.ToolBackgroundBrush, Strings.ToolCharacterBrush],
            buttons.Select(button => button.Content));
        Assert.Equal([true, false, false, false], buttons.Select(button => button.IsChecked));

        _canvas.ViewModel.SelectTool(ColorBrushTool.BackgroundId);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(buttons, SubToolbarButtons(window, Strings.ToolForegroundBrush));
        Assert.Equal([false, false, true, false], buttons.Select(button => button.IsChecked));

        _canvas.ViewModel.SelectTool(TextTool.Id);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(docking.SubToolbarParent);
    }

    [AvaloniaFact]
    public void ChoosingAnEraser_ShowsTheEraserGroupsSubToolbar()
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = ShowWindow(docking);

        _canvas.ViewModel.SelectTool(SoftEraserTool.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(ShellDockingIds.ToolsToolbar, docking.SubToolbarParent);
        var buttons = SubToolbarButtons(window, Strings.ToolSoftEraser);
        Assert.Equal([Strings.ToolEraser, Strings.ToolSoftEraser, Strings.ToolBackgroundEraser],
            buttons.Select(button => button.Content));
        Assert.Equal([false, true, false], buttons.Select(button => button.IsChecked));
    }

    [AvaloniaFact]
    public void AToolGroupWithOneTool_HasNoSubToolbar()
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        ShowWindow(docking);

        _canvas.ViewModel.SelectTool(TextTool.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Null(docking.SubToolbarParent);
    }

    [AvaloniaFact]
    public void TheToolsToolbar_HasAButtonPerGroup_AndTheBrushesFollowTheChosenColor()
    {
        var window = ShowWindow(new ShellDocking(_settings, NullLogger<ShellDocking>.Instance));
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        Dispatcher.UIThread.RunJobs();
        var brushGroup = window.GetVisualDescendants().OfType<ToggleButton>()
            .Single(button => Equals(button.Content, Strings.ToolGroupBrush) && button.IsChecked == true);

        _canvas.Colors.Target = PaintColorTarget.Background;

        Assert.Equal(ColorBrushTool.BackgroundId, _canvas.ViewModel.ActiveTool.Descriptor.Id);
        Assert.True(brushGroup.IsChecked);
    }

    [AvaloniaFact]
    public void AWindowOpenedWhileABrushIsChosen_ShowsItsSubToolbar()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);

        ShowWindow(docking);

        Assert.Equal(ShellDockingIds.ToolsToolbar, docking.SubToolbarParent);
    }

    [AvaloniaFact]
    public void TappingAlt_OpensTheMenuBar()
    {
        var window = ShowWindowWithTheCanvasFocused();

        TapAlt(window);

        Assert.True(MenuBarOf(window).IsOpen);
    }

    [AvaloniaFact]
    public void LettingGoOfAlt_AfterAnAltClickOnTheCanvas_LeavesTheMenuBarClosed()
    {
        var window = ShowWindowWithTheCanvasFocused();
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var centre = CentreOfTheCanvas(window);

        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        window.MouseDown(centre, MouseButton.Left, RawInputModifiers.Alt);
        window.MouseUp(centre, MouseButton.Left, RawInputModifiers.Alt);
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(MenuBarOf(window).IsOpen);

        // Nothing is left half done: tapping Alt again opens the menu bar.
        TapAlt(window);
        Assert.True(MenuBarOf(window).IsOpen);
    }

    [AvaloniaFact]
    public void HoldingAlt_AfterAnAltClickOnTheCanvas_KeepsTheMenuBarsLettersPlain()
    {
        var window = ShowWindowWithTheCanvasFocused();
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var centre = CentreOfTheCanvas(window);

        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        Assert.True(AreAccessKeysUnderlined(window));
        window.MouseDown(centre, MouseButton.Left, RawInputModifiers.Alt);

        // Windows repeats a key that's held down, so Alt keeps going down while it's held.
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        Assert.False(AreAccessKeysUnderlined(window));

        window.MouseUp(centre, MouseButton.Left, RawInputModifiers.Alt);
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Dispatcher.UIThread.RunJobs();
        Assert.False(AreAccessKeysUnderlined(window));

        // Alt on its own underlines them again.
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        Assert.True(AreAccessKeysUnderlined(window));
    }

    [AvaloniaFact]
    public void LettingGoOfAlt_PressedDuringADragOnTheCanvas_LeavesTheMenuBarClosed()
    {
        var window = ShowWindowWithTheCanvasFocused();
        _canvas.ViewModel.SelectTool(RectangleSelectTool.Id);
        var centre = CentreOfTheCanvas(window);

        window.MouseDown(centre, MouseButton.Left);
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        Assert.False(AreAccessKeysUnderlined(window));
        window.MouseMove(centre + new Point(40, 40), RawInputModifiers.Alt);
        window.MouseUp(centre + new Point(40, 40), MouseButton.Left, RawInputModifiers.Alt);
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Dispatcher.UIThread.RunJobs();

        Assert.False(MenuBarOf(window).IsOpen);
        Assert.False(AreAccessKeysUnderlined(window));

        // Nothing is left half done: tapping Alt again opens the menu bar.
        TapAlt(window);
        Assert.True(MenuBarOf(window).IsOpen);
    }

    private MainWindow ShowWindow(ShellDocking docking, bool isMacOS = false)
    {
        var window = CreateBuilder(MenuLayout.CreateDefault(isMacOS, includeDebugMenu: false), docking)
            .Build(KeyModifiers.Control);
        _windows.Add(window);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private MainWindow ShowWindowWithTheCanvasFocused()
    {
        var window = BuildWindow(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.GetVisualDescendants().OfType<CanvasControl>().Single().Focus();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // The main window with the Ramps window open and the typing in one of its text boxes, which holds some text and can
    // be typed in. With the macOS menus, the menu bar has the first shortcut of each item, so the window doesn't.
    private (MainWindow Window, TextBox TextBox) ShowWindowWithATextBoxFocused(bool isMacOS = false)
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        var window = ShowWindow(docking, isMacOS);
        docking.ToggleWindow(ShellDockingIds.RampsPanel);
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var textBox = window.GetVisualDescendants().OfType<RampsPanelView>().Single()
            .GetVisualDescendants().OfType<TextBox>().First(box => !box.IsReadOnly && !string.IsNullOrEmpty(box.Text));
        textBox.Focus();
        Dispatcher.UIThread.RunJobs();
        return (window, textBox);
    }

    private static NativeMenuItem MenuItem(MainWindow window, string menu, string item)
    {
        var menuItem = NativeMenu.GetMenu(window)!.Items.Cast<NativeMenuItem>().Single(entry => entry.Header == menu);
        return menuItem.Menu!.Items.OfType<NativeMenuItem>().Single(entry => entry.Header == item);
    }

    private static void PressCommandShortcut(MainWindow window, Key key, PhysicalKey physicalKey, string symbol)
    {
        window.KeyPress(key, RawInputModifiers.Control, physicalKey, symbol);
        window.KeyRelease(key, RawInputModifiers.Control, physicalKey, symbol);
        Dispatcher.UIThread.RunJobs();
    }

    private static void TapAlt(MainWindow window)
    {
        window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static Menu MenuBarOf(MainWindow window) => window.GetVisualDescendants().OfType<Menu>().Single();

    private static bool AreAccessKeysUnderlined(MainWindow window) => window.GetValue(AccessText.ShowAccessKeyProperty);

    private static Point CentreOfTheCanvas(MainWindow window)
    {
        var canvas = window.GetVisualDescendants().OfType<CanvasControl>().Single();
        return canvas.TranslatePoint(new Rect(canvas.Bounds.Size).Center, window)!.Value;
    }

    // The buttons of the toolbar the button showing toolName is on, which is its group's sub-toolbar.
    private static ToggleButton[] SubToolbarButtons(MainWindow window, string toolName)
    {
        var tool = window.GetVisualDescendants().OfType<ToggleButton>()
            .Single(button => Equals(button.Content, toolName));
        var bar = tool.GetVisualAncestors().OfType<StackPanel>()
            .First(panel => panel.Name == ToolbarBuilder.ControlName);
        return [.. bar.GetVisualDescendants().OfType<ToggleButton>()];
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

    // Every button on every toolbar. They're only in the visual tree once the window has laid out.
    private static Button[] ToolbarButtons(MainWindow window)
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();

        return
        [
            .. window.GetVisualDescendants().OfType<StackPanel>().Where(bar => bar.Name == ToolbarBuilder.ControlName)
                .SelectMany(bar => bar.GetVisualDescendants().OfType<Button>())
        ];
    }

    private MainWindow BuildWindow(MenuLayout layout, KeyModifiers commandModifiers = KeyModifiers.Control)
    {
        var window = CreateBuilder(layout).Build(commandModifiers);
        _windows.Add(window);
        return window;
    }

    private MainWindowBuilder CreateBuilder(MenuLayout layout, ShellDocking? docking = null) =>
        new(_actions, layout, ShellToolbars.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell,
            docking ?? new ShellDocking(_settings, NullLogger<ShellDocking>.Instance), _settings,
            new HistoryPanelViewModel(_canvas.ActiveDocument), new LayersPanelViewModel(_canvas.ActiveDocument),
            _canvas.CreatePaintColorsViewModel(), _canvas.CreateToolOptionsViewModel(), _ramps,
            _characters, new RememberedToolState(_settings));
}
