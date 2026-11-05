// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
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

/// <summary>Sub-toolbars in a bare window with the default layout, using a simulated pointer for drags.</summary>
public sealed class SubToolbarTests : IDisposable
{
    private const string File = ShellDockingIds.FileToolbar;
    private const string Tools = ShellDockingIds.ToolsToolbar;
    private const string Sub = "sub";

    private static readonly Point _middleOfNowhere = new(450, 350);
    private static readonly Point _leftEdge = new(20, 350);

    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;
    private readonly ActionRegistry _actions;

    private Window _window = null!;
    private DockingHost _host = null!;
    private double _gap;

    public SubToolbarTests()
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
    public void ItShowsBelowATopToolbar_AlignedToTheCorner_AGapAway()
    {
        ShowWindow();

        ShowSubToolbar(File);

        Assert.Equal(BoundsOf(File).Left, SubBounds.Left);
        Assert.Equal(BoundsOf(File).Bottom + _gap, SubBounds.Top);
    }

    [AvaloniaFact]
    public void ItShowsAboveABottomToolbar_Centred()
    {
        ShowWindow();

        ShowSubToolbar(Tools);

        Assert.Equal(BoundsOf(Tools).Top - _gap, SubBounds.Bottom);
        Assert.Equal(BoundsOf(Tools).Center.X, SubBounds.Center.X, 3);
    }

    [AvaloniaFact]
    public void ItHasNoGrips()
    {
        ShowWindow();

        ShowSubToolbar(File);

        Assert.All(SubFrame.GetVisualDescendants().OfType<ToolbarGrip>(), grip => Assert.False(grip.IsVisible));
    }

    [AvaloniaFact]
    public void ItGoesAboveToolbars_AndBelowFloatingOnes()
    {
        ShowWindow();

        ShowSubToolbar(File);
        Assert.True(SubFrame.ZIndex > Frame(Tools).ZIndex);

        DragAndDrop(Tools, _middleOfNowhere);
        Assert.True(SubFrame.ZIndex < Frame(Tools).ZIndex);
    }

    [AvaloniaFact]
    public void OnlyOneShowsAtATime()
    {
        ShowWindow();

        ShowSubToolbar(File);
        ShowSubToolbar(Tools);

        Assert.Single(_host.Children.OfType<ToolbarFrame>(), frame => frame.Bar?.Name == Sub);
        Assert.Equal(Tools, _docking.SubToolbarParent);
        Assert.Equal(BoundsOf(Tools).Top - _gap, SubBounds.Bottom);
    }

    [AvaloniaFact]
    public void HidingIt_TakesItAway()
    {
        ShowWindow();
        ShowSubToolbar(File);

        _docking.HideSubToolbar();
        Pump();

        Assert.DoesNotContain(_host.Children.OfType<ToolbarFrame>(), frame => frame.Bar?.Name == Sub);
        Assert.Null(_docking.SubToolbarParent);
    }

    [AvaloniaFact]
    public void ItGoesWithItsParent_WhenTheParentIsHidden_AndComesBackWithIt()
    {
        ShowWindow();
        ShowSubToolbar(File);

        _docking.ToggleToolbar(File);
        Pump();
        Assert.Equal(default, SubBounds.Size);

        _docking.ToggleToolbar(File);
        Pump();
        Assert.NotEqual(default, SubBounds.Size);
    }

    [AvaloniaFact]
    public void ItPushesAToolbarOnTheNeighbouringEdge_AndItMovesBackAfterwards()
    {
        ShowWindow();
        _docking.ToggleToolbar(File);
        DragAndDrop(Tools, _leftEdge);
        var before = BoundsOf(Tools);
        _docking.ToggleToolbar(File);
        Pump();

        _docking.SetToolbarOrientation(File, ToolbarOrientation.Horizontal);
        ShowSubToolbar(File, height: 400);
        Assert.Equal(SubBounds.Bottom + _gap, BoundsOf(Tools).Top);

        _docking.HideSubToolbar();
        Pump();
        Assert.Equal(before, BoundsOf(Tools));
    }

    [AvaloniaFact]
    public void ItNeverMovesTheCanvas()
    {
        ShowWindow();
        var before = _host.SafeArea;

        ShowSubToolbar(File);

        Assert.Equal(before, _host.SafeArea);
    }

    [AvaloniaFact]
    public void ItIsntSaved()
    {
        ShowWindow();
        var without = LayoutJson.Write(_docking.Layout!);

        ShowSubToolbar(File);

        Assert.Equal(without, LayoutJson.Write(_docking.Layout!));
    }

