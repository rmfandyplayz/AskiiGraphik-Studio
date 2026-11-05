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
/// Builds placeholder button bars: text buttons, one per action, bound to the action's command. A
/// toggle action, such as Show Grid, gets a toggle button that follows its state.
/// </summary>
internal sealed class ToolbarBuilder(ActionRegistry actions)
{
    /// <summary>The toolbar's control name, so tests can find it among the other widgets.</summary>
    public const string ControlName = "Toolbar";

    /// <summary>A single row, for the toolbar. Its frame turns it if the toolbar is vertical.</summary>
    public Control Build(ToolbarLayout layout)
    {
        var bar = new StackPanel { Name = ControlName, Orientation = Orientation.Horizontal };
        bar.Bind(StackPanel.SpacingProperty, bar.GetResourceObservable("AppSpacingSmall"));
        bar.Bind(Layoutable.MarginProperty, bar.GetResourceObservable("AppToolbarPadding"));
        AddEntries(bar, layout);
        return bar;
    }

    /// <summary>As many rows as it needs, for a bar inside a narrow panel.</summary>
    public Control BuildWrapping(ToolbarLayout layout)
    {
        var bar = new WrapPanel();
        bar.Bind(WrapPanel.ItemSpacingProperty, bar.GetResourceObservable("AppSpacingSmall"));
        bar.Bind(WrapPanel.LineSpacingProperty, bar.GetResourceObservable("AppSpacingSmall"));
        bar.Bind(Layoutable.MarginProperty, bar.GetResourceObservable("AppToolbarPadding"));
        AddEntries(bar, layout);
        return bar;
    }

    private void AddEntries(Panel bar, ToolbarLayout layout)
    {
        foreach (var entry in layout.Entries)
        {
            switch (entry)
            {
                case ActionMenuEntry item:
                    bar.Children.Add(BuildButton(actions.Get(item.ActionId)));
                    break;
                case SeparatorMenuEntry:
                    // Square, because the toolbar can turn, and only its length along the bar makes a gap.
                    var gap = new Control();
                    gap.Bind(Layoutable.WidthProperty, gap.GetResourceObservable("AppToolbarGroupGap"));
                    gap.Bind(Layoutable.HeightProperty, gap.GetResourceObservable("AppToolbarGroupGap"));
                    bar.Children.Add(gap);
                    break;
            }
        }
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

        // Only while it's showing, because a toolbar's view can be made many times: each time it's shown again, and
        // for each preview during a drag.
        void Follow(object? sender, EventArgs e) => toggle.IsChecked = state.IsChecked;
        toggle.AttachedToVisualTree += (_, _) =>
        {
            state.Changed += Follow;
            Follow(null, EventArgs.Empty);
        };
        toggle.DetachedFromVisualTree += (_, _) => state.Changed -= Follow;

        return toggle;
    }
}
