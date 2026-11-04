// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using AskiiArt.App.Settings;
using AskiiArt.Docking.Layouts;
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

    /// <summary>Raised when a window opens or closes, or a toolbar shows or hides.</summary>
    public event EventHandler? LayoutChanged;

    /// <summary>Shows the saved layout in <paramref name="host"/>, or the default one if there's none to use.</summary>
    public void Attach(DockingHost host, ShellCatalog catalog, ShellLayoutMetrics metrics)
    {
        _host = host;
        _catalog = catalog;
        _metrics = metrics;

        Show(LoadSavedLayout() ?? DefaultLayout());
    }

    /// <summary>Saves the layout and lets go of the window, which is closing.</summary>
    public void SaveAndDetach()
    {
        if (_layout is null)
            return;

        Save(_layout);

        _host = null;
        _catalog = null;
        _metrics = null;
        _layout = null;
    }

    public bool IsWindowOpen(string windowId) => _layout?.IsWindowOpen(windowId) == true;

    public bool IsToolbarVisible(string toolbarId) => _layout?.IsToolbarVisible(toolbarId) == true;

    /// <summary>Closes the window if it's open, and opens it otherwise.</summary>
    public void ToggleWindow(string windowId)
    {
        if (_layout is not { } layout || _metrics is not { } metrics)
            return;

        Show(layout.IsWindowOpen(windowId)
            ? LayoutEdits.CloseWindow(layout, windowId)
            : LayoutEdits.OpenWindow(layout, windowId, metrics.WidgetWidth));
    }

    /// <summary>Hides the toolbar if it's showing, and shows it otherwise.</summary>
    public void ToggleToolbar(string toolbarId)
    {
        if (_layout is { } layout)
            Show(LayoutEdits.SetToolbarVisible(layout, toolbarId, !layout.IsToolbarVisible(toolbarId)));
    }

    /// <summary>Goes back to the default layout.</summary>
    public void ResetLayout()
    {
        if (_layout is not null)
            Show(DefaultLayout());
    }

    private void Show(DockingLayout layout)
    {
        _layout = layout;
        _host!.Show(layout, _catalog!, _metrics!.Docking);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
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
