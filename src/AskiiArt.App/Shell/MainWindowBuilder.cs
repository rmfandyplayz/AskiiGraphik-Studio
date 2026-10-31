// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Shell.Menus;
using Avalonia.Controls;
using Avalonia.Input;

namespace AskiiArt.App.Shell;

/// <summary>
/// Creates the main window with its menus and keyboard shortcuts, all taken from the action
/// registry and the menu layout, showing the canvas's document.
/// </summary>
internal sealed class MainWindowBuilder(ActionRegistry actions, MenuLayout layout, CanvasViewModel canvas)
{
    public MainWindow Build(KeyModifiers platformCommandModifiers)
    {
        var window = new MainWindow { DataContext = canvas };
        var menus = new NativeMenuBuilder(actions, platformCommandModifiers);

        NativeMenu.SetMenu(window, menus.BuildWindowMenu(layout.WindowMenus));
        AddKeyBindings(window, platformCommandModifiers);

        return window;
    }

    /// <summary>The macOS application menu, or null on platforms that don't have one.</summary>
    public NativeMenu? BuildApplicationMenu(KeyModifiers platformCommandModifiers)
    {
        if (layout.ApplicationMenu.Count == 0)
            return null;

        return new NativeMenuBuilder(actions, platformCommandModifiers).BuildApplicationMenu(layout.ApplicationMenu);
    }

    private void AddKeyBindings(Window window, KeyModifiers platformCommandModifiers)
    {
        // In the macOS system menu bar, a menu item's shortcut already triggers it. Binding it
        // again on the window could run the action twice, so only actions that aren't in a menu
        // get a window key binding there.
        var actionsTriggeredByMenu = layout.IsSystemMenuBar
            ? layout.ActionIds().ToHashSet(StringComparer.Ordinal)
            : [];

        foreach (var action in actions.All)
        {
            if (actionsTriggeredByMenu.Contains(action.Id))
                continue;

            foreach (var shortcut in action.DefaultShortcuts)
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
