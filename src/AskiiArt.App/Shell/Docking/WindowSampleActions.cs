// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Debug builds only: opens and closes empty sample windows, because stacks of up to 4 can't be tried by hand with only
/// Layers and History. They aren't part of the default layout, so a restart forgets them. Placeholder: real windows
/// replace them.
/// </summary>
internal sealed class WindowSampleActions(ShellDocking docking, bool includeDebugActions) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        if (!includeDebugActions)
            yield break;

        yield return CreateToggle(WindowSampleActionIds.First, nameof(Strings.ActionSampleWindow1), ShellDockingIds.SampleWindow1);
        yield return CreateToggle(WindowSampleActionIds.Second, nameof(Strings.ActionSampleWindow2), ShellDockingIds.SampleWindow2);
        yield return CreateToggle(WindowSampleActionIds.Third, nameof(Strings.ActionSampleWindow3), ShellDockingIds.SampleWindow3);
    }

    private AppAction CreateToggle(string actionId, string labelKey, string windowId)
    {
        var state = new ActionCheckedState(() => docking.IsWindowOpen(windowId));
        docking.LayoutChanged += (_, _) => state.NotifyChanged();

        return new AppAction(actionId, labelKey, new RelayCommand(() => docking.ToggleWindow(windowId)), checkedState: state);
    }
}
