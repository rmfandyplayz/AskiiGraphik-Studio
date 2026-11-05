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
using Avalonia.Threading;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Shell.Docking;

/// <summary>The layout in a bare window: opening and closing, saving and restoring.</summary>
public sealed class ShellDockingTests : IDisposable
{
    private readonly FakeAppSettings _settings = new();
    private readonly List<Window> _windows = [];
    private readonly ShellDocking _docking;

    public ShellDockingTests()
    {
        _docking = new ShellDocking(_settings, NullLogger<ShellDocking>.Instance);
    }

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheDefaultLayout_HasEveryWindowAndToolbar()
    {
        ShowWindow();

        Assert.True(_docking.IsWindowOpen(ShellDockingIds.LayersPanel));
        Assert.True(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.FileToolbar));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.ToolsToolbar));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.EditToolbar));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.ViewToolbar));
    }

    [AvaloniaFact]
    public void TheDefaultLayout_HasTheWindowsAsWidgetsOnTheRight_AndTheToolbarsOnTheirAnchors()
    {
        var layout = ShellDefaultLayout.Create(240);

        var side = Assert.Single(layout.Sides);
        Assert.Equal((SideEdge.Right, SideKind.Widgets), (side.Edge, side.Kind));
        Assert.Equal([ShellDockingIds.LayersPanel, ShellDockingIds.HistoryPanel], layout.OpenWindowIds());
        Assert.Equal(ToolbarAnchor.TopLeft, Anchor(layout, ShellDockingIds.FileToolbar));
        Assert.Equal(ToolbarAnchor.BottomMiddle, Anchor(layout, ShellDockingIds.ToolsToolbar));
        Assert.Equal(ToolbarAnchor.TopRight, Anchor(layout, ShellDockingIds.EditToolbar));
        Assert.Equal(ToolbarAnchor.BottomRight, Anchor(layout, ShellDockingIds.ViewToolbar));
    }

    [AvaloniaFact]
    public void TogglingAWindow_ClosesItAndBringsItBack_AndEachChangeIsAnnounced()
    {
        var host = ShowWindow();
        var changes = 0;
        _docking.LayoutChanged += (_, _) => changes++;
        var viewsBefore = host.Children.OfType<WindowFrame>().Count();

        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        Assert.False(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
        Assert.Equal(viewsBefore - 1, host.Children.OfType<WindowFrame>().Count());
        Assert.Equal(1, changes);

        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        Assert.True(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
        Assert.Equal(viewsBefore, host.Children.OfType<WindowFrame>().Count());
        Assert.Equal(2, changes);
    }

    [AvaloniaFact]
    public void TogglingAToolbar_HidesItAndBringsItBack()
    {
        var host = ShowWindow();
        var viewsBefore = host.Children.Count;

        _docking.ToggleToolbar(ShellDockingIds.ViewToolbar);
        Assert.False(_docking.IsToolbarVisible(ShellDockingIds.ViewToolbar));
        Assert.Equal(viewsBefore - 1, host.Children.Count);

        _docking.ToggleToolbar(ShellDockingIds.ViewToolbar);
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.ViewToolbar));
        Assert.Equal(viewsBefore, host.Children.Count);
    }

    [AvaloniaFact]
    public void ReopeningAClosedWindow_PutsItBackWhereItWas()
    {
        var host = ShowWindow();
        var before = FrameFor(host, ShellDockingIds.LayersPanel).Bounds;

        _docking.ToggleWindow(ShellDockingIds.LayersPanel);
        Pump();
        _docking.ToggleWindow(ShellDockingIds.LayersPanel);
        Pump();

        Assert.Equal(before, FrameFor(host, ShellDockingIds.LayersPanel).Bounds);
    }

    [AvaloniaFact]
    public void AWindowThatStaysOpen_KeepsItsView()
    {
        var host = ShowWindow();
        var layers = FrameFor(host, ShellDockingIds.LayersPanel);

        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        _docking.ResetLayout();

        Assert.Same(layers, FrameFor(host, ShellDockingIds.LayersPanel));
    }

    [AvaloniaFact]
    public void AWindowWithoutRoom_ShowsOnlyItsTitle_UntilThereIsRoomAgain()
    {
        var host = ShowWindow();
        var window = _windows[^1];
        var history = FrameFor(host, ShellDockingIds.HistoryPanel);
        Assert.False(history.IsTitleOnly);

        window.Height = 200;
        Pump();
        Assert.True(history.IsTitleOnly);
        Assert.False(FrameFor(host, ShellDockingIds.LayersPanel).IsTitleOnly);

        window.Height = 700;
        Pump();
        Assert.False(history.IsTitleOnly);
    }

    [AvaloniaFact]
    public void ALayout_SurvivesSavingAndABuiltFromScratchWindow()
    {
        // On macOS each opening builds a new window (ADR 0023), so restoring has to work more than once.
        ShowWindow();
        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        _docking.ToggleToolbar(ShellDockingIds.ViewToolbar);
        _docking.SaveAndDetach();

        for (var opening = 0; opening < 2; opening++)
        {
            ShowWindow();
            Assert.False(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
            Assert.True(_docking.IsWindowOpen(ShellDockingIds.LayersPanel));
            Assert.False(_docking.IsToolbarVisible(ShellDockingIds.ViewToolbar));
            Assert.True(_docking.IsToolbarVisible(ShellDockingIds.FileToolbar));
            _docking.SaveAndDetach();
        }
    }

    [AvaloniaFact]
    public void ResetLayout_BringsBackEverything()
    {
        ShowWindow();
        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        _docking.ToggleWindow(ShellDockingIds.LayersPanel);
        _docking.ToggleToolbar(ShellDockingIds.FileToolbar);

        _docking.ResetLayout();

        Assert.True(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
        Assert.True(_docking.IsWindowOpen(ShellDockingIds.LayersPanel));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.FileToolbar));
    }

    [AvaloniaTheory]
    [InlineData("not json")]
    [InlineData("{ \"$type\": \"Some.Type.That.Does.Not.Exist\" }")]
    public void AnUnreadableSavedLayout_FallsBackToTheDefault(string saved)
    {
        _settings.SetText(ShellDocking.LayoutSettingKey, saved);

        ShowWindow();

        Assert.True(_docking.IsWindowOpen(ShellDockingIds.LayersPanel));
        Assert.True(_docking.IsToolbarVisible(ShellDockingIds.FileToolbar));
    }

    [AvaloniaFact]
    public void ALayoutFromTheOldDockLibrary_IsIgnored()
    {
        _settings.SetText("dockLayoutV2", "{ \"Dock\": \"layout\" }");

        ShowWindow();

        Assert.True(_docking.IsWindowOpen(ShellDockingIds.LayersPanel));
    }

    [AvaloniaFact]
    public void WindowMenuToggles_FollowWhatTheyToggle()
    {
        var actions = new ActionRegistry([new WindowMenuActions(_docking)]);
        ShowWindow();

        foreach (var id in new[] { WindowMenuActionIds.ToggleHistory, WindowMenuActionIds.ToggleToolsToolbar })
        {
            var toggle = actions.Get(id);
            var notifications = 0;
            toggle.CheckedState!.Changed += (_, _) => notifications++;
            Assert.True(toggle.CheckedState.IsChecked);

            toggle.Command.Execute(null);

            Assert.False(toggle.CheckedState.IsChecked);
            Assert.True(notifications > 0);
        }
    }

    [AvaloniaFact]
    public void WithNoWindow_TheActionsDoNothing()
    {
        _docking.ToggleWindow(ShellDockingIds.HistoryPanel);
        _docking.ToggleToolbar(ShellDockingIds.FileToolbar);
        _docking.ResetLayout();
        _docking.SaveAndDetach();

        Assert.False(_docking.IsWindowOpen(ShellDockingIds.HistoryPanel));
        Assert.False(_docking.IsToolbarVisible(ShellDockingIds.FileToolbar));
        Assert.Null(_settings.GetText(ShellDocking.LayoutSettingKey));
    }

    private static ToolbarAnchor Anchor(DockingLayout layout, string toolbarId) =>
        layout.Toolbars.Single(toolbar => toolbar.ToolbarId == toolbarId).Anchor;

    private static WindowFrame FrameFor(DockingHost host, string windowId) =>
        host.Children.OfType<WindowFrame>().Single(frame => frame.Title == windowId);

    private DockingHost ShowWindow()
    {
        var host = new DockingHost();
        var window = new Window { Width = 1000, Height = 700, Content = host };
        _windows.Add(window);

        var metrics = ShellLayoutMetrics.FromResources(Application.Current!);
        ShellWindowEntry Window(string id) => new(id, id, metrics.WindowMinHeight, () => new Border());
        ShellToolbarEntry Toolbar(string id) => new(id, () => new Border { Width = 100, Height = 30 });
        var catalog = new ShellCatalog(
            [Window(ShellDockingIds.LayersPanel), Window(ShellDockingIds.HistoryPanel)],
            [
                Toolbar(ShellDockingIds.FileToolbar), Toolbar(ShellDockingIds.ToolsToolbar),
                Toolbar(ShellDockingIds.EditToolbar), Toolbar(ShellDockingIds.ViewToolbar)
            ]);

        _docking.Attach(host, catalog, metrics);

        window.Show();
        Pump();
        return host;
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