    [AvaloniaFact]
    public void ItFollowsItsParentThroughADrag()
    {
        ShowWindow();
        ShowSubToolbar(File);

        StartDrag(File, _middleOfNowhere);

        Assert.Equal(BoundsOf(File).Bottom + _gap, SubBounds.Top);
        Assert.Equal(BoundsOf(File).Center.X, SubBounds.Center.X, 3);
        Assert.Equal(1100, SubFrame.ZIndex);

        var further = _middleOfNowhere + new Point(40, 25);
        _window.MouseMove(further);
        Pump();
        Assert.Equal(BoundsOf(File).Bottom + _gap, SubBounds.Top);

        _window.MouseUp(further, MouseButton.Left);
        Pump();
    }

    [AvaloniaFact]
    public void OverAZone_ItTurnsAndGoesSeeThroughWithItsParent()
    {
        ShowWindow();
        ShowSubToolbar(File);

        StartDrag(File, _leftEdge);

        Assert.Equal(ToolbarOrientation.Vertical, SubFrame.Orientation);
        Assert.True(SubFrame.IsAnchoring);
        Assert.Equal(BoundsOf(File).Right + _gap, SubBounds.Left);

        _window.MouseMove(_middleOfNowhere);
        Pump();
        Assert.False(SubFrame.IsAnchoring);
        Assert.Equal(ToolbarOrientation.Horizontal, SubFrame.Orientation);

        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
        Pump();
    }

    [AvaloniaFact]
    public void DroppingOnAZone_PutsItOnTheCanvasSide()
    {
        ShowWindow();
        ShowSubToolbar(File);

        DragAndDrop(File, _leftEdge);

        Assert.Equal(ToolbarOrientation.Vertical, SubFrame.Orientation);
        Assert.False(SubFrame.IsAnchoring);
        Assert.Equal(BoundsOf(File).Right + _gap, SubBounds.Left);
    }

