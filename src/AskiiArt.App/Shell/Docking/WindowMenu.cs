// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A window's menu, which its title's right-click and its menu button both open (docs/DOCKING.md §3.2). It offers the
/// kinds of window this one isn't, then Close: Float and Turn into Dock for a widget, and Turn into Widget and Turn into
/// Dock for a floating window.
/// </summary>
internal static class WindowMenu
{
    public static ContextMenu Create(string windowId, ActionRegistry actions, ShellDocking docking)
    {
        var floatItem = Item(actions.Get(WindowFrameActionIds.Float), windowId);
        var turnIntoDock = Item(actions.Get(WindowFrameActionIds.TurnIntoDock), windowId);
        var turnIntoWidget = Item(actions.Get(WindowFrameActionIds.TurnIntoWidget), windowId);

        var menu = new ContextMenu
        {
            Items = { floatItem, turnIntoWidget, turnIntoDock, Item(actions.Get(WindowFrameActionIds.Close), windowId) },
        };

        menu.Opening += (_, _) =>
        {
            var isFloating = docking.IsWindowFloating(windowId);
            floatItem.IsVisible = !isFloating;
            turnIntoWidget.IsVisible = isFloating;
        };

        return menu;
    }

    private static MenuItem Item(AppAction action, string windowId) => new()
    {
        Header = UiText.WithoutAccessKey(UiText.Get(action.LabelKey)),
        Command = action.Command,
        CommandParameter = windowId,
    };
}
