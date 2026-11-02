// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using Avalonia;
using Avalonia.Controls;
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
            new ShellDock(new FakeAppSettings(), NullLogger<ShellDock>.Instance), new FakeAppSettings());

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
    public void TheEmptyPartOfAWidgetRow_BelongsToTheCanvas()
    {
        var menuBar = Chrome(ShellDockIds.MenuBarDock);
        var besideIt = RectIn(menuBar).Right + 50;

        Assert.Same(Canvas, _window.InputHitTest(new Point(besideIt, RectIn(menuBar).Center.Y)));
    }

    [AvaloniaTheory]
    [InlineData(ShellDockIds.MenuBarDock)]
    [InlineData(ShellDockIds.ToolbarDock)]
    [InlineData(ShellDockIds.StatusBarDock)]
    public void AFixedWidget_GetsClicksFirst_AndHugsItsContent(string dockId)
    {
        var chrome = Chrome(dockId);
        var hit = _window.InputHitTest(RectIn(chrome).Center) as Visual;

        Assert.NotNull(hit);
        Assert.NotSame(Canvas, hit);
        Assert.Contains(chrome, hit.GetVisualAncestors().Append(hit));
        Assert.NotEqual(Avalonia.Layout.HorizontalAlignment.Stretch, chrome.HorizontalAlignment);
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
        var point = new Point(300, 400);

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
        var side = Chrome(ShellDockIds.ToolbarDock);

        var usableLeft = viewport.Insets.Left;
        var usableRight = viewport.ViewportSize.Width - viewport.Insets.Right;
        Assert.True(viewport.Insets.Top >= RectIn(side).Bottom * scaling);
        Assert.True(viewport.Insets.Right >=
                    ShellLayoutMetrics.FromResources(Application.Current!).SideWidth * scaling);
        Assert.True(viewport.Insets.Bottom > 0);
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
    public void TheToolbarAndStatusBar_ShowInTheirWidgets()
    {
        Assert.Contains(Chrome(ShellDockIds.ToolbarDock).GetVisualDescendants(),
            visual => visual is StackPanel { Name: ToolbarBuilder.ControlName });
        Assert.Contains(Chrome(ShellDockIds.StatusBarDock).GetVisualDescendants(), visual => visual is StatusBarView);
        Assert.Contains(Chrome(ShellDockIds.MenuBarDock).GetVisualDescendants(), visual => visual is NativeMenuBar);
    }

    private ToolChromeControl Chrome(string dockId) => _window.GetVisualDescendants().OfType<ToolChromeControl>()
        .Single(chrome => chrome.DataContext is IDockable dockable && dockable.Id == dockId);

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