    [AvaloniaFact]
    public void CancellingADrag_PutsItBack()
    {
        ShowWindow();
        ShowSubToolbar(File);
        var before = SubBounds;

        StartDrag(File, _leftEdge);
        _window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Pump();

        Assert.Equal(before, SubBounds);
        Assert.Equal(ToolbarOrientation.Horizontal, SubFrame.Orientation);
        Assert.False(SubFrame.IsAnchoring);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void PulledFromTheBottom_ItStaysAboveTheFloatingToolbar()
    {
        ShowWindow();
        ShowSubToolbar(Tools);

        DragAndDrop(Tools, _middleOfNowhere);

        Assert.True(Placement(Tools).IsFloating);
        Assert.Equal(SubToolbarSide.AboveOrLeft, Placement(Tools).SubToolbarSide);
        Assert.Equal(BoundsOf(Tools).Top - _gap, SubBounds.Bottom);
    }

    [AvaloniaFact]
    public void PulledFromTheTop_ItStaysBelowTheFloatingToolbar_UnlessThereIsNoRoom()
    {
        ShowWindow();
        ShowSubToolbar(File);

        DragAndDrop(File, _middleOfNowhere);
        Assert.Equal(BoundsOf(File).Bottom + _gap, SubBounds.Top);

        var nearTheBottom = new Point(_middleOfNowhere.X, _host.Bounds.Height - 15);
        DragAndDrop(File, nearTheBottom);
        Assert.Equal(BoundsOf(File).Top - _gap, SubBounds.Bottom);
        Assert.Equal(SubToolbarSide.BelowOrRight, Placement(File).SubToolbarSide);
    }

    [AvaloniaFact]
    public void PushedBackInsideTheRegion_ACopyOfItShowsWhereItWouldLand()
    {
        ShowWindow();
        ShowSubToolbar(File);
        var widgets = _host.GetVisualDescendants().OfType<WindowFrame>().First();
        var overWidgets = new Point(widgets.Bounds.Center.X, 350);

        StartDrag(File, overWidgets);

        var landings = _host.Children.OfType<ToolbarLandingPreview>().Where(landing => landing.IsVisible).ToList();
        Assert.Equal(2, landings.Count);
        var copy = landings.Single(landing => landing.Toolbar.Bar?.Name == Sub);
        Assert.True(copy.ShowsCopy);
        var previewed = copy.Bounds;

        _window.MouseUp(overWidgets, MouseButton.Left);
        Pump();

        Assert.Equal(previewed, SubBounds);
        Assert.Empty(_host.Children.OfType<ToolbarLandingPreview>());
    }

    [AvaloniaFact]
    public void OverAZone_OnlyTheZonesOutlineShows()
    {
        ShowWindow();
        ShowSubToolbar(File);

        StartDrag(File, _leftEdge);

        var visible = _host.Children.OfType<ToolbarLandingPreview>().Single(landing => landing.IsVisible);
        Assert.False(visible.ShowsCopy);
        _window.MouseUp(_leftEdge, MouseButton.Left);
    }

    [AvaloniaFact]
    public void AnotherToolbarsDrag_LeavesItWhereItIs()
    {
        ShowWindow();
        ShowSubToolbar(File);
        var before = SubBounds;

        StartDrag(Tools, _middleOfNowhere);

        Assert.Equal(before, SubBounds);
        Assert.DoesNotContain(_host.Children.OfType<DropZoneView>(), zone =>
            zone.Bounds.Intersects(SubBounds));
        _window.MouseUp(_middleOfNowhere, MouseButton.Left);
    }

    [AvaloniaFact]
    public void TheSampleActions_ShowAndHideIt_WithCheckMarks()
    {
        ShowWindow();
        var registry = new ActionRegistry([new SubToolbarSampleActions(_docking, includeDebugActions: true)]);
        var onFile = registry.Get(SubToolbarSampleActionIds.OnFile);
        var onTools = registry.Get(SubToolbarSampleActionIds.OnTools);

        onFile.Command.Execute(null);
        Pump();
        Assert.Equal(File, _docking.SubToolbarParent);
        Assert.True(onFile.CheckedState!.IsChecked);

        onTools.Command.Execute(null);
        Pump();
        Assert.Equal(Tools, _docking.SubToolbarParent);
        Assert.False(onFile.CheckedState!.IsChecked);
        Assert.True(onTools.CheckedState!.IsChecked);

        onTools.Command.Execute(null);
        Pump();
        Assert.Null(_docking.SubToolbarParent);
    }

    [AvaloniaFact]
    public void TheLongSample_IsLongerAndTakesEffectAtOnce()
    {
        ShowWindow();
        var registry = new ActionRegistry([new SubToolbarSampleActions(_docking, includeDebugActions: true)]);

        registry.Get(SubToolbarSampleActionIds.OnFile).Command.Execute(null);
        Pump();
        var bar = () =>
            _host.Children.OfType<ToolbarFrame>().Single(frame => frame.Bar?.Name != File && frame.Bar is StackPanel);
        var shortButtons = ((StackPanel)bar().Bar!).Children.Count;

        registry.Get(SubToolbarSampleActionIds.Long).Command.Execute(null);
        Pump();

        Assert.True(registry.Get(SubToolbarSampleActionIds.Long).CheckedState!.IsChecked);
        Assert.True(((StackPanel)bar().Bar!).Children.Count > shortButtons);
    }

    [AvaloniaFact]
    public void TheSampleActions_ExistOnlyInDebugBuilds()
    {
        var docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);

        Assert.Empty(new SubToolbarSampleActions(docking, includeDebugActions: false).CreateActions());
        Assert.NotEmpty(new SubToolbarSampleActions(docking, includeDebugActions: true).CreateActions());
    }

    private ToolbarFrame SubFrame => _host.Children.OfType<ToolbarFrame>().Single(frame => frame.Bar?.Name == Sub);

    private Rect SubBounds => SubFrame.Bounds;

    private void ShowSubToolbar(string parent, double width = 80, double height = 30)
    {
        _docking.ShowSubToolbar(parent, () => new Border { Name = Sub, Width = width, Height = height });
        Pump();
    }

    private ToolbarPlacement Placement(string toolbarId) =>
        _docking.Layout!.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId);

    private ToolbarFrame Frame(string toolbarId) =>
        _host.Children.OfType<ToolbarFrame>().Single(frame => frame.Bar?.Name == toolbarId);

    private Rect BoundsOf(string toolbarId) => Frame(toolbarId).Bounds;

    private Point GripOf(string toolbarId)
    {
        var grip = Frame(toolbarId).Grips[0];
        return grip.TranslatePoint(new Point(grip.Bounds.Width / 2, grip.Bounds.Height / 2), _host)!.Value;
    }

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
        _gap = metrics.Docking.ToolbarGap;

        ShellWindowEntry Window(string id) => new(id, id, metrics.WindowMinHeight, () => new Border());

        ShellToolbarEntry Toolbar(string id) => new(id,
            () => new Border { Name = id, Width = 100, Height = 30 },
            () => ToolbarMenu.Create(id, _actions, _docking));

        var catalog = new ShellCatalog(
            [Window(ShellDockingIds.LayersPanel), Window(ShellDockingIds.HistoryPanel)],
            [
                Toolbar(File), Toolbar(Tools), Toolbar(ShellDockingIds.EditToolbar),
                Toolbar(ShellDockingIds.ViewToolbar)
            ]);

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
