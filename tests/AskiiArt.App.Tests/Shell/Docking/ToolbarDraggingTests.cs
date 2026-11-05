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
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>Dragging toolbars with a simulated pointer, in a bare window with the default layout.</summary>
public sealed class ToolbarDraggingTests : IDisposable
{
    private const string File = ShellDockingIds.FileToolbar;
    private const string Tools = ShellDockingIds.ToolsToolbar;
    private const string Edit = ShellDockingIds.EditToolbar;

    private static readonly Point _middleOfNowhere = new(450, 350);
    private static readonly Point _leftEdge = new(20, 350);

    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;
    private readonly ActionRegistry _actions;

    private Window _window = null!;
    private DockingHost _host = null!;
    private double _toolbarWidth = 100;

    public ToolbarDraggingTests()
    {
        _docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
        _actions = new ActionRegistry([new ToolbarMenuActions(_docking)]);
    }

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void AClickOnAGrip_IsntADrag()
    {
        ShowWindow();
        var grip = GripOf(File);

        _window.MouseDown(grip, MouseButton.Left);
        _window.MouseMove(grip + new Point(1, 1));
        Pump();
        Assert.Empty(Zones);

        _window.MouseUp(grip + new Point(1, 1), MouseButton.Left);
        Pump();

        Assert.False(Placement(File).IsFloating);
        Assert.Equal(ToolbarAnchor.TopLeft, Placement(File).Anchor);
    }

    [AvaloniaFact]
    public void AButtonPressAwayFromTheGrips_StartsNoDrag()
    {
        ShowWindow();
        var middle = BoundsOf(File).Center;

        _window.MouseDown(middle, MouseButton.Left);
        _window.MouseMove(_middleOfNowhere);
        Pump();

        Assert.Empty(Zones);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void ARightPressOnAGrip_StartsNoDrag()
    {
        ShowWindow();
        var grip = GripOf(File);

        _window.MouseDown(grip, MouseButton.Right);
        _window.MouseMove(_middleOfNowhere);
        Pump();

        Assert.Empty(Zones);
        _window.MouseUp(_middleOfNowhere, MouseButton.Right);
    }

    [AvaloniaFact]
    public void StartingADrag_ShowsAZoneForEachFreeAnchor_AndItsOwn()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);

