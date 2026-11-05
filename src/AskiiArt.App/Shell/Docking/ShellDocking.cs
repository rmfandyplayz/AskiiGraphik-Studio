// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using AskiiArt.App.Settings;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The open window's layout: what the Window menu's actions work on, and what gets saved (ADR 0035). On
/// macOS a window can be closed and another built later, so a layout attaches to each window in turn;
/// between windows there's nothing to act on.
/// </summary>
internal sealed partial class ShellDocking(IAppSettings settings, ILogger<ShellDocking> logger)
{
    internal const string LayoutSettingKey = "dockingLayout";

    private DockingHost? _host;
    private ShellCatalog? _catalog;
    private ShellLayoutMetrics? _metrics;
    private DockingLayout? _layout;
    private ToolbarDragController? _toolbarDrags;

    /// <summary>Raised when the layout changes, which a drag only does when it ends.</summary>
    public event EventHandler? LayoutChanged;

    /// <summary>Raised when a sub-toolbar shows or goes.</summary>
    public event EventHandler? SubToolbarChanged;

    /// <summary>Shows the saved layout in <paramref name="host"/>, or the default one if there's none to use.</summary>
    public void Attach(DockingHost host, ShellCatalog catalog, ShellLayoutMetrics metrics)
    {
        _host = host;
        _catalog = catalog;
        _metrics = metrics;
        _toolbarDrags = new ToolbarDragController(host, metrics, () => _layout, layout => Show(layout, animate: true));
        host.ToolbarFrameCreated += OnToolbarFrameCreated;

        Show(LoadSavedLayout() ?? DefaultLayout(), animate: false);
    }

    /// <summary>Saves the layout and lets go of the window, which is closing.</summary>
    public void SaveAndDetach()
    {
        if (LayoutToChange() is not { } layout)
            return;

        Save(layout);

        _host!.ToolbarFrameCreated -= OnToolbarFrameCreated;
        _host = null;
        _catalog = null;
        _metrics = null;
        _toolbarDrags = null;
        _layout = null;
        SetSubToolbarParent(null);
    }

    /// <summary>The layout as it stands, if there's a window. A drag only changes it when it ends.</summary>
    public DockingLayout? Layout => _layout;

    /// <summary>The toolbar that has the sub-toolbar, if there is one.</summary>
    public string? SubToolbarParent { get; private set; }

    public bool IsWindowOpen(string windowId) => _layout?.IsWindowOpen(windowId) == true;

    public bool IsToolbarVisible(string toolbarId) => _layout?.IsToolbarVisible(toolbarId) == true;

    /// <summary>Which way a toolbar runs. With no window there's nothing to ask, and it says horizontal.</summary>
    public ToolbarOrientation ToolbarOrientationOf(string toolbarId) =>
        ToolbarNamed(toolbarId)?.Orientation ?? ToolbarOrientation.Horizontal;

    /// <summary>Whether the user can choose a toolbar's direction: on a corner or floating, not on an edge middle.</summary>
    public bool CanChooseToolbarOrientation(string toolbarId) => ToolbarNamed(toolbarId)?.CanChooseOrientation == true;

    /// <summary>Closes the window if it's open, and opens it otherwise.</summary>
    public void ToggleWindow(string windowId)
    {
        if (LayoutToChange() is not { } layout || _metrics is not { } metrics)
            return;

        Show(
            layout.IsWindowOpen(windowId)
                ? LayoutEdits.CloseWindow(layout, windowId)
                : LayoutEdits.OpenWindow(layout, windowId, metrics.WidgetWidth),
            animate: true);
    }

    /// <summary>Hides the toolbar if it's showing, and shows it otherwise.</summary>
    public void ToggleToolbar(string toolbarId)
    {
        if (LayoutToChange() is not { } layout)
            return;

        Show(
            layout.IsToolbarVisible(toolbarId)
                ? LayoutEdits.HideToolbar(layout, toolbarId)
                : LayoutEdits.ShowToolbar(layout, toolbarId, WhereToFloatIfItsAnchorIsTaken(layout, toolbarId)),
            animate: true);
    }

    /// <summary>Hides a toolbar.</summary>
    public void HideToolbar(string toolbarId)
    {
        if (LayoutToChange() is { } layout)
            Show(LayoutEdits.HideToolbar(layout, toolbarId), animate: true);
    }

