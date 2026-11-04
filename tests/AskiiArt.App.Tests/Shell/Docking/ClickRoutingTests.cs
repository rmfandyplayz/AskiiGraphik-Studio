// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Editing;
using AskiiArt.App.Layers;
using AskiiArt.App.Resources;
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
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>
/// The real main window: the canvas underneath, windows and toolbars on top of it (ADR 0035). Empty
/// spots have to reach the canvas, and windows and toolbars have to get clicks first.
/// </summary>
public sealed class ClickRoutingTests : IDisposable
{
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly TestCanvas _canvas = new();
    private readonly ShellDocking _docking = new(new FakeAppSettings(), NullLogger<ShellDocking>.Instance);
    private readonly MainWindow _window;

    public ClickRoutingTests()
    {
        var layers = new LayersPanelViewModel(_canvas.ActiveDocument);
        var actions = new ActionRegistry(TestActions.CreateProviders(_canvas, _shell, _dialogs, layers: layers));
        var builder = new MainWindowBuilder(actions, MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false),
            ShellToolbars.CreateDefault(), _canvas.ViewModel, new WindowTitle(_canvas.ActiveDocument),
            _canvas.CreateDocumentFiles(_dialogs, _shell), _shell, _docking, new FakeAppSettings(),
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
    public void AToolbar_HugsItsButtons_AndTheRestOfItsEdgeBelongsToTheCanvas()
    {
        var view = ToolbarView;
        view.Bar = new Border { Width = 100, Height = 20 };
        Pump();
        var toolbar = RectIn(view);

        Assert.Same(Canvas, _window.InputHitTest(new Point(toolbar.Right + 20, toolbar.Center.Y)));
    }

    [AvaloniaFact]
    public void EveryToolbar_GetsClicksFirst()
    {
        Assert.Equal(4, Toolbars.Count);
        Assert.All(Toolbars, toolbar =>
        {
            var hit = _window.InputHitTest(RectIn(toolbar).Center) as Visual;

            Assert.NotNull(hit);
            Assert.NotSame(Canvas, hit);
            Assert.Contains(toolbar, hit.GetVisualAncestors().Append(hit));
        });
    }

    [AvaloniaFact]
    public void EveryToolbar_FitsItsButtons()
    {
        Assert.All(Toolbars, toolbar =>
        {
            var bar = toolbar.GetVisualDescendants().OfType<StackPanel>()
                .Single(panel => panel.Name == ToolbarBuilder.ControlName);

            Assert.True(bar.DesiredSize.Height <= toolbar.Bounds.Height,
                $"The buttons need {bar.DesiredSize.Height}, and the toolbar gives them {toolbar.Bounds.Height}.");
            Assert.True(bar.DesiredSize.Width <= toolbar.Bounds.Width,
                $"The buttons need {bar.DesiredSize.Width}, and the toolbar gives them {toolbar.Bounds.Width}.");
        });
    }

    [AvaloniaFact]
    public void TheDefaultLayout_PutsToolbarsOnTheirAnchors_WithoutOverlapping()
    {
        var region = RectIn(Canvas);
        var widgets = RectIn(Frames.First());
        var file = RectIn(ToolbarWith(Strings.ActionNew));
        var tools = RectIn(ToolbarWith(Strings.ToolText));
        var edit = RectIn(ToolbarWith(Strings.ActionUndo));
        var view = RectIn(ToolbarWith(Strings.ActionZoomIn));

        Assert.True(file.Left < region.Width / 4 && file.Top < region.Center.Y);
        Assert.True(tools.Bottom > region.Center.Y);
        Assert.True(edit.Right <= widgets.Left && edit.Top < region.Center.Y);
        Assert.True(view.Right <= widgets.Left && view.Bottom > region.Center.Y);
        Assert.False(file.Intersects(edit));
        Assert.False(tools.Intersects(view));
    }

    [AvaloniaFact]
    public void NoToolbar_PassesTheWindowsTopOrBottomEdge()
    {
        var top = Frames.Min(frame => RectIn(frame).Top);
        var bottom = Frames.Max(frame => RectIn(frame).Bottom);

        Assert.All(Toolbars, toolbar =>
        {
            Assert.True(RectIn(toolbar).Top >= top - 1);
            Assert.True(RectIn(toolbar).Bottom <= bottom + 1);
        });
    }

    [AvaloniaFact]
    public void WindowsAndToolbars_AreTranslucent()
    {
        Assert.True(Application.Current!.TryGetResource("AppWidgetBrush", _window.ActualThemeVariant, out var brush));

        Assert.InRange(((ISolidColorBrush)brush!).Color.A, 1, 254);
    }

    [AvaloniaFact]
    public void HidingAToolbar_GivesTheCanvasItsBand_OnlyOnceTheLastOneOnThatEdgeGoes()
    {
        var before = Canvas.Viewport!.Insets.Top;
        Assert.True(before > 0);

        _docking.ToggleToolbar(ShellDockingIds.FileToolbar);
        Pump();
        Assert.Equal(before, Canvas.Viewport!.Insets.Top);

        _docking.ToggleToolbar(ShellDockingIds.EditToolbar);
        Pump();
        Assert.Equal(0, Canvas.Viewport!.Insets.Top);

        _docking.ToggleToolbar(ShellDockingIds.FileToolbar);
        Pump();
        Assert.Equal(before, Canvas.Viewport!.Insets.Top);
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
    public void AToolbarThatDoesntFit_StaysInsideTheCanvasRegion_AndScrollsWithTheWheel()
    {
        var view = ToolbarView;
        view.Bar = new Border { Width = 5000, Height = 20 };
        Pump();
        var scroller = view.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.True(scroller.Extent.Width > scroller.Viewport.Width);
        Assert.True(RectIn(view).Right <= RectIn(Frames.First()).Left);
        Assert.Equal(ScrollBarVisibility.Hidden, scroller.HorizontalScrollBarVisibility);

        _window.MouseWheel(RectIn(scroller).Center, new Vector(0, -1));
        Pump();

        Assert.True(scroller.Offset.X > 0);
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

        Assert.Equal(1280, metrics.MainWindowMinWidth);
        Assert.Equal(720, metrics.MainWindowMinHeight);
        Assert.True(_window.MinWidth > 0);
        Assert.True(_window.MinHeight > 0);
        Assert.True(_window.MinWidth <= metrics.MainWindowMinWidth);
        Assert.True(_window.MinHeight <= metrics.MainWindowMinHeight);
    }

    [AvaloniaFact]
    public void AWindow_GetsClicksFirst()
    {
        var hit = _window.InputHitTest(new Point(_window.Bounds.Width - 100, 400));

        Assert.NotSame(Canvas, hit);
        Assert.NotNull(hit);
    }

    [AvaloniaFact]
    public void ClickingTheCanvas_ThroughTheHost_ReachesItsTool()
    {
        var hovered = _canvas.ViewModel.HoveredCell;
        var middle = Canvas.Viewport!.CanvasBounds.Center;
        var point = new Point(middle.X / _window.RenderScaling, middle.Y / _window.RenderScaling);

        _window.MouseMove(point);
        Pump();

        Assert.NotEqual(hovered, _canvas.ViewModel.HoveredCell);
    }

    [AvaloniaFact]
    public void TheCanvas_KeepsItsMiddleClearOfWindowsAndToolbars()
    {
        var viewport = Canvas.Viewport!;
        var scaling = _window.RenderScaling;
        var canvas = RectIn(Canvas);

        var usableLeft = viewport.Insets.Left;
        var usableRight = viewport.ViewportSize.Width - viewport.Insets.Right;
        Assert.Equal(0, usableLeft);
        Assert.True(viewport.Insets.Right >= (canvas.Right - RectIn(Frames.First()).Left) * scaling - 1);
        Assert.True(viewport.Insets.Top >= (RectIn(ToolbarWith(Strings.ActionNew)).Bottom - canvas.Top) * scaling - 1);
        Assert.True(viewport.Insets.Bottom >= (canvas.Bottom - RectIn(ToolbarWith(Strings.ActionZoomIn)).Top) * scaling - 1);
        Assert.Equal((usableLeft + usableRight) / 2.0, viewport.CanvasBounds.Center.X, tolerance: 1);
    }

    [AvaloniaFact]
    public void ClosingEveryWindow_GivesTheCanvasTheirSpace()
    {
        var before = Canvas.Viewport!.Insets.Right;

        _docking.ToggleWindow(ShellDockingIds.LayersPanel);
        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        Pump();

        Assert.True(before > 0);
        Assert.Equal(0, Canvas.Viewport!.Insets.Right);
    }

    [AvaloniaFact]
    public void ClosingAWindow_GivesItsHeightToTheOtherOne()
    {
        var layers = Frames.First();
        var before = RectIn(layers).Height;

        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        Pump();

        Assert.True(RectIn(layers).Height > before);
    }

    [AvaloniaFact]
    public void AllowingAnySize_LiftsTheMinimumSize_EvenAfterTheWindowMoves()
    {
        _window.AllowAnySize();
        _window.KeepMinimumSizeOnScreen();

        Assert.Equal(0, _window.MinWidth);
        Assert.Equal(0, _window.MinHeight);
    }

    [AvaloniaFact]
    public void EveryToolbar_HasAGripAtEachEnd()
    {
        Assert.All(Toolbars, toolbar => Assert.Equal(2, toolbar.GetVisualDescendants().OfType<ToolbarGrip>().Count()));
    }

    [AvaloniaFact]
    public void DraggingAToolbar_LeavesTypingWithTheCanvas()
    {
        var grip = RectIn(ToolbarView.Grips[0]).Center;
        var somewhere = new Point(500, 400);

        _window.MouseDown(grip, MouseButton.Left);
        _window.MouseMove(somewhere);
        _window.MouseUp(somewhere, MouseButton.Left);
        Pump();

        Assert.True(_docking.Layout!.Toolbars.Single(toolbar => toolbar.ToolbarId == ShellDockingIds.FileToolbar).IsFloating);
        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void ClosingAToolbarsMenu_GivesTypingBackToTheCanvas()
    {
        var view = ToolbarView;
        var grip = RectIn(view.Grips[0]).Center;

        _window.MouseDown(grip, MouseButton.Right);
        _window.MouseUp(grip, MouseButton.Right);
        Pump();
        Assert.True(view.ContextMenu!.IsOpen);

        view.ContextMenu.Close();
        Pump();

        Assert.True(Canvas.IsFocused);
    }

    [AvaloniaFact]
    public void AWindow_ShowsItsTitle()
    {
        Assert.Contains(Strings.PanelLayers, Frames.Select(frame => frame.Title));
        Assert.Contains(Strings.PanelHistory, Frames.Select(frame => frame.Title));
    }

    [AvaloniaFact]
    public void TheToolbarsAndMenuBar_ShowInTheirPlaces()
    {
        Assert.All(Toolbars, toolbar => Assert.Contains(toolbar.GetVisualDescendants(),
            visual => visual is StackPanel { Name: ToolbarBuilder.ControlName }));
        Assert.Contains(Named<Border>("MenuBarStrip").GetVisualDescendants(), visual => visual is NativeMenuBar);
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
    public void TheHistoryPanel_StaysInStep()
    {
        Paint('a');
        var history = _canvas.Session.History;
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

    private List<ToolbarFrame> Toolbars => [.. _window.GetVisualDescendants().OfType<ToolbarFrame>()];

    private List<WindowFrame> Frames => [.. _window.GetVisualDescendants().OfType<WindowFrame>()];

    // Any one toolbar, for tests about what a toolbar does.
    private ToolbarFrame ToolbarView => ToolbarWith(Strings.ActionNew);

    private ToolbarFrame ToolbarWith(string label) => Toolbars.Single(toolbar =>
        toolbar.GetVisualDescendants().OfType<Button>()
            .Any(button => Equals(button.Content, UiText.WithoutAccessKey(label))));

    private ListBox HistoryList() =>
        _window.GetVisualDescendants().OfType<ListBox>().Single(list => list.Name == "Steps");

    private void Paint(char glyph)
    {
        var builder = new CellEditBuilder(_canvas.Document, _canvas.Document.ActiveFrame.Layers[0].Id);
        builder.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune(glyph)));
        _canvas.Session.History.Execute(builder.Build()!);
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
