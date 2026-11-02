// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>The Window menu's actions: a check-marked toggle for each movable panel, and Reset Layout.</summary>
internal sealed class ShellDockActions(ShellDock dock) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return CreatePanelToggle(ShellDockActionIds.ToggleLayers, nameof(Strings.ActionToggleLayers), ShellDockIds.LayersPanel);
        yield return CreatePanelToggle(ShellDockActionIds.ToggleHistory, nameof(Strings.ActionToggleHistory), ShellDockIds.HistoryPanel);
        yield return new AppAction(ShellDockActionIds.ResetLayout, nameof(Strings.ActionResetLayout), new RelayCommand(dock.ResetLayout));
    }

    private AppAction CreatePanelToggle(string actionId, string labelKey, string panelId)
    {
        var state = new ActionCheckedState(() => dock.IsPanelOpen(panelId));
        dock.PanelsChanged += (_, _) => state.NotifyChanged();

        return new AppAction(actionId, labelKey, new RelayCommand(() => dock.TogglePanel(panelId)), checkedState: state);
    }
}