        // File, Tools, Edit and View hold 4 anchors. File's own is back, leaving 5.
        Assert.Equal(5, Zones.Count);
    }

    [AvaloniaFact]
    public void TheToolbar_FollowsThePointer_FromWhereItWasHeld()
    {
        ShowWindow();
        var grip = GripOf(File);
        var heldFromCorner = grip - BoundsOf(File).TopLeft;

        StartDrag(File, _middleOfNowhere);
        Assert.Equal(_middleOfNowhere - heldFromCorner, BoundsOf(File).TopLeft);

        var further = _middleOfNowhere + new Point(50, 30);
        _window.MouseMove(further);
        Pump();
        Assert.Equal(further - heldFromCorner, BoundsOf(File).TopLeft);
    }

    [AvaloniaFact]
    public void TheGrips_KeepShowing_ForTheWholeDrag()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);
        Assert.All(Frame(File).GetVisualDescendants().OfType<ToolbarGrip>(), grip => Assert.True(grip.DotOpacity > 0));

        _window.MouseUp(_leftEdge, MouseButton.Left);
        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.All(Frame(File).GetVisualDescendants().OfType<ToolbarGrip>(), grip => Assert.Equal(0, grip.DotOpacity));
    }

    [AvaloniaFact]
    public void BothGrips_StartADrag()
    {
        ShowWindow();

        var endGrip = Center(Frame(File).Grips[1]);
        _window.MouseDown(endGrip, MouseButton.Left);
        _window.MouseMove(_middleOfNowhere);
        Pump();

        Assert.NotEmpty(Zones);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void DroppingOnAFreeEdgeMiddle_PutsItThere_RunningAlongTheEdge()
    {
        ShowWindow();

        DragAndDrop(File, _leftEdge);

        Assert.Equal(ToolbarAnchor.LeftMiddle, Placement(File).Anchor);
        Assert.Equal(ToolbarOrientation.Vertical, Placement(File).Orientation);
        Assert.Equal(ToolbarOrientation.Vertical, Frame(File).Orientation);
        Assert.Empty(Zones);
    }

    [AvaloniaFact]
    public void OverAnEdgeMiddle_TheToolbarTurnsToShowWhatADropWouldDo_AndTurnsBackAwayFromIt()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);
        Assert.Equal(ToolbarOrientation.Vertical, Frame(File).Orientation);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.Equal(ToolbarOrientation.Horizontal, Frame(File).Orientation);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void DroppingOnACorner_KeepsItsDirection()
    {
        ShowWindow();
        _docking.SetToolbarOrientation(File, ToolbarOrientation.Vertical);
        Pump();

        DragAndDrop(File, new Point(20, _host.Bounds.Height - 20));

        Assert.Equal(ToolbarAnchor.BottomLeft, Placement(File).Anchor);
        Assert.Equal(ToolbarOrientation.Vertical, Placement(File).Orientation);
    }

    [AvaloniaFact]
    public void DroppingAnywhereElse_FloatsIt_WhereItWasLeft()
    {
        ShowWindow();

        DragAndDrop(File, _middleOfNowhere);

        Assert.True(Placement(File).IsFloating);
        var at = Placement(File).FloatingAt!.Value;
        Assert.Equal(new Point(at.X, at.Y), BoundsOf(File).TopLeft);
        Assert.InRange(at.X, _middleOfNowhere.X - 30, _middleOfNowhere.X);
    }

    [AvaloniaFact]
    public void DroppingOverAWidget_FloatsItBesideTheWidget()
    {
        ShowWindow();
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First();

        DragAndDrop(File, new Point(widgets.Bounds.Center.X, 350));

        Assert.True(Placement(File).IsFloating);
        Assert.True(BoundsOf(File).Right <= widgets.Bounds.Left);
    }

    [AvaloniaFact]
    public void WhereTheToolbarWouldLand_ShowsDuringTheDrag_AndIsWhereItLands()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);
        var previewed = Landing!.Bounds;

        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        Assert.Equal(previewed, BoundsOf(File));
    }

    [AvaloniaFact]
    public void ThePreview_IsACopyOfTheToolbar_TurnedTheWayItWouldLand()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);

        var landing = Landing!;
        Assert.True(landing.IsVisible);
        Assert.False(landing.IsHitTestVisible);
        Assert.Equal(File, landing.Toolbar.Bar!.Name);
        Assert.Equal(ToolbarOrientation.Vertical, landing.Toolbar.Orientation);
        Assert.Equal(landing.Bounds.Size, landing.Toolbar.Bounds.Size);
        Assert.Equal(landing.Bounds.Size, landing.Outline.Bounds.Size);
        Assert.Equal(new Size(100, 58), landing.Bounds.Size);

        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();
        Assert.Null(Landing);
    }

    [AvaloniaFact]
    public void ThePreview_IsntShown_WhenTheToolbarWouldLandRightWhereItIsHeld()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);

        Assert.False(Landing!.IsVisible);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void ThePreview_ShowsWhereAToolbarOverAWidgetWouldBePushedBackTo()
    {
        ShowWindow();
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First();
        var overWidgets = new Point(widgets.Bounds.Center.X, 350);

        StartDrag(File, overWidgets);

        Assert.True(Landing!.IsVisible);
        Assert.True(Landing.Bounds.Right <= widgets.Bounds.Left);
        Assert.Equal(new Size(128, 30), Landing.Bounds.Size);
        var previewed = Landing.Bounds;

        _window.MouseUp(overWidgets, MouseButton.Left);
        Pump();
        Assert.Equal(previewed, BoundsOf(File));
    }

    [AvaloniaFact]
    public void ThePreviewsOutline_KeepsMovingForAsLongAsItShows()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);
        Pump();

        Assert.True(Landing!.Outline.Pulse > 0);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AZoneUnderThePreview_FadesAway_AndComesBackWhenThePreviewGoes()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);
        Assert.DoesNotContain(Zones, zone => zone.IsCovered);

        _window.MouseMove(_leftEdge);
        Pump();
        var under = Zones.Single(zone => zone.Bounds.Contains(_leftEdge));
        Assert.True(under.IsCovered);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.False(under.IsCovered);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AZoneOverlappedByAPushedBackPreview_FadesAway_ThoughThePointerIsntOnIt()
    {
        ShowWindow();
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First();
        var overWidgets = new Point(widgets.Bounds.Center.X, _host.Bounds.Height / 2);

        StartDrag(File, overWidgets);

        Assert.Contains(Zones, zone => zone.IsCovered && zone.Bounds.Intersects(Landing!.Bounds));
        Assert.DoesNotContain(Zones, zone => zone.IsCovered && !zone.Bounds.Intersects(Landing!.Bounds));
        _window.MouseUp(overWidgets, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AStripBetweenToolbars_LeavesAGapToEach_AndStaysPutDuringTheDrag()
    {
        ShowWindow();

        StartDrag(Tools, _middleOfNowhere);
        var gap = ShellLayoutMetrics.FromResources(Application.Current!).Docking.ToolbarGap;
        var strip = Zones.Single(zone => zone.Bounds.Left > BoundsOf(File).Right && zone.Bounds.Top < 30).Bounds;

        Assert.Equal(BoundsOf(File).Right + gap, strip.Left);
        Assert.Equal(BoundsOf(Edit).Left - gap, strip.Right);

        _window.MouseMove(_leftEdge);
        Pump();
        Assert.Contains(Zones, zone => zone.Bounds == strip);

        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void ToolbarsShrinkForAStripDuringADrag_AndGetTheirLengthBackAfterIt()
    {
        _toolbarWidth = 600;
        ShowWindow();
        var before = BoundsOf(File).Width;

        StartDrag(Tools, _middleOfNowhere);
        var strip = Zones.Single(zone => zone.Bounds.Left > 100 && zone.Bounds.Top < 40).Bounds;

        Assert.True(BoundsOf(File).Width < before);
        Assert.True(BoundsOf(File).Right < strip.Left);
        Assert.True(BoundsOf(Edit).Left > strip.Right);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();

        Assert.Equal(before, BoundsOf(File).Width);
    }

    [AvaloniaFact]
    public void TheLayout_OnlyChangesOnTheDrop()
    {
        ShowWindow();
        var before = LayoutJson.Write(_docking.Layout!);
        var changes = 0;
        _docking.LayoutChanged += (_, _) => changes++;

        StartDrag(File, _middleOfNowhere);
        _window.MouseMove(_leftEdge);
        Pump();

        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(0, changes);

        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        Assert.NotEqual(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void TheSafeArea_HoldsStillDuringADrag_AndChangesOnTheDrop()
    {
        ShowWindow();
        _docking.ToggleToolbar(Edit);
        Pump();
        var before = _host.SafeArea;
        Assert.True(before.Top > 0);

        StartDrag(File, _middleOfNowhere);
        Assert.Equal(before, _host.SafeArea);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();

        Assert.Equal(0, _host.SafeArea.Top);
    }

    [AvaloniaFact]
    public void Escape_CancelsTheDrag_AndTheReleaseThatFollowsDoesNothing()
    {
        ShowWindow();
        var before = LayoutJson.Write(_docking.Layout!);
        var where = BoundsOf(File);

        StartDrag(File, _middleOfNowhere);
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump();

        Assert.Empty(Zones);
        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
        Assert.Equal(where, BoundsOf(File));

        _window.MouseMove(_leftEdge);
        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
    }

    [AvaloniaFact]
    public void CancellingOverAnEdgeMiddle_GivesTheToolbarItsOwnShapeBack()
    {
        ShowWindow();
        var where = BoundsOf(File);

        StartDrag(File, _leftEdge);
        Assert.Equal(ToolbarOrientation.Vertical, Frame(File).Orientation);

        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        Assert.Equal(ToolbarOrientation.Horizontal, Frame(File).Orientation);
        Assert.Equal(where, BoundsOf(File));
    }

    [AvaloniaFact]
    public void ADragAfterACancelledTurn_HoldsTheToolbarWhereItWouldLand()
    {
        ShowWindow();
        StartDrag(File, _leftEdge);
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();

        StartDrag(File, _middleOfNowhere);

        Assert.False(Landing!.IsVisible);
        Assert.True(BoundsOf(File).Contains(_middleOfNowhere));
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void Escape_WithNoDrag_IsLeftAlone()
    {
        ShowWindow();
        var seen = false;
        _window.KeyDown += (_, e) => seen = e.Key == Key.Escape;

        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);

        Assert.True(seen);
    }

    [AvaloniaFact]
    public void TheAppLosingFocus_CancelsTheDrag()
    {
        ShowWindow();
        var before = LayoutJson.Write(_docking.Layout!);

        StartDrag(File, _middleOfNowhere);
        _window.Hide();
        Pump();

        Assert.Empty(Zones);
        Assert.Equal(before, LayoutJson.Write(_docking.Layout!));
    }

    [AvaloniaFact]
    public void AChangeFromElsewhereDuringADrag_CancelsIt()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);
        _docking.ResetLayout();
        Pump();

        Assert.Empty(Zones);
        Assert.False(Placement(File).IsFloating);
    }

    [AvaloniaFact]
    public void ClosingTheWindowDuringADrag_SavesTheLayoutFromBeforeIt()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);
        _docking.SaveAndDetach();

        var saved = LayoutJson.TryRead(_settings.GetText(ShellDocking.LayoutSettingKey)!);
        Assert.False(saved!.Toolbars.Single(toolbar => toolbar.ToolbarId == File).IsFloating);
    }

    [AvaloniaFact]
    public void AFloatingToolbar_SurvivesARestart()
    {
        ShowWindow();
        DragAndDrop(File, _middleOfNowhere);
        var where = BoundsOf(File);
        _docking.SaveAndDetach();

        ShowWindow();

        Assert.Equal(where.TopLeft, BoundsOf(File).TopLeft);
    }

    [AvaloniaFact]
    public void AFloatingToolbar_CanBeDraggedBackToAnAnchor()
    {
        ShowWindow();
        DragAndDrop(File, _middleOfNowhere);

        DragAndDrop(File, new Point(20, 20));

        Assert.False(Placement(File).IsFloating);
        Assert.Equal(ToolbarAnchor.TopLeft, Placement(File).Anchor);
    }

    [AvaloniaFact]
    public void ShrinkingTheWindow_KeepsAFloatingToolbarInside_AndRegrowingPutsItBack()
    {
        ShowWindow();
        DragAndDrop(File, new Point(800, 500));
        var where = BoundsOf(File).TopLeft;

        _window.Width = 400;
        _window.Height = 300;
        Pump();
        Assert.True(BoundsOf(File).Right <= 400);
        Assert.True(BoundsOf(File).Bottom <= 300);

        _window.Width = 1000;
        _window.Height = 700;
        Pump();
        Assert.Equal(where, BoundsOf(File).TopLeft);
    }

    [AvaloniaFact]
    public void ALaterFloatingToolbar_DrawsOverAnEarlierOne()
    {
        ShowWindow();
        DragAndDrop(File, _middleOfNowhere);
        DragAndDrop(Tools, _middleOfNowhere + new Point(30, 5));
        Assert.True(Frame(Tools).ZIndex > Frame(File).ZIndex);

        DragAndDrop(File, _middleOfNowhere + new Point(5, 5));
        Assert.True(Frame(File).ZIndex > Frame(Tools).ZIndex);
    }

    [AvaloniaFact]
    public void ShowingAToolbarWhoseAnchorWasTaken_FloatsItThere()
    {
        ShowWindow();
        var where = BoundsOf(Edit).TopLeft;
        _docking.ToggleToolbar(Edit);
        Pump();
        DragAndDrop(File, new Point(_host.Bounds.Width - 20 - 240, 20));
        Assert.Equal(ToolbarAnchor.TopRight, Placement(File).Anchor);

        _docking.ToggleToolbar(Edit);
        Pump();

        Assert.True(Placement(Edit).IsFloating);
        Assert.Equal(where, BoundsOf(Edit).TopLeft);
        Assert.Equal(ToolbarAnchor.TopRight, Placement(File).Anchor);
    }

    [AvaloniaFact]
    public void TheMenu_ChangesACornerToolbarsDirection()
    {
        ShowWindow();
        var menu = Frame(File).ContextMenu!;
        var vertical = (MenuItem)menu.Items[1]!;

        vertical.Command!.Execute(vertical.CommandParameter);
        Pump();

        Assert.Equal(ToolbarOrientation.Vertical, Placement(File).Orientation);
        Assert.Equal(ToolbarOrientation.Vertical, Frame(File).Orientation);
        Assert.Equal(ToolbarAnchor.TopLeft, Placement(File).Anchor);
    }

    [AvaloniaFact]
    public void TheMenu_GreysOutDirections_OnAnEdgeMiddle()
    {
        ShowWindow();
        var horizontal = (MenuItem)Frame(Tools).ContextMenu!.Items[0]!;
        var vertical = (MenuItem)Frame(Tools).ContextMenu!.Items[1]!;
        Pump();

        Assert.False(horizontal.Command!.CanExecute(horizontal.CommandParameter));
        Assert.False(vertical.Command!.CanExecute(vertical.CommandParameter));

        DragAndDrop(Tools, _middleOfNowhere + new Point(30, 5));

        Assert.True(horizontal.Command.CanExecute(horizontal.CommandParameter));
        Assert.True(vertical.Command.CanExecute(vertical.CommandParameter));
    }

    [AvaloniaFact]
    public void TheMenu_ChangesAFloatingToolbarsDirection_AndItStaysWhereItIs()
    {
        ShowWindow();
        DragAndDrop(File, _middleOfNowhere);
        var where = BoundsOf(File).TopLeft;
        var vertical = (MenuItem)Frame(File).ContextMenu!.Items[1]!;

        vertical.Command!.Execute(vertical.CommandParameter);
        Pump();

        Assert.Equal(ToolbarOrientation.Vertical, Frame(File).Orientation);
        Assert.Equal(where, BoundsOf(File).TopLeft);
    }

    [AvaloniaFact]
    public void TheMenu_HidesTheToolbar()
    {
        ShowWindow();
        var hide = (MenuItem)Frame(File).ContextMenu!.Items[^1]!;

        Assert.Equal(UiText.WithoutAccessKey(Strings.ActionToolbarHide), hide.Header);
        hide.Command!.Execute(hide.CommandParameter);
        Pump();

        Assert.False(_docking.IsToolbarVisible(File));
        Assert.DoesNotContain(_host.Children.OfType<ToolbarFrame>(), frame => frame.Bar?.Name == File);
    }

    [AvaloniaFact]
    public void ARightClick_OpensTheMenu_WithTheToolbarsDirectionMarked()
    {
        ShowWindow();
        var menu = Frame(File).ContextMenu!;
        var onToolbar = GripOf(File);

        _window.MouseDown(onToolbar, MouseButton.Right);
        _window.MouseUp(onToolbar, MouseButton.Right);
        Pump();

        Assert.True(menu.IsOpen);
        Assert.True(((MenuItem)menu.Items[0]!).IsChecked);
        Assert.False(((MenuItem)menu.Items[1]!).IsChecked);

        menu.Close();
        Pump();
    }

    private List<DropZoneView> Zones => [.. _host.Children.OfType<DropZoneView>()];

    private ToolbarLandingPreview? Landing => _host.Children.OfType<ToolbarLandingPreview>().SingleOrDefault();

    private ToolbarPlacement Placement(string toolbarId) =>
        _docking.Layout!.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId);

    private ToolbarFrame Frame(string toolbarId) =>
        _host.Children.OfType<ToolbarFrame>().Single(frame => frame.Bar?.Name == toolbarId);

    private Rect BoundsOf(string toolbarId) => Frame(toolbarId).Bounds;

    private Point GripOf(string toolbarId) => Center(Frame(toolbarId).Grips[0]);

    private Point Center(Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), _host)!.Value;

    private void StartDrag(string toolbarId, Point to)
    {
        _window.MouseDown(GripOf(toolbarId), MouseButton.Left);
        _window.MouseMove(to);
        Pump();
    }

    private void DragAndDrop(string toolbarId, Point to)
    {
        StartDrag(toolbarId, to);
        _window.MouseUp(to, MouseButton.Left);
        Pump();
    }

    private void ShowWindow()
    {
        _host = new DockingHost();
        _window = new Window { Width = 1000, Height = 700, Content = _host };
        _windows.Add(_window);

        var metrics = ShellLayoutMetrics.FromResources(Application.Current!) with { MotionDuration = TimeSpan.Zero };
        ShellWindowEntry Window(string id) => new(id, id, metrics.WindowMinHeight, () => new Border());

        ShellToolbarEntry Toolbar(string id) => new(id,
            () => new Border { Name = id, Width = _toolbarWidth, Height = 30 },
            () => ToolbarMenu.Create(id, _actions, _docking));

        var catalog = new ShellCatalog(
            [Window(ShellDockingIds.LayersPanel), Window(ShellDockingIds.HistoryPanel)],
            [Toolbar(File), Toolbar(Tools), Toolbar(Edit), Toolbar(ShellDockingIds.ViewToolbar)]);

        _docking.Attach(_host, catalog, metrics);

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
