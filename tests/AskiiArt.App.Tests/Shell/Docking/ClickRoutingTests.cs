// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Editing;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>
/// The real main window: the canvas underneath, the panel layout on top (ADR 0025). These guard the
/// styles in DockStyles.axaml, which depend on how Dock builds its templates, so a Dock update that
/// changes them fails here.
/// </summary>
public sealed class ClickRoutingTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly TestCanvas _canvas = new();
    private readonly MainWindow _window;

    public ClickRoutingTests()
    {
        var actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs));
        var builder = new MainWindowBuilder(actions, MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false),
            ToolbarLayout.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell,
            new ShellDock(new FakeAppSettings(), NullLogger<ShellDock>.Instance), new FakeAppSettings(),
            new HistoryPanelViewModel(_canvas.ActiveDocument));

        _window = builder.Build(KeyModifiers.Control);
        _window.Width = 1200;
        _window.Height = 800;
        _window.Show();
        Pump();
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    private CanvasControl Canvas => _window.GetVisualDescendants().OfType<CanvasControl>().Single();

    [AvaloniaFact]
    public void TheEmptyMiddleOfTheWindow_BelongsToTheCanvas()
    {
        Assert.Same(Canvas, _window.InputHitTest(new Point(300, 400)));
    }

    [AvaloniaFact]
    public void TheGapBetweenTheMenuBarAndToolbar_BelongsToTheCanvas()
    {
        _window.SetToolbar(new Border { Width = 100, Height = 20 });
        Pump();
        var menuBar = RectIn(Named<Border>("MenuBarWidget"));
        var toolbar = RectIn(Named<Border>("ToolbarWidget"));
        Assert.True(toolbar.Left > menuBar.Right);

        Assert.Same(Canvas, _window.InputHitTest(new Point((menuBar.Right + toolbar.Left) / 2, menuBar.Center.Y)));
    }

    [AvaloniaTheory]
    [InlineData("MenuBarWidget")]
    [InlineData("ToolbarWidget")]
    public void ATopBarWidget_GetsClicksFirst(string name)
    {
        var widget = Named<Border>(name);
        var hit = _window.InputHitTest(RectIn(widget).Center) as Visual;

        Assert.NotNull(hit);
        Assert.NotSame(Canvas, hit);
        Assert.Contains(widget, hit.GetVisualAncestors().Append(hit));
    }

    [AvaloniaFact]
    public void TheMenuBarAndToolbar_ShareARow_WithTheToolbarAtTheRight()
    {
        var menuBar = RectIn(Named<Border>("MenuBarWidget"));
        var scroller = RectIn(Named<ScrollViewer>("ToolbarScroller"));

        Assert.Equal(menuBar.Top, scroller.Top, tolerance: 8);
        Assert.Equal(_window.Bounds.Width, scroller.Right, tolerance: 1);
    }

    [AvaloniaFact]
    public void AToolbarThatDoesntFit_ScrollsWithTheWheel_WhileTheMenuBarKeepsItsWidth()
    {
        var menuBarWidth = Named<Border>("MenuBarWidget").Bounds.Width;
        _window.SetToolbar(new Border { Width = 5000, Height = 20 });
        Pump();
        var scroller = Named<ScrollViewer>("ToolbarScroller");
        Assert.True(scroller.Extent.Width > scroller.Viewport.Width);
        Assert.Equal(ScrollBarVisibility.Hidden, scroller.HorizontalScrollBarVisibility);
        var over = RectIn(scroller).Center;

        _window.MouseWheel(over, new Vector(0, -1));
        Pump();

        Assert.True(scroller.Offset.X > 0);
        Assert.Equal(menuBarWidth, Named<Border>("MenuBarWidget").Bounds.Width);
    }

    [AvaloniaFact]
    public void TheStatusBar_IsAStripBelowTheCanvas_NotAWidgetOverIt()
    {
        var status = RectIn(_window.GetVisualDescendants().OfType<StatusBarView>().Single());
        var canvas = RectIn(Canvas);

        Assert.Equal(_window.Bounds.Width, status.Width, tolerance: 1);
        Assert.Equal(_window.Bounds.Height, status.Bottom, tolerance: 1);
        Assert.True(canvas.Bottom <= status.Top);
    }

    [AvaloniaFact]
    public void TheMainWindow_HasAMinimumSize()
    {
        var metrics = ShellLayoutMetrics.FromResources(Application.Current!);

        Assert.Equal(1280, metrics.WindowMinWidth);
        Assert.Equal(720, metrics.WindowMinHeight);
        Assert.True(_window.MinWidth > 0);
        Assert.True(_window.MinHeight > 0);
        Assert.True(_window.MinWidth <= metrics.WindowMinWidth);
        Assert.True(_window.MinHeight <= metrics.WindowMinHeight);
    }

    [AvaloniaFact]
    public void AMovablePanel_GetsClicksFirst()
    {
        var hit = _window.InputHitTest(new Point(_window.Bounds.Width - 100, 400));

        Assert.NotSame(Canvas, hit);
        Assert.NotNull(hit);
    }

    [AvaloniaFact]
    public void ClickingTheCanvas_ThroughTheLayout_Reaches_ItsTool()
    {
        var hovered = _canvas.ViewModel.HoveredCell;
        var middle = Canvas.Viewport!.CanvasBounds.Center;
        var point = new Point(middle.X / _window.RenderScaling, middle.Y / _window.RenderScaling);

        _window.MouseMove(point);
        Pump();

        Assert.NotEqual(hovered, _canvas.ViewModel.HoveredCell);
    }

    [AvaloniaFact]
    public void WhileAPanelIsBeingDragged_DropAreasCatchTheHit()
    {
        var host = _window.Panels;
        var middle = new Point(300, 400);

        host.IsDraggingDock = true;
        Pump();
        var hitWhileDragging = _window.InputHitTest(middle);

        host.IsDraggingDock = false;
        Pump();

        Assert.NotSame(Canvas, hitWhileDragging);
        Assert.Same(Canvas, _window.InputHitTest(middle));
    }

    [AvaloniaFact]
    public void TheCanvas_KeepsItsMiddleClearOfWidgets()
    {
        var viewport = Canvas.Viewport!;
        var scaling = _window.RenderScaling;

        var usableLeft = viewport.Insets.Left;
        var usableRight = viewport.ViewportSize.Width - viewport.Insets.Right;
        Assert.True(viewport.Insets.Top >= RectIn(Named<Grid>("TopBar")).Bottom * scaling);
        Assert.True(viewport.Insets.Right >=
                    ShellLayoutMetrics.FromResources(Application.Current!).SideWidth * scaling);
        Assert.Equal(0, viewport.Insets.Bottom);
        Assert.Equal((usableLeft + usableRight) / 2.0, viewport.CanvasBounds.Center.X, tolerance: 1);
    }

    [AvaloniaFact]
    public void ClosingEveryMovablePanel_GivesTheCanvasTheirSpace()
    {
        var before = Canvas.Viewport!.Insets.Right;
        var layout = _window.Panels;
        var root = (Dock.Model.Controls.IRootDock)layout.Layout!;

        foreach (var id in new[] { ShellDockIds.LayersPanel, ShellDockIds.HistoryPanel })
            layout.Factory!.CloseDockable(ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == id));

        Pump();

        Assert.True(Canvas.Viewport!.Insets.Right < before);
    }

    [AvaloniaFact]
    public void TheToolbarAndMenuBar_ShowInTheirWidgets()
    {
        Assert.Contains(Named<Border>("ToolbarWidget").GetVisualDescendants(),
            visual => visual is StackPanel { Name: ToolbarBuilder.ControlName });
        Assert.Contains(Named<Border>("MenuBarWidget").GetVisualDescendants(), visual => visual is NativeMenuBar);
    }

    [AvaloniaFact]
    public void DraggingAPanelOverTheCanvas_OffersTheWindowEdges_ButNotTheCanvasItself()
    {
        StartDragging(ShellDockIds.HistoryPanel);

        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();

        Assert.NotEmpty(_window.GetVisualDescendants().OfType<GlobalDockTarget>());
        Assert.Empty(_window.GetVisualDescendants().OfType<DockTarget>());
        _window.MouseUp(new Point(300, 400), MouseButton.Left);
        Pump();
    }

    [AvaloniaFact]
    public void ReleasingAPanelOverTheCanvas_FloatsIt()
    {
        StartDragging(ShellDockIds.HistoryPanel);

        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseUp(new Point(300, 400), MouseButton.Left);
        Pump();

        var floating = Assert.Single(Root.Windows!);
        Assert.Contains(ShellDockFactory.AllDockables((Dock.Model.Controls.IRootDock)floating.Layout!),
            dockable => dockable.Id == ShellDockIds.HistoryPanel);
    }

    [AvaloniaFact]
    public void DroppingAPanelOnTheLeftEdge_DocksItLeftOfTheCanvas_AtTheSideWidth()
    {
        StartDragging(ShellDockIds.HistoryPanel);
        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();

        var leftTarget = _window.GetVisualDescendants().OfType<GlobalDockTarget>().Single()
            .GetVisualDescendants().OfType<Image>().Single(image => image.Name == "PART_LeftSelector");
        var onTarget = RectIn(leftTarget).Center;
        _window.MouseMove(onTarget, RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseUp(onTarget, MouseButton.Left);
        Pump();

        Assert.True(Root.Windows is null || Root.Windows.Count == 0);
        var sideWidth = ShellLayoutMetrics.FromResources(Application.Current!).SideWidth * _window.RenderScaling;
        Assert.InRange(Canvas.Viewport!.Insets.Left, sideWidth, sideWidth + 8);
    }

    [AvaloniaFact]
    public void ClosingAMenu_GivesTypingBackToTheCanvas()
    {
        var menu = _window.GetVisualDescendants().OfType<Menu>().Single();
        var file = menu.GetVisualDescendants().OfType<MenuItem>().First();
        menu.Open();
        file.Focus();
        Pump();
        Assert.False(Canvas.IsFocused);

        menu.Close();
        Pump();

        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void ClickingAHistoryRow_JumpsThere_AndTypingStaysWithTheCanvas()
    {
        Paint('a');
        Paint('b');
        Pump();
        var steps = HistoryList();
        Assert.Equal(2, steps.SelectedIndex);
        var row = steps.ContainerFromIndex(1)!;
        var onRow = RectIn(row).Center;

        _window.MouseDown(onRow, MouseButton.Left);
        _window.MouseUp(onRow, MouseButton.Left);
        Pump();

        Assert.Equal(1, _canvas.Session.History.UndoCount);
        Assert.Equal(1, steps.SelectedIndex);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void TheHistoryPanel_StaysInStep_WhileItFloats()
    {
        Paint('a');
        var history = _canvas.Session.History;
        var panel = ShellDockFactory.AllDockables(Root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel);
        _window.Panels.Factory!.FloatDockable(panel);
        Pump();
        var steps = HistoryList();
        Assert.Equal(1, steps.SelectedIndex);

        Paint('b');
        Pump();
        Assert.Equal(3, steps.ItemCount);
        Assert.Equal(2, steps.SelectedIndex);

        history.Undo();
        Pump();
        Assert.Equal(1, steps.SelectedIndex);

        steps.SelectedIndex = 0;
        Pump();
        Assert.Equal(0, history.UndoCount);
    }

    private ListBox HistoryList() =>
        _window.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "Steps");

    private void Paint(char glyph)
    {
        var builder = new CellEditBuilder(_canvas.Document, _canvas.Document.ActiveFrame.Layers[0].Id);
        builder.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune(glyph)));
        _canvas.Session.History.Execute(builder.Build()!);
    }

    private Dock.Model.Controls.IRootDock Root => (Dock.Model.Controls.IRootDock)_window.Panels.Layout!;

    private void StartDragging(string panelId)
    {
        var grip = _window.GetVisualDescendants().OfType<ToolChromeControl>()
            .Single(chrome => chrome.DataContext is IDock { ActiveDockable.Id: var active } && active == panelId)
            .GetVisualDescendants().OfType<Grid>().First(grid => grid.Name == "PART_Grip");
        var start = RectIn(grip).TopLeft + new Point(20, grip.Bounds.Height / 2);

        _window.MouseDown(start, MouseButton.Left);
        _window.MouseMove(start + new Point(-30, 30), RawInputModifiers.LeftMouseButton);
        Pump();
    }

    private T Named<T>(string name) where T : Control =>
        _window.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private Rect RectIn(Visual visual) => new(visual.TranslatePoint(default, _window)!.Value, visual.Bounds.Size);

    private static void Pump()
    {
        for (var pass = 0; pass < 5; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
