// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Settings;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Shell;

/// <summary>
/// Creates the main window with its menus, toolbar and keyboard shortcuts, all taken from the
/// action registry and the menu and toolbar layouts, showing the canvas's document. The window's
/// title follows the document, and closing it asks about unsaved changes first. On macOS a new
/// window is built each time one opens (ADR 0023).
/// </summary>
internal sealed class MainWindowBuilder(
    ActionRegistry actions,
    MenuLayout layout,
    ToolbarLayout toolbar,
    CanvasViewModel canvas,
    WindowTitle title,
    DocumentFiles files,
    IAppShell shell,
    ShellDock panels,
    IAppSettings settings)
{
    public MainWindow Build(KeyModifiers platformCommandModifiers)
    {
        var window = new MainWindow { DataContext = canvas, Title = title.Text };
        var menus = new NativeMenuBuilder(actions, platformCommandModifiers);

        FollowTitle(window);
        NativeMenu.SetMenu(window, menus.BuildWindowMenu(layout.WindowMenus));
        ShowPanels(window);
        AddKeyBindings(window, platformCommandModifiers);
        AskAboutUnsavedChangesBeforeClosing(window);
        SayWhenTheSettingsWereDamaged(window);

        return window;
    }

    private void ShowPanels(MainWindow window)
    {
        var views = new Dictionary<string, Func<Control>>(StringComparer.Ordinal)
        {
            [ShellDockIds.ToolbarPanel] = () => new ToolbarBuilder(actions).Build(toolbar),
            [ShellDockIds.StatusBarPanel] = () => new StatusBarView { DataContext = canvas },
            [ShellDockIds.HistoryPanel] = () => new PanelPlaceholderView(),
            [ShellDockIds.LayersPanel] = () => new PanelPlaceholderView(),
        };

        // On macOS the menus are in the system menu bar. This shows the window's native menu.
        if (!layout.IsSystemMenuBar)
            views[ShellDockIds.MenuBarPanel] = () => new NativeMenuBar();

        var metrics = ShellLayoutMetrics.FromResources(Application.Current!, window.ActualThemeVariant);
        panels.Attach(window.Panels, new ShellDockFactory(metrics, hasMenuBarPanel: !layout.IsSystemMenuBar),
            new ShellPanelTemplates(views));
        window.Closed += (_, _) => panels.SaveAndDetach();
    }

    // After the window has opened, so the message has a window to belong to.
    private void SayWhenTheSettingsWereDamaged(Window window)
    {
        window.Opened += (_, _) =>
        {
            if (settings.TakeDamageNotice())
            {
                Dispatcher.UIThread.Post(() =>
                    _ = shell.ShowMessageAsync(Strings.SettingsDamagedTitle, Strings.SettingsDamagedMessage));
            }
        };
    }

    /// <summary>The macOS application menu, or null on platforms that don't have one.</summary>
    public NativeMenu? BuildApplicationMenu(KeyModifiers platformCommandModifiers)
    {
        if (layout.ApplicationMenu.Count == 0)
            return null;

        return new NativeMenuBuilder(actions, platformCommandModifiers).BuildApplicationMenu(layout.ApplicationMenu);
    }

    // The title, and on macOS the close button's dot, until the window closes: later windows follow
    // the same title.
    private void FollowTitle(Window window)
    {
        window.Opened += (_, _) => MacOSDocumentEdited.Set(window, title.HasUnsavedChanges);
        title.PropertyChanged += OnTitleChanged;
        window.Closed += (_, _) => title.PropertyChanged -= OnTitleChanged;

        void OnTitleChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WindowTitle.Text))
                window.Title = title.Text;
            else if (e.PropertyName == nameof(WindowTitle.HasUnsavedChanges))
                MacOSDocumentEdited.Set(window, title.HasUnsavedChanges);
        }
    }

    // Covers the close button, Close Window, Quit and the app shutting down, which all close the window.
    private void AskAboutUnsavedChangesBeforeClosing(Window window)
    {
        var isAsking = false;
        var mayClose = false;
        window.Closing += async (_, e) =>
        {
            if (mayClose || !files.HasUnsavedChanges)
                return;

            // Closing can't wait for an answer, so it's cancelled, and repeated if the user agrees.
            e.Cancel = true;
            if (isAsking)
                return;

            var isQuitting = e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown;
            isAsking = true;
            try
            {
                mayClose = await files.ConfirmDiscardingChangesAsync();
            }
            finally
            {
                isAsking = false;
            }

            if (!mayClose)
                return;

            // After this Closing event has finished, rather than from inside it. Cancelling the close
            // also cancelled quitting, so that's asked for again: on macOS, closing the window alone
            // would leave the app running.
            if (isQuitting)
                Dispatcher.UIThread.Post(shell.RequestShutdown);
            else
                Dispatcher.UIThread.Post(window.Close);
        };
    }

    private void AddKeyBindings(Window window, KeyModifiers platformCommandModifiers)
    {
        // In the macOS system menu bar, a menu item's shortcut (an action's first one) already
        // triggers it. Binding it again on the window could run the action twice, so there only
        // the other shortcuts get a window key binding.
        var actionsTriggeredByMenu = layout.IsSystemMenuBar
            ? layout.ActionIds().ToHashSet(StringComparer.Ordinal)
            : [];

        foreach (var action in actions.All)
        {
            var firstBoundShortcut = actionsTriggeredByMenu.Contains(action.Id) ? 1 : 0;
            foreach (var shortcut in action.DefaultShortcuts.Skip(firstBoundShortcut))
            {
                window.KeyBindings.Add(new KeyBinding
                {
                    Gesture = shortcut.ToKeyGesture(platformCommandModifiers),
                    Command = action.Command,
                });
            }
        }
    }
}
