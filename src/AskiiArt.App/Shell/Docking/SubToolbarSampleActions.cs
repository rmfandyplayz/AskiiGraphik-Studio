// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// Debug builds only: puts a sample sub-toolbar on a toolbar, to test them by hand until tool groups arrive in M3. Its
/// buttons do nothing. Placeholder: tool groups replace it.
/// </summary>
internal sealed class SubToolbarSampleActions(ShellDocking docking, bool includeDebugActions) : IActionProvider
{
    private const int ShortSampleButtons = 3;
    private const int LongSampleButtons = 16;

    private ActionCheckedState? _isLongState;
    private bool _isLong;

    public IEnumerable<AppAction> CreateActions()
    {
        if (!includeDebugActions)
            yield break;

        yield return CreateParentToggle(SubToolbarSampleActionIds.OnFile, nameof(Strings.ActionSubToolbarOnFile), ShellDockingIds.FileToolbar);
        yield return CreateParentToggle(SubToolbarSampleActionIds.OnTools, nameof(Strings.ActionSubToolbarOnTools), ShellDockingIds.ToolsToolbar);
        yield return CreateParentToggle(SubToolbarSampleActionIds.OnEdit, nameof(Strings.ActionSubToolbarOnEdit), ShellDockingIds.EditToolbar);
        yield return CreateParentToggle(SubToolbarSampleActionIds.OnView, nameof(Strings.ActionSubToolbarOnView), ShellDockingIds.ViewToolbar);

        _isLongState = new ActionCheckedState(() => _isLong);
        yield return new AppAction(SubToolbarSampleActionIds.Long, nameof(Strings.ActionSubToolbarLong),
            new RelayCommand(ToggleLong), checkedState: _isLongState);
    }

    private AppAction CreateParentToggle(string actionId, string labelKey, string toolbarId)
    {
        var state = new ActionCheckedState(() => docking.SubToolbarParent == toolbarId);
        docking.SubToolbarChanged += (_, _) => state.NotifyChanged();

        return new AppAction(actionId, labelKey, new RelayCommand(() => Toggle(toolbarId)), checkedState: state);
    }

    private void Toggle(string toolbarId)
    {
        if (docking.SubToolbarParent == toolbarId)
            docking.HideSubToolbar();
        else
            docking.ShowSubToolbar(toolbarId, CreateView);
    }

    private void ToggleLong()
    {
        _isLong = !_isLong;
        _isLongState?.NotifyChanged();

        if (docking.SubToolbarParent is { } toolbarId)
            docking.ShowSubToolbar(toolbarId, CreateView);
    }

    private Control CreateView()
    {
        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        bar.Bind(StackPanel.SpacingProperty, bar.GetResourceObservable("AppSpacingSmall"));
        bar.Bind(Layoutable.MarginProperty, bar.GetResourceObservable("AppToolbarPadding"));

        var buttons = _isLong ? LongSampleButtons : ShortSampleButtons;
        for (var number = 1; number <= buttons; number++)
        {
            bar.Children.Add(new Button
            {
                Content = string.Format(CultureInfo.CurrentCulture, Strings.SubToolbarSampleButtonFormat, number),
                Focusable = false,
            });
        }

        return bar;
    }
}
