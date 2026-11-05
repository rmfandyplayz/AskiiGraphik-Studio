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
    private const string View = ShellDockingIds.ViewToolbar;

    private static readonly Point _middleOfNowhere = new(450, 350);
    private static readonly Point _leftEdge = new(20, 350);

    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;
    private readonly ActionRegistry _actions;

    private Window _window = null!;
    private DockingHost _host = null!;
    private double _toolbarWidth = 100;
    private TimeSpan _motionDuration = TimeSpan.Zero;

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
    public void OverAZone_TheOutlineGoesAroundTheZone_WithNoCopyOfTheToolbar()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);

        var strip = Zones.Single(zone => zone.Bounds.Contains(_leftEdge));
        var landing = Landing!;
        Assert.True(landing.IsVisible);
        Assert.False(landing.IsHitTestVisible);
        Assert.False(landing.ShowsCopy);
        Assert.Equal(strip.Bounds, landing.Bounds);
        Assert.Equal(landing.Bounds.Size, landing.Outline.Bounds.Size);

        _window.MouseUp(_leftEdge, MouseButton.Left);
        Pump();
        Assert.Null(Landing);
    }

    [AvaloniaFact]
    public void OverACornerSquare_TheOutlineGoesAroundTheSquare()
    {
        ShowWindow();
        var corner = new Point(20, _host.Bounds.Height - 20);

        StartDrag(File, corner);

        Assert.Equal(Zones.Single(zone => zone.Bounds.Contains(corner)).Bounds, Landing!.Bounds);
        Assert.False(Landing.ShowsCopy);
        _window.MouseUp(corner, MouseButton.Left);
    }

    [AvaloniaFact]
    public void OverAZone_TheDraggedToolbarGoesSeeThrough_AndComesBackAwayFromIt()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);
        Assert.False(Frame(File).IsAnchoring);

        _window.MouseMove(_leftEdge);
        Pump();
        Assert.True(Frame(File).IsAnchoring);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.False(Frame(File).IsAnchoring);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void TheDraggedToolbar_IsSolidAgainAfterTheDrop()
    {
        ShowWindow();

        DragAndDrop(File, _leftEdge);

        Assert.False(Frame(File).IsAnchoring);
    }

    [AvaloniaFact]
    public void TheDraggedToolbar_IsSolidAgainAfterCancelling()
    {
        ShowWindow();

        StartDrag(File, _leftEdge);
        _window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Pump();

        Assert.False(Frame(File).IsAnchoring);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void StartingADrag_HidesEveryOtherToolbarAtOnce()
    {
        _motionDuration = TimeSpan.FromSeconds(10);
        ShowWindow();

        StartDrag(Tools, _middleOfNowhere);

        Assert.All([File, Edit, View], toolbarId =>
        {
            Assert.True(Frame(toolbarId).IsHiddenForDrag);
            Assert.Equal(0, Frame(toolbarId).Opacity);
            Assert.False(Frame(toolbarId).IsHitTestVisible);
        });
        Assert.False(Frame(Tools).IsHiddenForDrag);
        Assert.Equal(1, Frame(Tools).Opacity);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AFloatingToolbar_IsHiddenDuringADragToo()
    {
        ShowWindow();
        DragAndDrop(File, _middleOfNowhere);

        StartDrag(Tools, _leftEdge);

        Assert.True(Frame(File).IsHiddenForDrag);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void TheDrop_BringsTheOthersBack_AndTheNextDragHidesThemAgain()
    {
        ShowWindow();

        DragAndDrop(Tools, _leftEdge);
        Assert.False(Frame(File).IsHiddenForDrag);
        Assert.True(Frame(File).IsHitTestVisible);

        StartDrag(File, _middleOfNowhere);
        Assert.True(Frame(Tools).IsHiddenForDrag);
        Assert.False(Frame(File).IsHiddenForDrag);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void Cancelling_BringsTheOthersBack()
    {
        ShowWindow();

        StartDrag(Tools, _middleOfNowhere);
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();

        Assert.False(Frame(File).IsHiddenForDrag);
    }

    [AvaloniaFact]
    public void AfterADrag_ZonesFadeOutFirst_AndTheOthersFadeBackInTheirPlaces()
    {
        _motionDuration = TimeSpan.FromMilliseconds(40);
        ShowWindow();
        var where = BoundsOf(File);

        StartDrag(Tools, _leftEdge);
        _window.MouseUp(_leftEdge, MouseButton.Left);

        // They only go on a later frame, so this can't be too late however slow the machine is.
        Assert.NotEmpty(Zones);
        Assert.All(Zones, zone => Assert.False(zone.IsHitTestVisible));

        Thread.Sleep(60);
        Pump();

        Assert.Empty(Zones);
        Assert.Null(Landing);
        Assert.Equal(where, BoundsOf(File));
    }

    [AvaloniaFact]
    public void TheOthers_DontSlide_WhenTheyComeBack_ThoughTheHeldToolbarDoes()
    {
        _toolbarWidth = 600;
        _motionDuration = TimeSpan.FromSeconds(10);
        ShowWindow();
        var file = BoundsOf(File);
        var tools = BoundsOf(Tools);

        // A drop on the top strip would squeeze File, so it's out of place until the drag is cancelled.
        var topStrip = new Point(_host.Bounds.Width / 3, 20);
        StartDrag(Tools, topStrip);
        Assert.True(BoundsOf(File).Width < file.Width);

        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(topStrip, MouseButton.Left);
        Pump();

        Assert.Equal(file, BoundsOf(File));
        Assert.NotEqual(tools, BoundsOf(Tools));
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
        Assert.True(Landing.ShowsCopy);
        Assert.False(Frame(File).IsAnchoring);
        Assert.Equal(File, Landing.Toolbar.Bar!.Name);
        Assert.True(Landing.Bounds.Right <= widgets.Bounds.Left);
        Assert.Equal(new Size(128, 30), Landing.Bounds.Size);
        Assert.Equal(Landing.Bounds.Size, Landing.Toolbar.Bounds.Size);
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
        var ranSoFar = Landing!.Outline.DistanceRun;
        Thread.Sleep(5);
        Pump();

        Assert.True(Landing!.Outline.DistanceRun > ranSoFar);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void TheZoneUnderThePointer_GivesWayToTheOutline_AndComesBackWhenThePointerLeaves()
    {
        ShowWindow();

        StartDrag(File, _middleOfNowhere);
        Assert.DoesNotContain(Zones, zone => zone.IsOutlined);

        _window.MouseMove(_leftEdge);
        Pump();
        var under = Zones.Single(zone => zone.Bounds.Contains(_leftEdge));
        Assert.True(under.IsOutlined);
        Assert.False(under.IsDimmed);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.False(under.IsOutlined);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AZoneOverlappedByAPushedBackPreview_Dims_ButStaysVisible()
    {
        _motionDuration = TimeSpan.Zero;
        ShowWindow();
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First();
        var overWidgets = new Point(widgets.Bounds.Center.X, _host.Bounds.Height / 2);

        StartDrag(File, overWidgets);
        Pump();

        var dimmed = Zones.Where(zone => zone.IsDimmed).ToList();
        Assert.NotEmpty(dimmed);
        Assert.All(dimmed, zone => Assert.True(zone.Bounds.Intersects(Landing!.Bounds)));
        Assert.DoesNotContain(Zones, zone => !zone.IsDimmed && zone.Bounds.Intersects(Landing!.Bounds));

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.DoesNotContain(Zones, zone => zone.IsDimmed);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AStrip_StaysPutDuringTheDrag_AndIsCentredOnItsEdge()
    {
        ShowWindow();

        StartDrag(Tools, _middleOfNowhere);
        var margin = ShellLayoutMetrics.FromResources(Application.Current!).Docking.Margin;
        var strip = Zones.Single(zone => zone.Bounds.Top == margin && zone.Bounds.Width > zone.Bounds.Height * 3)
            .Bounds;
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First().Bounds;

        Assert.Equal(strip.Left - margin, widgets.Left - margin - strip.Right, 3);

        _window.MouseMove(_leftEdge);
        Pump();
        Assert.Contains(Zones, zone => zone.Bounds == strip);

        _window.MouseUp(_leftEdge, MouseButton.Left);
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

        var metrics = ShellLayoutMetrics.FromResources(Application.Current!) with { MotionDuration = _motionDuration };
        ShellWindowEntry Window(string id) => new(id, id, metrics.WindowMinHeight, () => new Border());

        ShellToolbarEntry Toolbar(string id) => new(id,
            () => new Border { Name = id, Width = _toolbarWidth, Height = 30 },
            () => ToolbarMenu.Create(id, _actions, _docking));

        var catalog = new ShellCatalog(
            [Window(ShellDockingIds.LayersPanel), Window(ShellDockingIds.HistoryPanel)],
            [Toolbar(File), Toolbar(Tools), Toolbar(Edit), Toolbar(View)]);

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
