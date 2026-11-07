// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Docking.Layouts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>Dragging and resizing windows with a simulated pointer, in a bare window with the default layout.</summary>
public sealed class WindowDraggingTests : IDisposable
{
    private const string Layers = ShellDockingIds.LayersPanel;
    private const string History = ShellDockingIds.HistoryPanel;
    private const string Tools = ShellDockingIds.ToolsToolbar;

    private static readonly Point _middleOfNowhere = new(400, 350);
    private static readonly Point _leftEdge = new(5, 350);

    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;
    private readonly ActionRegistry _actions;

    private Window _window = null!;
    private DockingHost _host = null!;
    private ShellLayoutMetrics _metrics = null!;

    public WindowDraggingTests()
    {
        _docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        _actions = new ActionRegistry([new ToolbarMenuActions(_docking), new WindowFrameActions(_docking)]);
    }

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void AClickOnATitle_IsntADrag()
    {
        ShowWindow();
        var title = TitleOf(Layers);

        _window.MouseDown(title, MouseButton.Left);
        _window.MouseMove(title + new Point(1, 1));
        _window.MouseUp(title + new Point(1, 1), MouseButton.Left);
        Pump();

        Assert.False(_docking.IsWindowFloating(Layers));
    }

    [AvaloniaFact]
    public void DraggingATitleOverTheCanvas_FloatsTheWindow_WhereItWasLeft()
    {
        ShowWindow();
        var size = BoundsOf(Layers).Size;
        var fromCorner = TitleOf(Layers) - BoundsOf(Layers).TopLeft;

        DragAndDrop(Layers, _middleOfNowhere);

        Assert.True(_docking.IsWindowFloating(Layers));
        Assert.Equal(_middleOfNowhere - fromCorner, BoundsOf(Layers).TopLeft);
        Assert.Equal(size, BoundsOf(Layers).Size);
    }

    [AvaloniaFact]
    public void WhileHeld_TheWindowFollowsThePointer_AndTheOthersFillItsPlace()
    {
        ShowWindow();
        var fromCorner = TitleOf(Layers) - BoundsOf(Layers).TopLeft;
        var historyBefore = BoundsOf(History);

        StartDrag(Layers, _middleOfNowhere);

        Assert.Equal(_middleOfNowhere - fromCorner, BoundsOf(Layers).TopLeft);
        Assert.True(BoundsOf(History).Height > historyBefore.Height);
        Assert.True(Frame(Layers).ZIndex > Frame(History).ZIndex);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void OverAWindowsLowerHalf_ThePlaceholderShowsWhereItWouldLand_AndADropPutsItThere()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);

        var lowerHalf = new Point(BoundsOf(History).Center.X, BoundsOf(History).Bottom - 20);
        StartDrag(Layers, lowerHalf);

        var placeholder = Placeholder!;
        Assert.True(placeholder.IsVisible);
        Assert.True(placeholder.Bounds.Top > BoundsOf(History).Top);
        Assert.True(Frame(Layers).IsAnchoring);
        var landing = placeholder.Bounds;

        _window.MouseUp(lowerHalf, MouseButton.Left);
        Pump();

