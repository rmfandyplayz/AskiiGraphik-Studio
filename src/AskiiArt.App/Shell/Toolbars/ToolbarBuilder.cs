// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Menus;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AskiiArt.App.Shell.Toolbars;

/// <summary>
/// Builds the placeholder toolbar: a row of text buttons, one per action, bound to the action's
/// command. A toggle action, such as Show Grid, gets a toggle button that follows its state.
/// </summary>
internal sealed class ToolbarBuilder(ActionRegistry actions)
{
    /// <summary>The toolbar's control name, so tests can find it among the other widgets.</summary>
    public const string ControlName = "Toolbar";

    public Control Build(ToolbarLayout layout)
    {
        var bar = new StackPanel { Name = ControlName, Orientation = Orientation.Horizontal };
        bar.Bind(StackPanel.SpacingProperty, bar.GetResourceObservable("AppSpacingSmall"));
        bar.Bind(Layoutable.MarginProperty, bar.GetResourceObservable("AppToolbarPadding"));

        foreach (var entry in layout.Entries)
        {
            switch (entry)
            {
                case ActionMenuEntry item:
                    bar.Children.Add(BuildButton(actions.Get(item.ActionId)));
                    break;
                case SeparatorMenuEntry:
                    var gap = new Control();
                    gap.Bind(Layoutable.WidthProperty, gap.GetResourceObservable("AppToolbarGroupGap"));
                    bar.Children.Add(gap);
                    break;
            }
        }

        return bar;
    }

    // Toolbar buttons don't take keyboard focus, so typing keeps going to the canvas after a click.
    private static Button BuildButton(AppAction action)
    {
        var label = UiText.WithoutAccessKey(UiText.Get(action.LabelKey));
        if (action.CheckedState is not { } state)
            return new Button { Content = label, Command = action.Command, Focusable = false };

        var toggle = new ToggleButton
        {
            Content = label,
            Command = action.Command,
            Focusable = false,
            IsChecked = state.IsChecked
        };
        state.Changed += (_, _) => toggle.IsChecked = state.IsChecked;
        return toggle;
    }
}
