// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Layers;
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
/// Creates the main window with its menus, toolbars and keyboard shortcuts, all taken from the
/// action registry and the menu and toolbar layouts, showing the canvas's document. The window's
/// title follows the document, and closing it asks about unsaved changes first. On macOS a new
/// window is built each time one opens (ADR 0023).
/// </summary>
internal sealed class MainWindowBuilder(
    ActionRegistry actions,
    MenuLayout layout,
    ShellToolbars toolbars,
    CanvasViewModel canvas,
    WindowTitle title,
    DocumentFiles files,
    IAppShell shell,
    ShellDocking docking,
    IAppSettings settings,
    HistoryPanelViewModel history,
    LayersPanelViewModel layers)
{
    public MainWindow Build(KeyModifiers platformCommandModifiers)
    {
        var window = new MainWindow { DataContext = canvas, Title = title.Text };
        var menus = new NativeMenuBuilder(actions, platformCommandModifiers);

        FollowTitle(window);
        NativeMenu.SetMenu(window, menus.BuildWindowMenu(layout.WindowMenus));
        window.SetMenuBar(layout.IsSystemMenuBar ? null : new NativeMenuBar());
        ShowPanels(window);
        AddKeyBindings(window, platformCommandModifiers);
        AskAboutUnsavedChangesBeforeClosing(window);
        SayWhenTheSettingsWereDamaged(window);

        return window;
    }

    private void ShowPanels(MainWindow window)
    {
        var metrics = ShellLayoutMetrics.FromResources(Application.Current!, window.ActualThemeVariant);
        var bars = new ToolbarBuilder(actions);

        ShellWindowEntry Window(string id, string title, Func<Control> createView) =>
            new(id, title, metrics.WindowMinHeight, createView, () => WindowMenu.Create(id, actions, docking),
                actions.Get(WindowFrameActionIds.Close).Command);

        // Each view is built when its window or toolbar shows, and again if it's reopened.
        var catalog = new ShellCatalog(
        [
            Window(ShellDockingIds.LayersPanel, Strings.PanelLayers,
                () => new LayersPanelView
                {
                    DataContext = layers,
                    Buttons = bars.BuildWrapping(ToolbarLayout.CreateLayersPanel()),
                }),
            Window(ShellDockingIds.HistoryPanel, Strings.PanelHistory,
                () => new HistoryPanelView { DataContext = history }),
            .. SampleWindows(Window),
        ],
        [
            .. toolbars.Layouts.Select(toolbar => new ShellToolbarEntry(toolbar.Key,
                () => bars.Build(toolbar.Value),
                () => ToolbarMenu.Create(toolbar.Key, actions, docking))),
        ]);

        docking.Attach(window.Panels, catalog, metrics);
        window.Closed += (_, _) => docking.SaveAndDetach();
    }

    // They exist exactly when the actions that open them do, which is in debug builds.
    private IEnumerable<ShellWindowEntry> SampleWindows(Func<string, string, Func<Control>, ShellWindowEntry> window)
    {
        if (actions.All.All(action => action.Id != WindowSampleActionIds.First))
            yield break;

        yield return window(ShellDockingIds.SampleWindow1, Strings.ActionSampleWindow1, () => new Border());
        yield return window(ShellDockingIds.SampleWindow2, Strings.ActionSampleWindow2, () => new Border());
        yield return window(ShellDockingIds.SampleWindow3, Strings.ActionSampleWindow3, () => new Border());
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
