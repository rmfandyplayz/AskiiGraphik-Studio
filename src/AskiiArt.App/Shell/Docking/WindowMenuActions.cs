// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>The Window menu's actions: a check-marked toggle for each window and toolbar, and Reset Layout.</summary>
internal sealed class WindowMenuActions(ShellDocking docking) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return CreateWindowToggle(WindowMenuActionIds.ToggleLayers, nameof(Strings.ActionToggleLayers), ShellDockingIds.LayersPanel);
        yield return CreateWindowToggle(WindowMenuActionIds.ToggleHistory, nameof(Strings.ActionToggleHistory), ShellDockingIds.HistoryPanel);
        yield return CreateToolbarToggle(WindowMenuActionIds.ToggleFileToolbar, nameof(Strings.ActionToggleFileToolbar), ShellDockingIds.FileToolbar);
        yield return CreateToolbarToggle(WindowMenuActionIds.ToggleToolsToolbar, nameof(Strings.ActionToggleToolsToolbar), ShellDockingIds.ToolsToolbar);
        yield return CreateToolbarToggle(WindowMenuActionIds.ToggleEditToolbar, nameof(Strings.ActionToggleEditToolbar), ShellDockingIds.EditToolbar);
        yield return CreateToolbarToggle(WindowMenuActionIds.ToggleViewToolbar, nameof(Strings.ActionToggleViewToolbar), ShellDockingIds.ViewToolbar);
        yield return new AppAction(WindowMenuActionIds.ResetLayout, nameof(Strings.ActionResetLayout), new RelayCommand(docking.ResetLayout));
    }

    private AppAction CreateWindowToggle(string actionId, string labelKey, string windowId) =>
        CreateToggle(actionId, labelKey, () => docking.IsWindowOpen(windowId), () => docking.ToggleWindow(windowId));

    private AppAction CreateToolbarToggle(string actionId, string labelKey, string toolbarId) =>
        CreateToggle(actionId, labelKey, () => docking.IsToolbarVisible(toolbarId), () => docking.ToggleToolbar(toolbarId));

    private AppAction CreateToggle(string actionId, string labelKey, Func<bool> isChecked, Action toggle)
    {
        var state = new ActionCheckedState(isChecked);
        docking.LayoutChanged += (_, _) => state.NotifyChanged();

        return new AppAction(actionId, labelKey, new RelayCommand(toggle), checkedState: state);
    }
}
