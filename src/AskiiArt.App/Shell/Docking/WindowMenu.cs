// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.Docking.Layouts;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A window's menu, which its title's right-click and its menu button both open, as does right-clicking a dock window's
/// tab (docs/DOCKING.md §3.2). It offers the kinds of window this one isn't, then Close: Float and Turn into Dock for a
/// widget, Turn into Widget and Turn into Dock for a floating window, and Float and Turn into Widget for a dock window.
/// An item that can't work where the window is now is greyed out.
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
            var kind = docking.WindowKindOf(windowId);
            floatItem.IsVisible = kind != WindowKind.Floating;
            turnIntoWidget.IsVisible = kind != WindowKind.Widget;
            turnIntoDock.IsVisible = kind != WindowKind.Dock;

            // Whether there's room depends on the app window's size as well as the layout, so it's asked afresh.
            Recheck(turnIntoWidget);
            Recheck(turnIntoDock);
        };

        return menu;
    }

    private static void Recheck(MenuItem item)
    {
        if (item.Command is IRelayCommand command)
            command.NotifyCanExecuteChanged();
    }

    private static MenuItem Item(AppAction action, string windowId) => new()
    {
        Header = UiText.WithoutAccessKey(UiText.Get(action.LabelKey)),
        Command = action.Command,
        CommandParameter = windowId,
    };
}
