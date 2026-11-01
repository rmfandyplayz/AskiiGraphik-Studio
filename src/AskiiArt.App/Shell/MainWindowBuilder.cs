// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Shell;

/// <summary>
/// Creates the main window with its menus, toolbar and keyboard shortcuts, all taken from the
/// action registry and the menu and toolbar layouts, showing the canvas's document. The window's
/// title follows the document, and closing it asks about unsaved changes first.
/// </summary>
internal sealed class MainWindowBuilder(ActionRegistry actions, MenuLayout layout, ToolbarLayout toolbar,
    CanvasViewModel canvas, WindowTitle title, DocumentFiles files)
{
    public MainWindow Build(KeyModifiers platformCommandModifiers)
    {
        var window = new MainWindow { DataContext = canvas, Title = title.Text };
        var menus = new NativeMenuBuilder(actions, platformCommandModifiers);

        title.PropertyChanged += (_, _) => window.Title = title.Text;
        NativeMenu.SetMenu(window, menus.BuildWindowMenu(layout.WindowMenus));
        window.SetToolbar(new ToolbarBuilder(actions).Build(toolbar));
        AddKeyBindings(window, platformCommandModifiers);
        AskAboutUnsavedChangesBeforeClosing(window);

        return window;
    }

    /// <summary>The macOS application menu, or null on platforms that don't have one.</summary>
    public NativeMenu? BuildApplicationMenu(KeyModifiers platformCommandModifiers)
    {
        if (layout.ApplicationMenu.Count == 0)
            return null;

        return new NativeMenuBuilder(actions, platformCommandModifiers).BuildApplicationMenu(layout.ApplicationMenu);
    }

    // Covers the close button, Quit and the app shutting down, which all close the window.
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

            isAsking = true;
            try
            {
                mayClose = await files.ConfirmDiscardingChangesAsync();
            }
            finally
            {
                isAsking = false;
            }

            // After this Closing event has finished, rather than from inside it.
            if (mayClose)
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
