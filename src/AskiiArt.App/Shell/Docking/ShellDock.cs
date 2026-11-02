// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using AskiiArt.App.Settings;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Serializer.SystemTextJson;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The open window's panel layout: what the Window menu's actions work on, and what gets saved
/// (ADR 0025). On macOS a window can be closed and another built later, so a layout attaches to each
/// window in turn; between windows there's nothing to act on.
/// </summary>
internal sealed partial class ShellDock(IAppSettings settings, ILogger<ShellDock> logger)
{
    // Changes when the default layout gains something a saved one wouldn't have, because a saved layout
    // without it means the user closed it. The toolbar joined the layout in V2 (ADR 0029).
    internal const string LayoutSettingKey = "dockLayoutV2";

    private static readonly DockSerializer _serializer = new();

    private DockControl? _host;
    private ShellDockFactory? _factory;

    /// <summary>Raised when a panel opens or closes.</summary>
    public event EventHandler? PanelsChanged;

    /// <summary>Shows the saved layout in <paramref name="host"/>, or the default one if it can't be used.</summary>
    public void Attach(DockControl host, ShellDockFactory factory, IDataTemplate panels)
    {
        _host = host;
        _factory = factory;

        host.DataTemplates.Add(panels);
        host.Factory = factory;
        factory.DockableClosed += OnPanelsChanged;
        factory.DockableAdded += OnPanelsChanged;
        factory.WindowClosed += OnPanelsChanged;
        factory.WindowMoveDragBegin += OnFloatingPanelDragStarted;
        factory.WindowMoveDragEnd += OnFloatingPanelDragEnded;

        host.Loaded += ShowFloatingPanels;

        ShowLayout(LoadSavedLayout(factory) ?? factory.CreateLayout());
        PanelsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Saves the layout and lets go of the window, which is closing.</summary>
    public void SaveAndDetach()
    {
        if (_host is null || _factory is null)
            return;

        Save();

        _host.Loaded -= ShowFloatingPanels;
        _factory.DockableClosed -= OnPanelsChanged;
        _factory.DockableAdded -= OnPanelsChanged;
        _factory.WindowClosed -= OnPanelsChanged;
        _factory.WindowMoveDragBegin -= OnFloatingPanelDragStarted;
        _factory.WindowMoveDragEnd -= OnFloatingPanelDragEnded;
        _host = null;
        _factory = null;
    }

    public bool IsPanelOpen(string panelId) =>
        Root is { } root && ShellDockFactory.AllDockables(root).Any(dockable => dockable.Id == panelId);

    /// <summary>Closes the panel if it's open, and opens it otherwise.</summary>
    public void TogglePanel(string panelId)
    {
        if (Root is not { } root || _factory is null)
            return;

        if (ShellDockFactory.AllDockables(root).FirstOrDefault(dockable => dockable.Id == panelId) is { } panel)
            _factory.CloseDockable(panel);
        else
            _factory.OpenPanel(root, panelId);

        PanelsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Goes back to the default layout, closing any floating panels.</summary>
    public void ResetLayout()
    {
        if (_factory is null)
            return;

        if (Root is { Windows: { } windows })
        {
            foreach (var window in windows.ToArray())
                _factory.RemoveWindow(window);
        }

        ShowLayout(_factory.CreateLayout());
        PanelsChanged?.Invoke(this, EventArgs.Empty);
    }

    private IRootDock? Root => _host?.Layout as IRootDock;

    private void ShowLayout(IRootDock layout)
    {
        _factory!.InitLayout(layout);
        _host!.Layout = layout;
    }

    // Dock only shows a window when it's made or told to: a restored layout's windows stay hidden until
    // then. This waits for the host to have a size, because a window can reopen smaller than it was,
    // and a panel saved outside it would be out of reach.
    private void ShowFloatingPanels(object? sender, RoutedEventArgs e)
    {
        if (_host is not { } host)
            return;

        host.Loaded -= ShowFloatingPanels;

        foreach (var window in Root?.Windows ?? [])
        {
            window.X = Math.Clamp(window.X, 0, Math.Max(0, host.Bounds.Width - window.Width));
            window.Y = Math.Clamp(window.Y, 0, Math.Max(0, host.Bounds.Height - window.Height));
            window.Present(window.IsModal);
        }
    }

    private void OnPanelsChanged(object? sender, EventArgs e) => PanelsChanged?.Invoke(this, EventArgs.Empty);

    // Dock only sets this flag for a docked panel's drag. Moving a floating panel needs it too: the
    // layer's see-through areas only catch hits while it's set (DockStyles.axaml), and without those
    // hits Dock finds nothing to offer over the canvas.
    private void OnFloatingPanelDragStarted(object? sender, EventArgs e) => SetDragging(true);

    private void OnFloatingPanelDragEnded(object? sender, EventArgs e) => SetDragging(false);

    private void SetDragging(bool isDragging)
    {
        if (_host is { } host)
            host.IsDraggingDock = isDragging;
    }

    private IRootDock? LoadSavedLayout(ShellDockFactory factory)
    {
        if (settings.GetText(LayoutSettingKey) is not { } saved)
            return null;

        try
        {
            if (_serializer.Deserialize<IRootDock>(saved) is { } layout && ShellDockFactory.IsUsable(layout))
            {
                ShellDockFactory.RefreshPanelTitles(layout);
                return layout;
            }

            LogLayoutUnusable(logger);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException
                                      or ArgumentException)
        {
            // Most likely from another version of Dock. Losing the layout costs little, so there's
            // no need to bother the user about it.
            LogLayoutUnreadable(logger, e);
        }

        return null;
    }

    private void Save()
    {
        if (Root is not { } root)
            return;

        // A floating panel's position lives in its host until it's asked for.
        foreach (var window in root.Windows ?? [])
            window.Save();

        try
        {
            settings.SetText(LayoutSettingKey, _serializer.Serialize(root));
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException)
        {
            LogLayoutNotSaved(logger, e);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The saved panel layout can't be used here. Using the default layout.")]
    private static partial void LogLayoutUnusable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The saved panel layout couldn't be read. Using the default layout.")]
    private static partial void LogLayoutUnreadable(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The panel layout couldn't be saved.")]
    private static partial void LogLayoutNotSaved(ILogger logger, Exception exception);
}
