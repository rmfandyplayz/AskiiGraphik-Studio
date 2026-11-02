// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Editing;
using AskiiArt.App.Layers;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
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
    private readonly ShellDock _dock = new(new FakeAppSettings(), NullLogger<ShellDock>.Instance);
    private readonly MainWindow _window;

    public ClickRoutingTests()
    {
        var layers = new LayersPanelViewModel(_canvas.ActiveDocument);
        var actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs, layers: layers));
        var builder = new MainWindowBuilder(actions, MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false),
            ToolbarLayout.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell, _dock, new FakeAppSettings(),
            new HistoryPanelViewModel(_canvas.ActiveDocument), layers);

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
    public void TheToolbarHugsItsButtons_AndTheRestOfItsRowBelongsToTheCanvas()
    {
        ToolbarView.Bar = new Border { Width = 100, Height = 20 };
        Pump();
        var toolbar = RectIn(ToolbarView);
        Assert.True(toolbar.Left > 100);

        Assert.Same(Canvas, _window.InputHitTest(new Point(toolbar.Left / 2, toolbar.Center.Y)));
    }

    [AvaloniaFact]
    public void TheToolbar_GetsClicksFirst()
    {
        var hit = _window.InputHitTest(RectIn(ToolbarView).Center) as Visual;

        Assert.NotNull(hit);
        Assert.NotSame(Canvas, hit);
        Assert.Contains(ToolbarView, hit.GetVisualAncestors().Append(hit));
    }

    [AvaloniaFact]
    public void TheToolbarsRow_FitsItsButtons()
    {
        var bar = ToolbarView.GetVisualDescendants().OfType<StackPanel>()
            .Single(panel => panel.Name == ToolbarBuilder.ControlName);

        Assert.True(bar.DesiredSize.Height <= ToolbarView.Bounds.Height,
            $"The buttons need {bar.DesiredSize.Height}, and the row gives them {ToolbarView.Bounds.Height}.");
    }

    [AvaloniaFact]
    public void TheMenuBar_IsAStripAboveTheCanvas_NotAWidgetOverIt()
    {
        var strip = RectIn(Named<Border>("MenuBarStrip"));
        var canvas = RectIn(Canvas);

        Assert.Equal(0, strip.Top, tolerance: 1);
        Assert.Equal(_window.Bounds.Width, strip.Width, tolerance: 1);
        Assert.True(canvas.Top >= strip.Bottom);
    }

    [AvaloniaFact]
    public void AToolbarThatDoesntFit_ScrollsWithTheWheel()
    {
        ToolbarView.Bar = new Border { Width = 5000, Height = 20 };
        Pump();
        var scroller = ToolbarView.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.True(scroller.Extent.Width > scroller.Viewport.Width);
        Assert.Equal(ScrollBarVisibility.Hidden, scroller.HorizontalScrollBarVisibility);

        _window.MouseWheel(RectIn(scroller).Center, new Vector(0, -1));
        Pump();

        Assert.True(scroller.Offset.X > 0);
    }

    [AvaloniaFact]
    public void TheToolbar_DocksOnAWindowEdge_AndTheCanvasGetsItsRowBack()
    {
        StartDragging(ShellDockIds.ToolbarPanel);

        DropOnWindowEdge("PART_LeftSelector");

        var sideWidth = ShellLayoutMetrics.FromResources(Application.Current!).SideWidth * _window.RenderScaling;
        Assert.InRange(Canvas.Viewport!.Insets.Left, sideWidth, sideWidth + 8);
        Assert.Equal(0, Canvas.Viewport!.Insets.Top);
    }

    [AvaloniaFact]
    public void TheToolbar_DockedAlongTheBottom_KeepsItsHeight()
    {
        StartDragging(ShellDockIds.ToolbarPanel);

        DropOnWindowEdge("PART_BottomSelector");

        var height = ShellLayoutMetrics.FromResources(Application.Current!).ToolbarHeight * _window.RenderScaling;
        Assert.InRange(Canvas.Viewport!.Insets.Bottom, height, height + 8);
        Assert.Equal(0, Canvas.Viewport!.Insets.Top);
    }

    [AvaloniaFact]
    public void TheToolbar_Floats()
    {
        StartDragging(ShellDockIds.ToolbarPanel);

        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseUp(new Point(300, 400), MouseButton.Left);
        Pump();

        Assert.Single(Root.Windows!);
        Assert.Single(_window.GetVisualDescendants().OfType<ToolbarPanelView>());
        Assert.Equal(0, Canvas.Viewport!.Insets.Top);
    }

    [AvaloniaFact]
    public void ReopeningTheToolbar_PutsItBackAlongTheTop()
    {
        var before = Canvas.Viewport!.Insets.Top;
        Assert.True(before > 0);

        _dock.TogglePanel(ShellDockIds.ToolbarPanel);
        Pump();
        Assert.Equal(0, Canvas.Viewport!.Insets.Top);
        Assert.Empty(_window.GetVisualDescendants().OfType<ToolbarPanelView>());

        _dock.TogglePanel(ShellDockIds.ToolbarPanel);
        Pump();
        Assert.InRange(Canvas.Viewport!.Insets.Top, before, before + 8);
        Assert.Single(_window.GetVisualDescendants().OfType<ToolbarPanelView>());
    }

    [AvaloniaFact]
    public void ReopeningAPanel_DoesntTabItWithTheToolbar()
    {
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        Pump();

        var host = ShellDockFactory.AllDockables(Root).OfType<IDock>().Single(dock =>
            dock.VisibleDockables?.Any(dockable => dockable.Id == ShellDockIds.HistoryPanel) == true);
        Assert.Contains(host.VisibleDockables!, dockable => dockable.Id == ShellDockIds.LayersPanel);
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
        var toolbarBottom = (RectIn(ToolbarView).Bottom - RectIn(Canvas).Top) * scaling;
        Assert.True(viewport.Insets.Top >= toolbarBottom,
            $"Insets start at {viewport.Insets.Top}, and the toolbar ends at {toolbarBottom}.");
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
    public void TheToolbarAndMenuBar_ShowInTheirPlaces()
    {
        Assert.Contains(ToolbarView.GetVisualDescendants(),
            visual => visual is StackPanel { Name: ToolbarBuilder.ControlName });
        Assert.Contains(Named<Border>("MenuBarStrip").GetVisualDescendants(), visual => visual is NativeMenuBar);
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
    public void DraggingAFloatingPanelOverTheCanvas_OffersTheWindowEdges_ButNotTheCanvasItself()
    {
        FloatAndStartDragging(ShellDockIds.HistoryPanel);

        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();

        Assert.NotEmpty(_window.GetVisualDescendants().OfType<GlobalDockTarget>());
        Assert.Empty(_window.GetVisualDescendants().OfType<DockTarget>());
        _window.MouseUp(new Point(300, 400), MouseButton.Left);
        Pump();
    }

    [AvaloniaFact]
    public void DroppingAFloatingPanelOnTheLeftEdge_DocksItLeftOfTheCanvas_AtTheSideWidth()
    {
        FloatAndStartDragging(ShellDockIds.HistoryPanel);
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
        Assert.Contains(ShellDockFactory.AllDockables(Root), dockable => dockable.Id == ShellDockIds.HistoryPanel);
        Assert.DoesNotContain(ShellDockFactory.AllDockables(Root), dockable => dockable is ManagedDockWindowDocument);
        var sideWidth = ShellLayoutMetrics.FromResources(Application.Current!).SideWidth * _window.RenderScaling;
        Assert.InRange(Canvas.Viewport!.Insets.Left, sideWidth, sideWidth + 8);
    }

    [AvaloniaFact]
    public void ReleasingAFloatingPanelOverTheCanvas_LeavesItFloating_AndTheCanvasGetsClicksAgain()
    {
        FloatAndStartDragging(ShellDockIds.HistoryPanel);
        var over = new Point(300, 400);
        _window.MouseMove(over, RawInputModifiers.LeftMouseButton);
        Pump();
        Assert.True(_window.Panels.IsDraggingDock);

        _window.MouseUp(over, MouseButton.Left);
        Pump();

        Assert.Single(Root.Windows!);
        Assert.False(_window.Panels.IsDraggingDock);
        Assert.Same(Canvas, _window.InputHitTest(new Point(600, 200)));
    }

    [AvaloniaTheory]
    [InlineData("PART_DragHandle")]
    [InlineData("PART_Grid")]
    public void AFloatingPanel_MovesFromAnywhereOnItsHeader(string partOfHeader)
    {
        var panel = ShellDockFactory.AllDockables(Root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel);
        _window.Panels.Factory!.FloatDockable(panel);
        Pump();
        var floating = _window.GetVisualDescendants().OfType<MdiDocumentWindow>().Single();
        var part = floating.GetVisualDescendants().OfType<Control>().First(control => control.Name == partOfHeader);
        var before = RectIn(floating).TopLeft;
        var start = RectIn(part).Center;

        _window.MouseDown(start, MouseButton.Left);
        _window.MouseMove(start + new Point(-400, 0), RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseUp(start + new Point(-400, 0), MouseButton.Left);
        Pump();

        Assert.Equal(before.X - 400, RectIn(floating).X);
    }

    [AvaloniaFact]
    public void DroppingAFloatingPanelOnADockedPanel_TabsThemTogether()
    {
        FloatAndStartDragging(ShellDockIds.HistoryPanel);
        var layers = _window.GetVisualDescendants().OfType<ToolChromeControl>()
            .Single(chrome => chrome.DataContext is IDock { ActiveDockable.Id: ShellDockIds.LayersPanel });
        var over = RectIn(layers).Center;
        _window.MouseMove(over, RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseMove(over + new Point(1, 1), RawInputModifiers.LeftMouseButton);
        Pump();

        _window.MouseUp(over + new Point(1, 1), MouseButton.Left);
        Pump();

        Assert.True(Root.Windows is null || Root.Windows.Count == 0);
        var both = ShellDockFactory.AllDockables(Root).OfType<IDock>().Single(dock =>
            dock.VisibleDockables?.Any(dockable => dockable.Id == ShellDockIds.HistoryPanel) == true);
        Assert.Contains(both.VisibleDockables!, dockable => dockable.Id == ShellDockIds.LayersPanel);
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

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(3)]
    public void OpeningADocument_HighlightsItsActiveLayer(int layersBefore)
    {
        var layers =
            (LayersPanelViewModel)_window.GetVisualDescendants().OfType<LayersPanelView>().Single().DataContext!;
        for (var added = 1; added < layersBefore; added++)
            layers.AddLayer();

        Pump();
        var rows = LayerRows();
        var opened = new EditingSession(Document.Create(new CanvasSize(10, 10), "Bottom"));
        opened.History.Execute(new AddLayerCommand(1, new LayerProperties("Middle")));
        opened.History.Execute(new AddLayerCommand(2, new LayerProperties("Top")));
        opened.SetActiveLayer(opened.Document.ActiveFrame.Layers[^1].Id);

        _canvas.ActiveDocument.Replace(opened);
        Pump();

        Assert.Equal(3, rows.ItemCount);
        Assert.Equal(0, rows.SelectedIndex);
        Assert.Equal(opened.ActiveLayer.Id, ((LayerRow)rows.SelectedItem!).LayerId);
    }

    [AvaloniaFact]
    public void FloatingAPanel_KeepsItOpenAsFarAsTheWindowMenuKnows()
    {
        var lastAnswer = false;
        _dock.PanelsChanged += (_, _) => lastAnswer = _dock.IsPanelOpen(ShellDockIds.HistoryPanel);
        var panel = ShellDockFactory.AllDockables(Root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel);

        _window.Panels.Factory!.FloatDockable(panel);
        Pump();

        Assert.True(lastAnswer);
    }

    [AvaloniaFact]
    public void ClosingAFloatingPanel_TellsTheWindowMenu()
    {
        var lastAnswer = true;
        var panel = ShellDockFactory.AllDockables(Root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel);
        _window.Panels.Factory!.FloatDockable(panel);
        Pump();
        _dock.PanelsChanged += (_, _) => lastAnswer = _dock.IsPanelOpen(ShellDockIds.HistoryPanel);

        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        Pump();

        Assert.False(lastAnswer);
    }

    [AvaloniaFact]
    public void ClickingALayerRow_MakesItActive_AndTypingStaysWithTheCanvas()
    {
        var bottom = _canvas.Session.ActiveLayer.Id;
        var layers = _window.GetVisualDescendants().OfType<LayersPanelView>().Single();
        ((LayersPanelViewModel)layers.DataContext!).AddLayer();
        Pump();
        var rows = LayerRows();
        Assert.Equal(0, rows.SelectedIndex);
        var onName = NameOfRow(rows, 1);

        _window.MouseDown(onName, MouseButton.Left);
        _window.MouseUp(onName, MouseButton.Left);
        Pump();

        Assert.Equal(bottom, _canvas.Session.ActiveLayer.Id);
        Assert.Equal(1, rows.SelectedIndex);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void ClickingARowsVisibleBox_HidesThatLayer_AndLeavesTheActiveLayerAlone()
    {
        var bottom = _canvas.Document.ActiveFrame.Layers[0];
        var layers = _window.GetVisualDescendants().OfType<LayersPanelView>().Single();
        ((LayersPanelViewModel)layers.DataContext!).AddLayer();
        Pump();
        var active = _canvas.Session.ActiveLayer.Id;
        var box = RowContainer(LayerRows(), 1).GetVisualDescendants().OfType<CheckBox>().First();
        var onBox = RectIn(box).Center;

        _window.MouseDown(onBox, MouseButton.Left);
        _window.MouseUp(onBox, MouseButton.Left);
        Pump();

        Assert.False(bottom.Properties.IsVisible);
        Assert.Equal(active, _canvas.Session.ActiveLayer.Id);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void DoubleClickingARow_OpensItsName_AndEnterKeepsTheNewName_ThenTypingGoesBackToTheCanvas()
    {
        var onName = NameOfRow(LayerRows(), 0);

        _window.MouseDown(onName, MouseButton.Left);
        _window.MouseUp(onName, MouseButton.Left);
        Pump();
        _window.MouseDown(onName, MouseButton.Left, RawInputModifiers.None);
        Pump();
        _window.MouseUp(onName, MouseButton.Left);
        Pump();

        var box = Assert.IsType<TextBox>(_window.FocusManager!.GetFocusedElement());
        box.Text = "Sky";
        _window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        _window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Pump();

        Assert.Equal("Sky", _canvas.Session.ActiveLayer.Properties.Name);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void TheRenameAction_OpensTheActiveRowsName_AndEscapeLeavesTheNameAlone()
    {
        var name = _canvas.Session.ActiveLayer.Properties.Name;
        var layers =
            (LayersPanelViewModel)_window.GetVisualDescendants().OfType<LayersPanelView>().Single().DataContext!;

        layers.RenameActiveLayer();
        Pump();
        var box = Assert.IsType<TextBox>(_window.FocusManager!.GetFocusedElement());
        box.Text = "Sky";
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Pump();

        Assert.Equal(name, _canvas.Session.ActiveLayer.Properties.Name);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void ClickingAwayFromARename_KeepsTheNewName()
    {
        var layers =
            (LayersPanelViewModel)_window.GetVisualDescendants().OfType<LayersPanelView>().Single().DataContext!;
        layers.RenameActiveLayer();
        Pump();
        var box = Assert.IsType<TextBox>(_window.FocusManager!.GetFocusedElement());
        box.Text = "Sky";

        var onCanvas = new Point(300, 400);
        _window.MouseDown(onCanvas, MouseButton.Left);
        _window.MouseUp(onCanvas, MouseButton.Left);
        Pump();

        Assert.Equal("Sky", _canvas.Session.ActiveLayer.Properties.Name);
    }

    [AvaloniaFact]
    public void TheLayersPanelsButtons_RunTheLayerActions()
    {
        var newButton = _window.GetVisualDescendants().OfType<LayersPanelView>().Single()
            .GetVisualDescendants().OfType<Button>().First(button => button.Command is not null);
        var onButton = RectIn(newButton).Center;

        _window.MouseDown(onButton, MouseButton.Left);
        _window.MouseUp(onButton, MouseButton.Left);
        Pump();

        Assert.Equal(2, _canvas.Document.ActiveFrame.Layers.Count);
        Assert.Equal(2, LayerRows().ItemCount);
        Assert.True(Canvas.IsFocused);
    }

    private ListBox LayerRows() =>
        _window.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "Rows");

    private static Control RowContainer(ListBox rows, int index) => rows.ContainerFromIndex(index)!;

    private Point NameOfRow(ListBox rows, int index)
    {
        var name = RowContainer(rows, index).GetVisualDescendants().OfType<TextBlock>().First(text => text.IsVisible);
        return RectIn(name).Center;
    }

    private void DropOnWindowEdge(string targetName)
    {
        _window.MouseMove(new Point(300, 400), RawInputModifiers.LeftMouseButton);
        Pump();

        var target = _window.GetVisualDescendants().OfType<GlobalDockTarget>().Single()
            .GetVisualDescendants().OfType<Image>().Single(image => image.Name == targetName);
        var onTarget = RectIn(target).Center;
        _window.MouseMove(onTarget, RawInputModifiers.LeftMouseButton);
        Pump();
        _window.MouseUp(onTarget, MouseButton.Left);
        Pump();
    }

    private ToolbarPanelView ToolbarView =>
        _window.GetVisualDescendants().OfType<ToolbarPanelView>().Single();

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

    private void FloatAndStartDragging(string panelId)
    {
        var panel = ShellDockFactory.AllDockables(Root).Single(dockable => dockable.Id == panelId);
        _window.Panels.Factory!.FloatDockable(panel);
        Pump();

        var floating = _window.GetVisualDescendants().OfType<MdiDocumentWindow>().Single();
        var header = floating.GetVisualDescendants().OfType<Control>().First(control => control.Name == "PART_Header");
        var start = RectIn(header).TopLeft + new Point(40, header.Bounds.Height / 2);

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
