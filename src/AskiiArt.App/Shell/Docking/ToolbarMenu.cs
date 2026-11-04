// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.Docking.Layouts;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A toolbar's right-click menu: Horizontal and Vertical (greyed out on an edge middle), and Hide.</summary>
internal static class ToolbarMenu
{
    public static ContextMenu Create(string toolbarId, ActionRegistry actions, ShellDocking docking)
    {
        var horizontal = Item(actions.Get(ToolbarMenuActionIds.Horizontal), toolbarId);
        var vertical = Item(actions.Get(ToolbarMenuActionIds.Vertical), toolbarId);
        horizontal.ToggleType = MenuItemToggleType.Radio;
        vertical.ToggleType = MenuItemToggleType.Radio;

        var menu = new ContextMenu
        {
            Items = { horizontal, vertical, new Separator(), Item(actions.Get(ToolbarMenuActionIds.Hide), toolbarId) },
        };

        menu.Opening += (_, _) =>
        {
            var isHorizontal = docking.ToolbarOrientationOf(toolbarId) == ToolbarOrientation.Horizontal;
            horizontal.IsChecked = isHorizontal;
            vertical.IsChecked = !isHorizontal;
        };

        return menu;
    }

    private static MenuItem Item(AppAction action, string toolbarId) => new()
    {
        Header = UiText.WithoutAccessKey(UiText.Get(action.LabelKey)),
        Command = action.Command,
        CommandParameter = toolbarId,
    };
}
