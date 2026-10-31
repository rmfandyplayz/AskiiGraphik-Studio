// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using Avalonia.Controls;
using Avalonia.Input;

namespace AskiiArt.App.Shell.Menus;

/// <summary>
/// Turns a <see cref="MenuLayout"/> into Avalonia native menus (ADR 0007). On macOS they appear in
/// the system menu bar; on Windows and Linux a <see cref="NativeMenuBar"/> shows them in the
/// window.
/// </summary>
internal sealed class NativeMenuBuilder(ActionRegistry actions, KeyModifiers platformCommandModifiers)
{
    public NativeMenu BuildWindowMenu(IReadOnlyList<MenuDefinition> menus)
    {
        var menuBar = new NativeMenu();

        foreach (var menu in menus)
        {
            var submenu = new NativeMenu();
            AddEntries(submenu, menu.Entries);
            menuBar.Add(new NativeMenuItem(UiText.Get(menu.LabelKey)) { Menu = submenu });
        }

        return menuBar;
    }

    public NativeMenu BuildApplicationMenu(IReadOnlyList<MenuEntry> entries)
    {
        var menu = new NativeMenu();
        AddEntries(menu, entries);
        return menu;
    }

    private void AddEntries(NativeMenu menu, IReadOnlyList<MenuEntry> entries)
    {
        foreach (var entry in entries)
        {
            switch (entry)
            {
                case SeparatorMenuEntry:
                    menu.Add(new NativeMenuItemSeparator());
                    break;
                case ActionMenuEntry actionEntry:
                    menu.Add(CreateItem(actionEntry));
                    break;
                default:
                    throw new NotSupportedException($"Unknown menu entry type '{entry.GetType().Name}'.");
            }
        }
    }

    private NativeMenuItem CreateItem(ActionMenuEntry entry)
    {
        var action = actions.Get(entry.ActionId);

        var label = entry.LabelWithAppNameKey is null
            ? UiText.Get(action.LabelKey)
            : UiText.GetWithAppName(entry.LabelWithAppNameKey);

        var item = new NativeMenuItem(label) { Command = action.Command };

        // Shown next to the label. On macOS it's also the key equivalent that triggers the item;
        // on Windows and Linux the shortcut itself is a window key binding (MainWindowBuilder).
        if (action.DefaultShortcuts.Count > 0)
            item.Gesture = action.DefaultShortcuts[0].ToKeyGesture(platformCommandModifiers);

        if (action.CheckedState is { } checkedState)
        {
            item.ToggleType = MenuItemToggleType.CheckBox;
            item.IsChecked = checkedState.IsChecked;
            checkedState.Changed += (_, _) => item.IsChecked = checkedState.IsChecked;
        }

        return item;
    }
}
