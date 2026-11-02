// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Tests.Fakes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Dock.Avalonia.Controls;
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
    public void DefaultLayout_HasEveryPanel_WithTheMenuBarOutsideMacOS()
    {
        ShowWindow(hasMenuBarPanel: true);

        Assert.True(_dock.IsPanelOpen(ShellDockIds.MenuBarPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.ToolbarPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.StatusBarPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
    }

    [AvaloniaFact]
    public void OnMacOS_ThereIsNoMenuBarPanel()
    {
        ShowWindow(hasMenuBarPanel: false);

        Assert.False(_dock.IsPanelOpen(ShellDockIds.MenuBarPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.ToolbarPanel));
    }

    [AvaloniaFact]
    public void TheMenuBarToolbarAndStatusBar_CantBeClosedMovedOrFloated()
    {
        var host = ShowWindow(hasMenuBarPanel: true);
        var root = (IRootDock)host.Layout!;

        foreach (var id in new[] { ShellDockIds.MenuBarPanel, ShellDockIds.ToolbarPanel, ShellDockIds.StatusBarPanel })
        {
            var panel = ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == id);
            Assert.False(panel.CanClose);
            Assert.False(panel.CanDrag);
            Assert.False(panel.CanFloat);
        }
    }

    [AvaloniaFact]
    public void TogglingAPanel_ClosesItAndBringsItBack_AndEachChangeIsAnnounced()
    {
        ShowWindow(hasMenuBarPanel: true);
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
        var host = ShowWindow(hasMenuBarPanel: true);
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.TogglePanel(ShellDockIds.LayersPanel);
        Pump();

        _dock.TogglePanel(ShellDockIds.LayersPanel);
        Pump();

        var root = (IRootDock)host.Layout!;
        var layers = ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.LayersPanel);
        var metrics = ShellLayoutMetrics.FromResources(Application.Current!);
        Assert.Equal(metrics.SideWidth, layers.Owner!.MaxWidth);
        Assert.False(ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.CanvasDock).Owner is null);
    }

    [AvaloniaFact]
    public void ALayout_SurvivesSavingAndABuiltFromScratchWindow()
    {
        // On macOS each opening builds a new window (ADR 0023), so restoring has to work more than once.
        ShowWindow(hasMenuBarPanel: true);
        _dock.TogglePanel(ShellDockIds.HistoryPanel);
        _dock.SaveAndDetach();

        for (var opening = 0; opening < 2; opening++)
        {
            ShowWindow(hasMenuBarPanel: true);
            Assert.False(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
            Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
            _dock.SaveAndDetach();
        }
    }

    [AvaloniaFact]
    public void AFloatingPanel_SurvivesSaving_AndStaysInsideTheWindow()
    {
        var host = ShowWindow(hasMenuBarPanel: true);
        var root = (IRootDock)host.Layout!;
        var factory = (ShellDockFactory)host.Factory!;
        factory.FloatDockable(ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel));
        Pump();
        Assert.Equal(DockFloatingWindowHostMode.Managed, root.FloatingWindowHostMode);
        Assert.Single(root.Windows!);
        _dock.SaveAndDetach();

        var rebuilt = ShowWindow(hasMenuBarPanel: true);

        var restored = (IRootDock)rebuilt.Layout!;
        Assert.Single(restored.Windows!);
        Assert.True(_dock.IsPanelOpen(ShellDockIds.HistoryPanel));
    }

    [AvaloniaFact]
    public void ResetLayout_RestoresEveryPanel_AndClosesFloatingOnes()
    {
        var host = ShowWindow(hasMenuBarPanel: true);
        var factory = (ShellDockFactory)host.Factory!;
        _dock.TogglePanel(ShellDockIds.LayersPanel);
        var root = (IRootDock)host.Layout!;
        factory.FloatDockable(ShellDockFactory.AllDockables(root).Single(dockable => dockable.Id == ShellDockIds.HistoryPanel));
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

        ShowWindow(hasMenuBarPanel: true);

        Assert.True(_dock.IsPanelOpen(ShellDockIds.LayersPanel));
        Assert.True(_dock.IsPanelOpen(ShellDockIds.MenuBarPanel));
    }

    [AvaloniaFact]
    public void ALayoutSavedWithoutTheMenuBar_IsNotUsedWhereTheMenuBarIsNeeded()
    {
        ShowWindow(hasMenuBarPanel: false);
        _dock.SaveAndDetach();

        ShowWindow(hasMenuBarPanel: true);

        Assert.True(_dock.IsPanelOpen(ShellDockIds.MenuBarPanel));
    }

    [AvaloniaFact]
    public void WindowMenuToggles_FollowThePanels()
    {
        var actions = new ActionRegistry([new ShellDockActions(_dock)]);
        ShowWindow(hasMenuBarPanel: true);
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

    private DockControl ShowWindow(bool hasMenuBarPanel)
    {
        var host = new DockControl { InitializeFactory = true, EnableManagedWindowLayer = true };
        var window = new Window { Width = 1000, Height = 700, Content = host };
        _windows.Add(window);

        var views = new Dictionary<string, Func<Control>>(StringComparer.Ordinal);
        _dock.Attach(host, new ShellDockFactory(ShellLayoutMetrics.FromResources(Application.Current!), hasMenuBarPanel),
            new ShellPanelTemplates(views));

        window.Show();
        Pump();
        return host;
    }

    private static void Pump() => Dispatcher.UIThread.RunJobs();
}
