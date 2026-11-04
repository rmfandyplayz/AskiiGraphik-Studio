// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.Docking.Layouts;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// What a toolbar's right-click menu does. One action serves every toolbar, with the toolbar's id as its command
/// parameter, because the toolbars are registered by id and more can be added later.
/// </summary>
internal sealed class ToolbarMenuActions(ShellDocking docking) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return CreateDirection(ToolbarMenuActionIds.Horizontal, nameof(Strings.ActionToolbarHorizontal), ToolbarOrientation.Horizontal);
        yield return CreateDirection(ToolbarMenuActionIds.Vertical, nameof(Strings.ActionToolbarVertical), ToolbarOrientation.Vertical);
        yield return new AppAction(ToolbarMenuActionIds.Hide, nameof(Strings.ActionToolbarHide), new RelayCommand<string>(Hide));
    }

    private AppAction CreateDirection(string actionId, string labelKey, ToolbarOrientation orientation)
    {
        var command = new RelayCommand<string>(
            toolbarId =>
            {
                if (toolbarId is not null)
                    docking.SetToolbarOrientation(toolbarId, orientation);
            },
            toolbarId => toolbarId is not null && docking.CanChooseToolbarOrientation(toolbarId));

        docking.LayoutChanged += (_, _) => command.NotifyCanExecuteChanged();
        return new AppAction(actionId, labelKey, command);
    }

    private void Hide(string? toolbarId)
    {
        if (toolbarId is not null)
            docking.HideToolbar(toolbarId);
    }
}
