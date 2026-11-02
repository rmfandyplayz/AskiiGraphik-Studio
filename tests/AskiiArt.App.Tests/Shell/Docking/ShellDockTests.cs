// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Tests.Fakes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>The panel layout, in a bare window: opening and closing panels, saving and restoring.</summary>
public sealed class ShellDockTests : IDisposable
{
    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDock _dock;

    public ShellDockTests()
    {
        _dock = new ShellDock(_settings, NullLogger<ShellDock>.Instance);
    }

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void DefaultLayout_HasEveryPanel()
    {
        ShowWindow();

        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
    }

    [AvaloniaFact]
    public void TogglingAPanel_ClosesItAndBringsItBack_AndEachChangeIsAnnounced()
    {
        ShowWindow();
        var changes = 0;
        _dock.PanelsChanged += (_, _) => changes++;

        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        Assert.False(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
        Assert.True(changes > 0);

        changes = 0;
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
        Assert.True(changes > 0);
    }

    [AvaloniaFact]
    public void ClosingBothMovablePanels_ThenReopeningOne_PutsItBesideTheCanvasAtTheSideWidth()
    {
        var host = ShowWindow();
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.TogglePanel(ShellDockIds.LayersPanel);
        Pump();

        _dock.TogglePanel(ShellDockIds.LayersPanel);
        Pump();

        var root = (IRootDock)host.Layout!;
        var layers = ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.LayersPanel);
        var metrics = ShellLayoutMetrics.FromResources(Application.Current!);
        Assert.Equal(metrics.SideWidth, layers.Owner!.MaxWidth);
        Assert.False(ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.CanvasDock)
            .Owner is null);
    }

    [AvaloniaFact]
    public void ALayout_SurvivesSavingAndABuiltFromScratchWindow()
    {
        // On macOS each opening builds a new window (ADR 0023), so restoring has to work more than once.
        ShowWindow();
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.SaveAndDetach();

        for (var opening = 0; opening < 2; opening++)
        {
            ShowWindow();
            Assert.False(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
            Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
            _dock.SaveAndDetach();
        }
    }

    [AvaloniaFact]
    public void AFloatingPanel_SurvivesSaving_AndStaysInsideTheWindow()
    {
        var host = ShowWindow();
        var root = (IRootDock)host.Layout!;
        var factory = (ShellDockFactory)host.Factory!;
        factory.FloatDockable(ShellDockFactory.AllDockables(root)
            .Single(dockable => dockable.Id == ShellDockIds.HistoryPanel));
        Pump();
        Assert.Equal(DockFloatingWindowHostMode.Managed, root.FloatingWindowHostMode);
        Assert.Single(root.Windows!);
        _dock.SaveAndDetach();

        var rebuilt = ShowWindow();

        var restored = (IRootDock)rebuilt.Layout!;
        Assert.Single(restored.Windows!);
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
    }

    [AvaloniaFact]
    public void AFloatingPanel_ComesBackOnScreen_WhereItWas()
    {
        var host = ShowWindow();
        var factory = (ShellDockFactory)host.Factory!;
        factory.FloatDockable(Panel(host, ShellDockIds.HistoryPanel));
        Pump();
        ((IRootDock)host.Layout!).Windows!.Single().Host!.SetPosition(150, 120);
        _dock.SaveAndDetach();

        var rebuilt = ShowWindow();

        var restored = ((IRootDock)rebuilt.Layout!).Windows!.Single();
        Assert.Contains(restored.Host, ((ShellDockFactory)rebuilt.Factory!).HostWindows);
        restored.Host!.GetPosition(out var x, out var y);
        Assert.Equal((150, 120), (x, y));
        Assert.Contains(rebuilt.GetVisualDescendants().OfType<ToolChromeControl>(),
            chrome => chrome.DataContext is IDock { ActiveDockable.Id: ShellDockIds.HistoryPanel });
    }

    [AvaloniaFact]
    public void AFloatingPanelSavedOutsideASmallerWindow_ComesBackInsideIt()
    {
        var host = ShowWindow();
        ((ShellDockFactory)host.Factory!).FloatDockable(Panel(host, ShellDockIds.HistoryPanel));
        Pump();
        var floating = ((IRootDock)host.Layout!).Windows!.Single();
        floating.Host!.SetSize(200, 100);
        floating.Host.SetPosition(5000, 5000);
        _dock.SaveAndDetach();

        var rebuilt = ShowWindow();

        ((IRootDock)rebuilt.Layout!).Windows!.Single().Host!.GetPosition(out var x, out var y);
        Assert.Equal((rebuilt.Bounds.Width - 200, rebuilt.Bounds.Height - 100), (x, y));
    }

    [AvaloniaFact]
    public void FloatingAPanelThatAlreadyFloatsAlone_DoesNothing()
    {
        var host = ShowWindow();
        var factory = (ShellDockFactory)host.Factory!;
        factory.FloatDockable(Panel(host, ShellDockIds.HistoryPanel));
        Pump();
        var floating = ((IRootDock)host.Layout!).Windows!.Single();

        factory.FloatDockable(Panel(host, ShellDockIds.HistoryPanel));
        Pump();

        Assert.Same(floating, ((IRootDock)host.Layout!).Windows!.Single());
    }

    [AvaloniaFact]
    public void ReopeningAPanel_DocksIt_EvenWhenTheOnlyOtherPanelFloats()
    {
        var host = ShowWindow();
        var root = (IRootDock)host.Layout!;
        _dock.TogglePanel(ShellDockIds.LayersPanel);
        ((ShellDockFactory)host.Factory!).FloatDockable(Panel(host, ShellDockIds.HistoryPanel));
        Pump();

        _dock.TogglePanel(ShellDockIds.LayersPanel);
        Pump();

        var floatingLayout = (IRootDock)root.Windows!.Single().Layout!;
        Assert.DoesNotContain(ShellDockFactory.AllDockables(floatingLayout),
            dockable => dockable.Id == ShellDockIds.LayersPanel);
        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
    }

    [AvaloniaFact]
    public void APanelDockedAroundTheWholeLayout_SurvivesSaving()
    {
        var host = ShowWindow();
        var root = (IRootDock)host.Layout!;
        var outermost = root.VisibleDockables!.OfType<IDock>().Single();
        var dock = new DockManager(new DockService());
        Assert.True(dock.ValidateDockable(Panel(host, ShellDockIds.HistoryPanel), outermost, DragAction.Move,
            DockOperation.Left, bExecute: true));
        Pump();
        _dock.SaveAndDetach();

        var rebuilt = ShowWindow();

        Assert.Contains(rebuilt.GetVisualDescendants().OfType<ToolChromeControl>(),
            chrome => chrome.DataContext is IDock { ActiveDockable.Id: ShellDockIds.HistoryPanel });
        Assert.Contains(rebuilt.GetVisualDescendants().OfType<ToolChromeControl>(),
            chrome => chrome.DataContext is IDock { ActiveDockable.Id: ShellDockIds.LayersPanel });
    }

    [AvaloniaFact]
    public void ResetLayout_RestoresEveryPanel_AndClosesFloatingOnes()
    {
        var host = ShowWindow();
        var factory = (ShellDockFactory)host.Factory!;
        _dock.TogglePanel(ShellDockIds.LayersPanel);
        var root = (IRootDock)host.Layout!;
        factory.FloatDockable(ShellDockFactory.AllDockables(root)
            .Single(dockable => dockable.Id == ShellDockIds.HistoryPanel));
        Pump();

        _dock.ResetLayout();
        Pump();

        var reset = (IRootDock)host.Layout!;
        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
        Assert.True(reset.Windows is null || reset.Windows.Count == 0);
    }

    [AvaloniaTheory]
    [InlineData("not json")]
    [InlineData("{ \"$type\": \"Some.Type.That.Does.Not.Exist\" }")]
    public void AnUnreadableSavedLayout_FallsBackToTheDefault(string saved)
    {
        _settings.SetText(ShellDock.LayoutSettingKey, saved);

        ShowWindow();

        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
    }

    [AvaloniaFact]
    public void WindowMenuToggles_FollowThePanels()
    {
        var actions = new ActionRegistry([new ShellDockActions(_dock)]);
        ShowWindow();
        var toggle = actions.Get(ShellDockActionIds.ToggleHistory);
        var notifications = 0;
        toggle.CheckedState!.Changed += (_, _) => notifications++;
        Assert.True(toggle.CheckedState.IsChecked);

        toggle.Command.Execute(null);

        Assert.False(toggle.CheckedState.IsChecked);
        Assert.True(notifications > 0);
    }

    [AvaloniaFact]
    public void WithNoWindow_TheActionsDoNothing()
    {
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.ResetLayout();
        _dock.SaveAndDetach();

        Assert.False(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
        Assert.Equal(Strings.PanelHistory, ShellDockIds.PanelTitle(ShellDockIds.HistoryPanel));
    }

    private DockControl ShowWindow()
    {
        var host = new DockControl { InitializeFactory = true, EnableManagedWindowLayer = true };
        var window = new Window { Width = 1000, Height = 700, Content = host };
        _windows.Add(window);

        var views = new Dictionary<string, Func<Control>>(StringComparer.Ordinal);
        _dock.Attach(host, new ShellDockFactory(ShellLayoutMetrics.FromResources(Application.Current!)),
            new ShellPanelTemplates(views));

        window.Show();
        Pump();
        return host;
    }

    private static IDockable Panel(DockControl host, string panelId) =>
        ShellDockFactory.AllDockables((IRootDock)host.Layout!).Single(dockable => dockable.Id == panelId);

    private static void Pump()
    {
        for (var pass = 0; pass < 5; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
