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

/// <summary>Docks' tabs, menus, conversions and borders, with a simulated pointer, starting from a saved layout.</summary>
public sealed class DockTests : IDisposable
{
    private const string Layers = ShellDockingIds.LayersPanel;
    private const string History = ShellDockingIds.HistoryPanel;
    private const string Sample = ShellDockingIds.SampleWindow1;

    private static readonly Point _middleOfNowhere = new(400, 350);

    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;
    private readonly ActionRegistry _actions;

    private Window _window = null!;
    private DockingHost _host = null!;
    private ShellLayoutMetrics _metrics = null!;

    public DockTests()
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
    public void ADockWindow_HasATabStrip_InPlaceOfItsTitleAndButtons_AndOnlyTheSelectedTabShows()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));

        Assert.True(Frame(Layers).IsDocked);
        Assert.True(Frame(Layers).Tabs.IsEffectivelyVisible);
        Assert.False(Frame(Layers).MenuOpener.IsEffectivelyVisible);
        Assert.False(Frame(Layers).Closer.IsEffectivelyVisible);
        Assert.Equal([Layers, History], Frame(Layers).Tabs.WindowIds);
        Assert.True(Frame(Layers).IsVisible);
        Assert.False(Frame(History).IsVisible);
    }

    [AvaloniaFact]
    public void ClickingATab_ShowsItsWindow()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));

        Click(TabOf(History));

        Assert.False(Frame(Layers).IsVisible);
        Assert.True(Frame(History).IsVisible);
        Assert.Equal(History, _docking.Layout!.GroupHolding(History)!.Selected);
    }

    [AvaloniaFact]
    public void DraggingATabThatIsntShowing_FloatsJustThatWindow()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));

        DragAndDrop(TabOf(History), _middleOfNowhere);

        Assert.True(_docking.IsWindowFloating(History));
        Assert.False(Frame(History).IsDocked);
        Assert.Equal(WindowKind.Dock, _docking.WindowKindOf(Layers));
        Assert.True(Frame(Layers).IsVisible);
    }

    [AvaloniaFact]
    public void ATabDraggedAlongItsOwnStrip_MovesThere_EvenThoughTheWindowThatWasShowingHides()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));
        var strip = StripOf(Layers);
        var stripStart = new Point(strip.Left + 3, strip.Center.Y);

        // Over its own strip, the held tab is the selected one, so the frame that has the pointer hides.
        _window.MouseDown(TabOf(History), MouseButton.Left);
        _window.MouseMove(stripStart);
        Pump();
        Assert.False(Frame(Layers).IsVisible);
        _window.MouseMove(stripStart + new Point(1, 0));
        _window.MouseUp(stripStart + new Point(1, 0), MouseButton.Left);
        Pump();

        Assert.Equal([History, Layers], _docking.Layout!.GroupHolding(History)!.WindowIds);
        Assert.Equal(History, _docking.Layout!.GroupHolding(History)!.Selected);
    }

    [AvaloniaFact]
    public void AWindowDroppedInTheMiddleOfADockWindow_BecomesItsLastTab()
    {
        ShowWindow(Dock(Column(Tabs(Layers))));
        _docking.ToggleWindow(Sample);
        Pump();

        DragAndDrop(TitleRowOf(Sample), BoundsOf(Layers).Center);

        Assert.Equal([Layers, Sample], _docking.Layout!.GroupHolding(Sample)!.WindowIds);
        Assert.True(Frame(Sample).IsVisible);
        Assert.True(Frame(Sample).IsDocked);
        Assert.False(Frame(Layers).IsVisible);
    }

    [AvaloniaFact]
    public void RightClickingATab_OpensThatWindowsOwnMenu()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));

        _window.MouseDown(TabOf(History), MouseButton.Right);
        _window.MouseUp(TabOf(History), MouseButton.Right);
        Pump();

        Assert.True(Frame(History).TitleMenu!.IsOpen);
        Assert.False(Frame(Layers).TitleMenu!.IsOpen);
        Frame(History).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void DoubleClickingATab_SqueezesItsGroupToItsTabStrip()
    {
        ShowWindow(Dock(Column(Tabs(Layers), Tabs(History))));

        DoubleClick(TabOf(Layers));

        Assert.True(_docking.Layout!.GroupHolding(Layers)!.IsTitleOnly);
        Assert.Equal(_metrics.Docking.TitleLength, BoundsOf(Layers).Height);
    }

    [AvaloniaFact]
    public void DoubleClickingATabThatIsntShowing_ShowsIt_AndSqueezesItsGroup()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));
        _docking.ToggleWindow(Sample);
        Pump();
        var group = BoundsOf(Layers);
        DragAndDrop(TitleRowOf(Sample), new Point(group.Center.X, group.Bottom - 10));
        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 1, 0), _docking.Layout!.PlaceOf(Sample));

        DoubleClick(TabOf(History));

        Assert.True(Frame(History).IsVisible);
        Assert.True(_docking.Layout!.GroupHolding(History)!.IsTitleOnly);
    }

    [AvaloniaFact]
    public void DoubleClickingTheTabOfAWindowAloneInItsColumn_DoesNothing()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));
        var before = _docking.Layout;

        DoubleClick(TabOf(Layers));

        Assert.Same(before, _docking.Layout);
    }

    [AvaloniaFact]
    public void ADockWindowsMenu_OffersFloatTurnIntoWidgetAndClose()
    {
        ShowWindow(Dock(Column(Tabs(Layers), Tabs(History))));

        var items = OpenMenu(Layers);

        Assert.Equal(
            [Label(Strings.ActionWindowFloat), Label(Strings.ActionWindowTurnIntoWidget), Label(Strings.ActionWindowClose)],
            items.Select(item => item.Header));
        Assert.True(items[1].Command!.CanExecute(items[1].CommandParameter));
        Frame(Layers).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void TurnIntoWidget_IsGreyedOut_ForADockThatWidgetsCouldntHold()
    {
        ShowWindow(Dock(Column(Tabs(Layers, History))));

        var items = OpenMenu(Layers);

        Assert.False(items[1].Command!.CanExecute(items[1].CommandParameter));
        Frame(Layers).TitleMenu!.Close();
        Pump();
    }

    [AvaloniaFact]
    public void TurnIntoWidget_TurnsTheWholeDockIntoWidgets()
    {
        ShowWindow(Dock(Column(Tabs(Layers), Tabs(History))));

        Execute(WindowFrameActionIds.TurnIntoWidget, History);

        Assert.Equal(WindowKind.Widget, _docking.WindowKindOf(Layers));
        Assert.Equal(WindowKind.Widget, _docking.WindowKindOf(History));
        Assert.False(Frame(Layers).IsDocked);
    }

    [AvaloniaFact]
    public void TurnIntoDock_TurnsAWidgetsWholeSideIntoADock()
    {
        ShowWindow(null);

        Execute(WindowFrameActionIds.TurnIntoDock, Layers);

        Assert.Equal(WindowKind.Dock, _docking.WindowKindOf(Layers));
        Assert.Equal(WindowKind.Dock, _docking.WindowKindOf(History));
        Assert.True(Frame(Layers).IsDocked);
        Assert.True(Frame(History).IsDocked);
    }

    [AvaloniaFact]
    public void TurnIntoDock_PutsAFloatingWindowInTheDock_AsAColumnAtItsOuterEdge()
    {
        var layout = LayoutEdits.FloatWindow(Dock(Column(Tabs(Layers))), History, new LayoutPoint(300, 100), 200, 300);
        ShowWindow(layout);

        Execute(WindowFrameActionIds.TurnIntoDock, History);

        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 0, 0), _docking.Layout!.PlaceOf(History));
        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 1, 0, 0), _docking.Layout!.PlaceOf(Layers));
        Assert.Equal(200, BoundsOf(History).Width);
    }

    [AvaloniaFact]
    public void DraggingTheBorderBetweenDockColumns_WidensOnlyTheOuterOne()
    {
        ShowWindow(Dock(Column(Tabs(Layers)), Column(Tabs(History))));
        var outer = BoundsOf(Layers);
        var inner = BoundsOf(History);
        var border = new Point(outer.Left, outer.Center.Y);

        _window.MouseDown(border, MouseButton.Left);
        _window.MouseMove(border + new Point(-40, 0));
        Pump();
        _window.MouseUp(border + new Point(-40, 0), MouseButton.Left);
        Pump();

        Assert.Equal(outer.Width + 40, BoundsOf(Layers).Width);
        Assert.Equal(inner.Width, BoundsOf(History).Width);
        Assert.Equal(inner.Left - 40, BoundsOf(History).Left);
    }

    [AvaloniaFact]
    public void DuringADrag_ASideWithWindows_HasAStripAtItsEdge_ThatMakesADock()
    {
        ShowWindow(null);
        var rightEdge = new Point(_host.Bounds.Width - 3, 350);

        StartDrag(TitleRowOf(Layers), rightEdge);
        Assert.Contains(_host.Children.OfType<DropZoneView>(), view => view.Bounds.Right == _host.Bounds.Width && view.IsOutlined);
        _window.MouseUp(rightEdge, MouseButton.Left);
        Pump();

        Assert.Equal(new WindowPlace(SideEdge.Right, SideKind.Dock, 0, 0, 0), _docking.Layout!.PlaceOf(Layers));
        Assert.Equal(WindowKind.Dock, _docking.WindowKindOf(History));
    }

    private static DockingLayout Dock(params SideColumn[] columns) =>
        new([new SideLayout(SideEdge.Right, SideKind.Dock, columns)], [], []);

    private static SideColumn Column(params WindowGroup[] groups) => new(240, groups);

    private static WindowGroup Tabs(params string[] windowIds) => new(windowIds);

    // Headless text draws every letter as wide as it is tall, so long titles would push tabs past the end of the strip.
    private static string TitleOf(string windowId) => windowId[6..7];

    private WindowFrame Frame(string windowId) =>
        _host.Children.OfType<WindowFrame>().Single(frame => frame.Title == TitleOf(windowId));

    private Rect BoundsOf(string windowId) => Frame(windowId).Bounds;

    private Point TitleRowOf(string windowId) => BoundsOf(windowId).TopLeft + new Point(20, 10);

    // A tab's middle, on whichever of its group's frames is showing.
    private Point TabOf(string windowId)
    {
        var showing = _host.Children.OfType<WindowFrame>()
            .Single(frame => frame is { IsDocked: true, IsVisible: true } && frame.Tabs.WindowIds.Contains(windowId));
        var tab = showing.Tabs.TabFor(windowId)!;
        return tab.TranslatePoint(new Point(tab.Bounds.Width / 2, tab.Bounds.Height / 2), _host)!.Value;
    }

    private Rect StripOf(string windowId)
    {
        var strip = Frame(windowId).Tabs;
        return new Rect(strip.TranslatePoint(default, _host)!.Value, strip.Bounds.Size);
    }

    private static string Label(string text) => UiText.WithoutAccessKey(text);

    private List<MenuItem> OpenMenu(string windowId)
    {
        var tab = TabOf(windowId);
        _window.MouseDown(tab, MouseButton.Right);
        _window.MouseUp(tab, MouseButton.Right);
        Pump();

        return [.. Frame(windowId).TitleMenu!.Items.OfType<MenuItem>().Where(item => item.IsVisible)];
    }

    private void Execute(string actionId, string windowId)
    {
        _actions.Get(actionId).Command.Execute(windowId);
        Pump();
    }

    private void Click(Point at)
    {
        _window.MouseDown(at, MouseButton.Left);
        _window.MouseUp(at, MouseButton.Left);
        Pump();
    }

    private void DoubleClick(Point at)
    {
        _window.MouseDown(at, MouseButton.Left);
        _window.MouseUp(at, MouseButton.Left);
        Pump();
        _window.MouseDown(at, MouseButton.Left);
        _window.MouseUp(at, MouseButton.Left);
        Pump();
    }

    private void StartDrag(Point from, Point to)
    {
        _window.MouseDown(from, MouseButton.Left);
        _window.MouseMove(to);
        Pump();
    }

    private void DragAndDrop(Point from, Point to)
    {
        StartDrag(from, to);
        _window.MouseUp(to, MouseButton.Left);
        Pump();
    }

    // With no layout, the default one, which has the layers and history windows as widgets on the right.
    private void ShowWindow(DockingLayout? saved)
    {
        if (saved is not null)
            _settings.SetText(ShellDocking.LayoutSettingKey, LayoutJson.Write(saved));

        _host = new DockingHost();
        _window = new Window { Width = 1000, Height = 700, Content = _host };
        _windows.Add(_window);

        _metrics = ShellLayoutMetrics.FromResources(Application.Current!) with { MotionDuration = TimeSpan.Zero };
        ShellWindowEntry Panel(string id) => new(id, TitleOf(id), _metrics.WindowMinHeight, () => new Border(),
            () => WindowMenu.Create(id, _actions, _docking), _actions.Get(WindowFrameActionIds.Close).Command);

        var catalog = new ShellCatalog([Panel(Layers), Panel(History), Panel(Sample)], []);
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