        Assert.False(_docking.IsWindowFloating(Layers));
        Assert.Equal(landing, BoundsOf(Layers));
        Assert.False(Frame(Layers).IsAnchoring);
        Assert.Null(Placeholder);
    }

    [AvaloniaFact]
    public void AwayFromAZone_ThereIsNoPlaceholder_AndTheWindowIsSolid()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);

        Assert.False(Placeholder!.IsVisible);
        Assert.False(Frame(Layers).IsAnchoring);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AnEmptySide_ShowsAStripAtItsEdge()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);

        // The right, which has windows, has a strip too, for making a dock.
        var strip = Assert.Single(_host.Children.OfType<DropZoneView>(), view => view.Bounds.Left == 0);
        Assert.Equal(_host.Bounds.Height, strip.Bounds.Height);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void OverAnEmptySidesStrip_AThinPlaceholderOpens_ToolbarsMoveIn_AndADropMakesAWidget()
    {
        ShowWindow();
        _docking.ToggleToolbar(ShellDockingIds.FileToolbar);
        Pump();
        var toolsBefore = BoundsOf(ShellDockingIds.EditToolbar);

        StartDrag(Layers, _leftEdge);

        Assert.Equal(_metrics.Docking.WindowEdgePlaceholderWidth, Placeholder!.Bounds.Width);
        Assert.True(Assert.Single(_host.Children.OfType<DropZoneView>(), view => view.Bounds.Left == 0).IsOutlined);
        _ = toolsBefore;

        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        Assert.Equal(_metrics.WidgetWidth, BoundsOf(Layers).Width);
        Assert.Equal(_metrics.Docking.Margin, BoundsOf(Layers).Left);
        Assert.False(_docking.IsWindowFloating(Layers));
    }

    [AvaloniaFact]
    public void DraggingTheLastWindowOffASide_LetsItsToolbarsMoveBack()
    {
        ShowWindow();
        var edit = BoundsOf(ShellDockingIds.EditToolbar);

        _docking.ToggleWindow(History);
        Pump();
        DragAndDrop(Layers, _middleOfNowhere);

        Assert.True(BoundsOf(ShellDockingIds.EditToolbar).Right > edit.Right);
    }

    [AvaloniaFact]
    public void Escape_PutsTheWindowBack_AndTheReleaseThatFollowsDoesNothing()
    {
        ShowWindow();
        var before = LayoutJson.Write(_docking.Layout!);
        var where = BoundsOf(Layers);

        StartDrag(Layers, _middleOfNowhere);
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump();

        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(where, BoundsOf(Layers));
        Assert.Null(Placeholder);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();
        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
    }

    [AvaloniaFact]
    public void TheLayout_OnlyChangesOnTheDrop()
    {
        ShowWindow();
        var before = LayoutJson.Write(_docking.Layout!);
        var changes = 0;
        _docking.LayoutChanged += (_, _) => changes++;

        StartDrag(Layers, _middleOfNowhere);
        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(0, changes);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();
        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void TheSafeArea_HoldsStillDuringADrag_AndAResize_AndChangesWhenItEnds()
    {
        ShowWindow();
        var before = _host.SafeArea;

        StartDrag(Layers, _middleOfNowhere);
        Assert.Equal(before, _host.SafeArea);
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();

        var edge = InnerEdge();
        _window.MouseDown(edge, MouseButton.Left);
        _window.MouseMove(edge + new Point(-40, 0));
        Pump();
        Assert.Equal(before, _host.SafeArea);

        _window.MouseUp(edge + new Point(-40, 0), MouseButton.Left);
        Pump();
        Assert.True(_host.SafeArea.Right > before.Right);
    }

    [AvaloniaFact]
    public void AChangeFromElsewhereDuringADrag_CancelsIt()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);
        _docking.ToggleToolbar(Tools);
        Pump();

        Assert.False(_docking.IsWindowFloating(Layers));
        Assert.Null(Placeholder);
    }

    [AvaloniaFact]
    public void ClosingTheAppWindowDuringADrag_SavesTheLayoutFromBeforeIt()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);
        _docking.SaveAndDetach();

        var saved = LayoutJson.TryRead(_settings.GetText(ShellDocking.LayoutSettingKey)!);
        Assert.Empty(saved!.FloatingWindows);
    }

    [AvaloniaFact]
    public void AFloatingWindow_SurvivesARestart()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var where = BoundsOf(Layers);
        _docking.SaveAndDetach();

        ShowWindow();

        Assert.True(_docking.IsWindowFloating(Layers));
        Assert.Equal(where, BoundsOf(Layers));
    }

    [AvaloniaFact]
    public void AFloatingWindow_DrawsOverToolbarsAndTheOtherWindows()
    {
        ShowWindow();

        DragAndDrop(Layers, _middleOfNowhere);

        Assert.True(Frame(Layers).ZIndex > Frame(History).ZIndex);
        Assert.True(Frame(Layers).ZIndex > ToolbarFrameOf(Tools).ZIndex);
    }

    [AvaloniaFact]
    public void PressingAFloatingWindow_BringsItToTheFront()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        DragAndDrop(History, _middleOfNowhere + new Point(250, 30));
        Assert.True(Frame(History).ZIndex > Frame(Layers).ZIndex);

        var onLayers = BoundsOf(Layers).TopLeft + new Point(10, 60);
        _window.MouseDown(onLayers, MouseButton.Left);
        _window.MouseUp(onLayers, MouseButton.Left);
        Pump();

        Assert.True(Frame(Layers).ZIndex > Frame(History).ZIndex);
    }

    [AvaloniaFact]
    public void ShrinkingTheAppWindow_KeepsAFloatingWindowInside_AndRegrowingPutsItBack()
    {
        ShowWindow();
        DragAndDrop(Layers, new Point(800, 500));
        var where = BoundsOf(Layers).TopLeft;

        _window.Width = 500;
        _window.Height = 400;
        Pump();
        Assert.True(BoundsOf(Layers).Right <= 500);
        Assert.True(BoundsOf(Layers).Bottom <= 400);

        _window.Width = 1000;
        _window.Height = 700;
        Pump();
        Assert.Equal(where, BoundsOf(Layers).TopLeft);
    }

    [AvaloniaFact]
    public void OnlyAFloatingWindow_HasResizeGrips()
    {
        ShowWindow();
        Assert.False(Frame(Layers).IsFloating);

        DragAndDrop(Layers, _middleOfNowhere);

        Assert.True(Frame(Layers).IsFloating);
        Assert.False(Frame(History).IsFloating);
    }

    [AvaloniaFact]
    public void DraggingAFloatingWindowsCorner_ResizesIt()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var before = BoundsOf(Layers);
        var corner = before.BottomRight - new Point(2, 2);

        _window.MouseDown(corner, MouseButton.Left);
        _window.MouseMove(corner + new Point(30, -20));
        Pump();
        _window.MouseUp(corner + new Point(30, -20), MouseButton.Left);
        Pump();

        Assert.Equal(before.Width + 30, BoundsOf(Layers).Width);
        Assert.Equal(before.Height - 20, BoundsOf(Layers).Height);
        Assert.Equal(before.TopLeft, BoundsOf(Layers).TopLeft);
    }

    [AvaloniaFact]
    public void SqueezingAFloatingWindow_SnapsItToItsTitle_AndDraggingBackSnapsItToItsMinimum()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var bottom = new Point(BoundsOf(Layers).Center.X, BoundsOf(Layers).Bottom - 2);
        var squeezed = new Point(bottom.X, BoundsOf(Layers).Top + 5);

        _window.MouseDown(bottom, MouseButton.Left);
        _window.MouseMove(squeezed);
        Pump();
        _window.MouseUp(squeezed, MouseButton.Left);
        Pump();

        Assert.True(Frame(Layers).IsTitleOnly);
        Assert.Equal(_metrics.Docking.TitleLength, BoundsOf(Layers).Height);

        var titleBottom = new Point(bottom.X, BoundsOf(Layers).Bottom - 2);
        var halfwayOpen = titleBottom + new Point(0, (_metrics.WindowMinHeight - _metrics.Docking.TitleLength) / 2 + 5);
        _window.MouseDown(titleBottom, MouseButton.Left);
        _window.MouseMove(halfwayOpen);
        Pump();
        _window.MouseUp(halfwayOpen, MouseButton.Left);
        Pump();

        Assert.False(Frame(Layers).IsTitleOnly);
        Assert.Equal(_metrics.WindowMinHeight, BoundsOf(Layers).Height);
    }

    [AvaloniaFact]
    public void DraggingTheGapBetweenWidgets_ResizesThem_AndOnlyTheDropChangesTheLayout()
    {
        ShowWindow();
        var upper = BoundsOf(Layers);
        var lower = BoundsOf(History);
        var gap = new Point(upper.Center.X, (upper.Bottom + lower.Top) / 2);
        var before = LayoutJson.Write(_docking.Layout!);

        _window.MouseDown(gap, MouseButton.Left);
        _window.MouseMove(gap + new Point(0, 50));
        Pump();

        Assert.Equal(upper.Height + 50, BoundsOf(Layers).Height);
        Assert.Equal(lower.Height - 50, BoundsOf(History).Height);
        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));

        _window.MouseUp(gap + new Point(0, 50), MouseButton.Left);
        Pump();

        Assert.NotEqual(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(upper.Height + 50, BoundsOf(Layers).Height);
    }

    [AvaloniaFact]
    public void Escape_CancelsAResize()
    {
        ShowWindow();
        var upper = BoundsOf(Layers);
        var gap = new Point(upper.Center.X, upper.Bottom + 2);

        _window.MouseDown(gap, MouseButton.Left);
        _window.MouseMove(gap + new Point(0, 50));
        Pump();
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump();
        Assert.Equal(upper, BoundsOf(Layers));

        _window.MouseUp(gap + new Point(0, 50), MouseButton.Left);
        Pump();
        Assert.Equal(upper, BoundsOf(Layers));
    }

    [AvaloniaFact]
    public void DraggingASidesInnerEdge_ResizesAllItsWindows()
    {
        ShowWindow();
        var width = BoundsOf(Layers).Width;
        var edge = InnerEdge();

        _window.MouseDown(edge, MouseButton.Left);
        _window.MouseMove(edge + new Point(-60, 0));
        Pump();
        _window.MouseUp(edge + new Point(-60, 0), MouseButton.Left);
        Pump();

        Assert.Equal(width + 60, BoundsOf(Layers).Width);
        Assert.Equal(width + 60, BoundsOf(History).Width);
    }

    [AvaloniaFact]
    public void TheMenuButtonAndTheCloseButton_HideUntilThePointerIsOverTheWindow()
    {
        ShowWindow();
        var frame = Frame(Layers);
        Assert.Equal(0, frame.Closer.Opacity);
        Assert.Equal(0, frame.MenuOpener.Opacity);

        _window.MouseMove(BoundsOf(Layers).Center);
        Pump();
        Assert.Equal(TokenOpacity(), frame.Closer.Opacity);
        Assert.Equal(TokenOpacity(), frame.MenuOpener.Opacity);

        _window.MouseMove(CenterOf(frame.Closer));
        Pump();
        Assert.Equal(1, frame.Closer.Opacity);
        Assert.Equal(TokenOpacity(), frame.MenuOpener.Opacity);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.Equal(0, frame.Closer.Opacity);
    }

    [AvaloniaFact]
    public void TheCloseButton_ClosesTheWindow_AndTheWindowMenuBringsItBack()
    {
        ShowWindow();

        _window.MouseMove(BoundsOf(Layers).Center);
        Pump();
        var button = CenterOf(Frame(Layers).Closer);
        _window.MouseDown(button, MouseButton.Left);
        _window.MouseUp(button, MouseButton.Left);
        Pump();

        Assert.False(_docking.IsWindowOpen(Layers));

        _docking.ToggleWindow(Layers);
        Pump();
        Assert.True(_docking.IsWindowOpen(Layers));
    }

    [AvaloniaFact]
    public void TheMenuButton_AndRightClickingTheTitle_OpenTheSameMenu()
    {
        ShowWindow();
        var menu = Frame(Layers).TitleMenu!;

        _window.MouseMove(BoundsOf(Layers).Center);
        Pump();
        var button = CenterOf(Frame(Layers).MenuOpener);
        _window.MouseDown(button, MouseButton.Left);
        _window.MouseUp(button, MouseButton.Left);
        Pump();
        Assert.True(menu.IsOpen);
        menu.Close();
        Pump();

        var title = TitleOf(Layers);
        _window.MouseDown(title, MouseButton.Right);
        _window.MouseUp(title, MouseButton.Right);
        Pump();
        Assert.True(menu.IsOpen);
        menu.Close();
        Pump();
    }

    [AvaloniaFact]
    public void AWidgetsMenu_OffersFloatTurnIntoDockAndClose()
    {
        ShowWindow();

        var items = OpenMenu(Layers);

        Assert.Equal(
            [Label(Strings.ActionWindowFloat), Label(Strings.ActionWindowTurnIntoDock), Label(Strings.ActionWindowClose)],
            items.Select(item => item.Header));
        Assert.True(items[1].Command!.CanExecute(items[1].CommandParameter));
        Frame(Layers).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void TheMenuButton_OffersOnlyWhatApplies_AsRightClickingDoes()
    {
        ShowWindow();

        Frame(Layers).MenuOpener.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Pump();

        Assert.Equal(
            [Label(Strings.ActionWindowFloat), Label(Strings.ActionWindowTurnIntoDock), Label(Strings.ActionWindowClose)],
            Frame(Layers).TitleMenu!.Items.OfType<MenuItem>().Where(item => item.IsVisible).Select(item => item.Header));
        Frame(Layers).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void AFloatingWindowsMenu_OffersTheOtherKinds_TurnIntoWidgetAndTurnIntoDock_ThenClose()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);

        var items = OpenMenu(Layers);

        Assert.Equal(
            [Label(Strings.ActionWindowTurnIntoWidget), Label(Strings.ActionWindowTurnIntoDock), Label(Strings.ActionWindowClose)],
            items.Select(item => item.Header));
        Assert.True(items[0].Command!.CanExecute(items[0].CommandParameter));
        Frame(Layers).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void DuringADrag_NoWindowShowsItsButtons_AndAfterwardsTheyCanAgain()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);
        Assert.True(Frame(Layers).AreButtonsHidden);
        Assert.True(Frame(History).AreButtonsHidden);
        Assert.Equal(0, Frame(History).MenuOpener.Opacity);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();

        Assert.False(Frame(Layers).AreButtonsHidden);
        Assert.False(Frame(History).AreButtonsHidden);
    }

    [AvaloniaFact]
    public void DuringAResize_NoWindowShowsItsButtons()
    {
        ShowWindow();
        var edge = InnerEdge();

        _window.MouseDown(edge, MouseButton.Left);
        _window.MouseMove(edge - new Point(20, 0));
        Pump();
        Assert.True(Frame(History).AreButtonsHidden);

        _window.MouseUp(edge - new Point(20, 0), MouseButton.Left);
        Pump();
        Assert.False(Frame(History).AreButtonsHidden);
    }

    [AvaloniaFact]
    public void PastTheAppWindowsEdge_TheWindowFollowsThePointer_AndALandingPreviewShowsWhereItGoesBack()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var fromCorner = TitleOf(Layers) - BoundsOf(Layers).TopLeft;
        var outside = new Point(1100, 750);

        StartDrag(Layers, outside);

        Assert.Equal(outside - fromCorner, BoundsOf(Layers).TopLeft);
        var landing = _host.Children.OfType<WindowLandingPreview>().Single();
        Assert.True(landing.IsVisible);
        Assert.Equal(1000 - _metrics.Docking.Margin, landing.Bounds.Right);
        Assert.Equal(700 - _metrics.Docking.Margin, landing.Bounds.Bottom);

        var landsAt = landing.Bounds;
        _window.MouseUp(outside, MouseButton.Left);
        Pump();

        Assert.True(_docking.IsWindowFloating(Layers));
        Assert.Equal(landsAt, BoundsOf(Layers));
    }

    [AvaloniaFact]
    public void WhereTheWindowLandsWhereItsHeld_TheresNoLandingPreview()
    {
        ShowWindow();

        StartDrag(Layers, _middleOfNowhere);

        Assert.False(_host.Children.OfType<WindowLandingPreview>().Single().IsVisible);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AWindowAloneOnItsSide_ThatsNeverFloated_FloatsAtHalfItsHeight()
    {
        ShowWindow();
        _docking.CloseWindow(History);
        Pump();
        var before = BoundsOf(Layers);

        DragAndDrop(Layers, _middleOfNowhere);

        Assert.Equal(before.Width, BoundsOf(Layers).Width);
        Assert.Equal(before.Height / 2, BoundsOf(Layers).Height, 3);
    }

    [AvaloniaFact]
    public void DoubleClickingATitle_SqueezesTheWindowToIt_AndDoubleClickingAgainOpensIt()
    {
        ShowWindow();
        var before = BoundsOf(History);

        DoubleClick(TitleOf(History));
        Assert.Equal(_metrics.Docking.TitleLength, BoundsOf(History).Height);
        Assert.True(Frame(History).IsTitleOnly);

        DoubleClick(TitleOf(History));
        Assert.Equal(before, BoundsOf(History));
    }

    [AvaloniaFact]
    public void TheGapsLine_ShowsFaintlyUnderThePointer_AndSolidlyWhileHeld()
    {
        ShowWindow();
        var gap = _host.Children.OfType<StackResizeHandleView>().Single(handle => handle.IsAcross);
        var onTheGap = CenterOf(gap);
        Assert.Null(gap.LineBrush);
        Assert.NotNull(gap.HoverLine);
        Assert.NotNull(gap.HeldLine);

        _window.MouseMove(onTheGap);
        Pump();
        Assert.Same(gap.HoverLine, gap.LineBrush);

        _window.MouseDown(onTheGap, MouseButton.Left);
        Pump();
        Assert.Same(gap.HeldLine, gap.LineBrush);

        _window.MouseUp(onTheGap, MouseButton.Left);
        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.Null(gap.LineBrush);
    }

    [AvaloniaFact]
    public void Float_TakesAWindowOffItsSide_ALittleWayIn_AtTheSameSize()
    {
        ShowWindow();
        var before = BoundsOf(Layers);
        var item = OpenMenu(Layers)[0];

        item.Command!.Execute(item.CommandParameter);
        Pump();

        Assert.True(_docking.IsWindowFloating(Layers));
        Assert.Equal(before.Left - _metrics.WindowFloatOffset, BoundsOf(Layers).Left);
        Assert.Equal(before.Top + _metrics.WindowFloatOffset, BoundsOf(Layers).Top);
        Assert.Equal(before.Size, BoundsOf(Layers).Size);
    }

    [AvaloniaFact]
    public void TurnIntoWidget_PutsAFloatingWindowOnASide()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var item = OpenMenu(Layers)[0];

        item.Command!.Execute(item.CommandParameter);
        Pump();

        Assert.False(_docking.IsWindowFloating(Layers));
        Assert.False(Frame(Layers).IsFloating);
    }

    [AvaloniaFact]
    public void CloseInTheMenu_ClosesAFloatingWindow_AndReopeningPutsItBack()
    {
        ShowWindow();
        DragAndDrop(Layers, _middleOfNowhere);
        var where = BoundsOf(Layers);
        var item = OpenMenu(Layers)[^1];

        item.Command!.Execute(item.CommandParameter);
        Pump();
        Assert.False(_docking.IsWindowOpen(Layers));

        _docking.ToggleWindow(Layers);
        Pump();
        Assert.True(_docking.IsWindowFloating(Layers));
        Assert.Equal(where, BoundsOf(Layers));
    }

    [AvaloniaFact]
    public void TheSampleWindows_OpenAndClose_WithCheckMarks()
    {
        ShowWindow();
        var registry = new ActionRegistry([new WindowSampleActions(_docking, includeDebugActions: true)]);
        var first = registry.Get(WindowSampleActionIds.First);
        Assert.False(first.CheckedState!.IsChecked);

        first.Command.Execute(null);
        Pump();

        Assert.True(first.CheckedState.IsChecked);
        Assert.True(Frame(ShellDockingIds.SampleWindow1).IsVisible);

        first.Command.Execute(null);
        Pump();

        Assert.False(first.CheckedState.IsChecked);
        Assert.DoesNotContain(_host.Children.OfType<WindowFrame>(), frame => frame.Title == ShellDockingIds.SampleWindow1);
    }

    [AvaloniaFact]
    public void TheSampleWindows_ArentKeptOverARestart()
    {
        ShowWindow();
        _docking.ToggleWindow(ShellDockingIds.SampleWindow1);
        Pump();
        _docking.SaveAndDetach();

        ShowWindow();

        Assert.False(_docking.IsWindowOpen(ShellDockingIds.SampleWindow1));
    }

    [Fact]
    public void TheSampleWindowActions_ExistOnlyInDebugBuilds()
    {
        var docking = new ShellDocking(new FakeAppSettings(), NullLogger<ShellDocking>.Instance);

        Assert.Empty(new WindowSampleActions(docking, includeDebugActions: false).CreateActions());
    }

    [AvaloniaFact]
    public void AWindowsContents_SurviveADrag()
    {
        ShowWindow();
        var body = Frame(Layers).Body;

        DragAndDrop(Layers, _middleOfNowhere);
        Assert.Same(body, Frame(Layers).Body);

        var overHistory = new Point(BoundsOf(History).Center.X, BoundsOf(History).Top + 5);
        DragAndDrop(Layers, overHistory);
        Assert.Same(body, Frame(Layers).Body);
    }

    private WindowPlaceholderView? Placeholder => _host.Children.OfType<WindowPlaceholderView>().SingleOrDefault();

    private WindowFrame Frame(string windowId) =>
        _host.Children.OfType<WindowFrame>().Single(frame => frame.Title == windowId);

    private ToolbarFrame ToolbarFrameOf(string toolbarId) =>
        _host.Children.OfType<ToolbarFrame>().Single(frame => frame.Bar?.Name == toolbarId);

    private Rect BoundsOf(string id) => id.StartsWith("toolbar", StringComparison.Ordinal)
        ? ToolbarFrameOf(id).Bounds
        : Frame(id).Bounds;

    private Point TitleOf(string windowId) => BoundsOf(windowId).TopLeft + new Point(20, 10);

    private Point InnerEdge() => new(BoundsOf(Layers).Left - 2, BoundsOf(Layers).Center.Y);

    private Point CenterOf(Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), _host)!.Value;

    private static string Label(string text) => UiText.WithoutAccessKey(text);

    private static double TokenOpacity() =>
        Application.Current!.TryGetResource("AppWindowButtonFaintOpacity", null, out var value) ? (double)value! : -1;

    private List<MenuItem> OpenMenu(string windowId)
    {
        var title = TitleOf(windowId);
        _window.MouseDown(title, MouseButton.Right);
        _window.MouseUp(title, MouseButton.Right);
        Pump();

        return [.. Frame(windowId).TitleMenu!.Items.OfType<MenuItem>().Where(item => item.IsVisible)];
    }

    private void DoubleClick(Point at)
    {
        _window.MouseDown(at, MouseButton.Left);
        _window.MouseUp(at, MouseButton.Left);
        _window.MouseDown(at, MouseButton.Left);
        _window.MouseUp(at, MouseButton.Left);
        Pump();
    }

    private void StartDrag(string windowId, Point to)
    {
        _window.MouseDown(TitleOf(windowId), MouseButton.Left);
        _window.MouseMove(to);
        Pump();
    }

    private void DragAndDrop(string windowId, Point to)
    {
        StartDrag(windowId, to);
        _window.MouseUp(to, MouseButton.Left);
        Pump();
    }

    private void ShowWindow()
    {
        _host = new DockingHost();
        _window = new Window { Width = 1000, Height = 700, Content = _host };
        _windows.Add(_window);

        _metrics = ShellLayoutMetrics.FromResources(Application.Current!) with { MotionDuration = TimeSpan.Zero };
        ShellWindowEntry Panel(string id) => new(id, id, _metrics.WindowMinHeight, () => new Border(),
            () => WindowMenu.Create(id, _actions, _docking), _actions.Get(WindowFrameActionIds.Close).Command);

        ShellToolbarEntry Toolbar(string id) => new(id,
            () => new Border { Name = id, Width = 100, Height = 30 },
            () => ToolbarMenu.Create(id, _actions, _docking));

        var catalog = new ShellCatalog(
            [Panel(Layers), Panel(History), Panel(ShellDockingIds.SampleWindow1)],
            [
                Toolbar(ShellDockingIds.FileToolbar), Toolbar(Tools), Toolbar(ShellDockingIds.EditToolbar),
                Toolbar(ShellDockingIds.ViewToolbar)
            ]);

        _docking.Attach(_host, catalog, _metrics);

        _window.Show();
        Pump();
    }

    private static void Pump()
    {
        for (var pass = 0; pass < 5; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