    /// <summary>Changes which way a toolbar runs, if the user can choose that where it is.</summary>
    public void SetToolbarOrientation(string toolbarId, ToolbarOrientation orientation)
    {
        if (LayoutToChange() is { } layout)
            Show(LayoutEdits.SetToolbarOrientation(layout, toolbarId, orientation), animate: true);
    }

    /// <summary>
    /// Shows a sub-toolbar, in place of any other, since only one tool is active. It isn't part of the layout, and isn't
    /// saved, because the tool decides it. If the toolbar isn't showing, it shows when the toolbar does.
    /// </summary>
    /// <param name="parentToolbarId">The toolbar it belongs to.</param>
    /// <param name="createView">Makes its buttons.</param>
    public void ShowSubToolbar(string parentToolbarId, Func<Control> createView)
    {
        if (LayoutToChange() is null)
            return;

        _host!.ShowSubToolbar(parentToolbarId, createView);
        SetSubToolbarParent(parentToolbarId);
    }

    /// <summary>Takes away the sub-toolbar, if there is one.</summary>
    public void HideSubToolbar()
    {
        if (LayoutToChange() is null)
            return;

        _host!.HideSubToolbar();
        SetSubToolbarParent(null);
    }

    /// <summary>Goes back to the default layout.</summary>
    public void ResetLayout()
    {
        if (LayoutToChange() is not null)
            Show(DefaultLayout(), animate: true);
    }

    // The layout for a change to start from. A change can arrive during a drag, from a shortcut for example, and
    // it wins: the drag is cancelled first, so it can't overwrite the change when it ends.
    private DockingLayout? LayoutToChange()
    {
        _toolbarDrags?.Cancel();
        return _layout;
    }

    private void Show(DockingLayout layout, bool animate)
    {
        _layout = layout;
        _host!.Show(layout, _catalog!, _metrics!, animate);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetSubToolbarParent(string? parentToolbarId)
    {
        SubToolbarParent = parentToolbarId;
        SubToolbarChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnToolbarFrameCreated(object? sender, ToolbarFrameCreatedEventArgs e) =>
        _toolbarDrags?.Watch(e.ToolbarId, e.Frame);

    private ToolbarPlacement? ToolbarNamed(string toolbarId) =>
        _layout?.Toolbars.FirstOrDefault(toolbar => toolbar.ToolbarId == toolbarId);

    private LayoutPoint WhereToFloatIfItsAnchorIsTaken(DockingLayout layout, string toolbarId)
    {
        if (ToolbarNamed(toolbarId) is not { } toolbar || !_catalog!.Toolbars.ContainsKey(toolbarId) ||
            !layout.IsAnchorTaken(toolbar.Anchor, toolbarId))
        {
            return default;
        }

        var measurements = _host!.MeasurementsIncluding(toolbar);
        return LayoutSolver.WhereAloneOnItsAnchor(layout, toolbarId, measurements, _metrics!.Docking);
    }

    private DockingCatalog Catalog()
    {
        var defaults = ShellDefaultLayout.Create(_metrics!.WidgetWidth);
        return new DockingCatalog(
            [.. defaults.OpenWindowIds().Where(_catalog!.Windows.ContainsKey)],
            [.. defaults.Toolbars.Where(toolbar => _catalog.Toolbars.ContainsKey(toolbar.ToolbarId))],
            _metrics.WidgetWidth);
    }

    private DockingLayout DefaultLayout() =>
        LayoutRepair.Repair(ShellDefaultLayout.Create(_metrics!.WidgetWidth), Catalog());

    private DockingLayout? LoadSavedLayout()
    {
        if (settings.GetText(LayoutSettingKey) is not { } saved)
            return null;

        if (LayoutJson.TryRead(saved) is { } layout)
            return LayoutRepair.Repair(layout, Catalog());

        // Losing the layout costs little, so there's no need to bother the user about it.
        LogLayoutUnreadable(logger);
        return null;
    }

    private void Save(DockingLayout layout)
    {
        try
        {
            settings.SetText(LayoutSettingKey, LayoutJson.Write(layout));
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            LogLayoutNotSaved(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The saved layout couldn't be read. Using the default layout.")]
    private static partial void LogLayoutUnreadable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The layout couldn't be saved.")]
    private static partial void LogLayoutNotSaved(ILogger logger, Exception exception);
}
