// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.Core.Tools;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Painting;

internal static class BrushActionIds
{
    public const string ForegroundBrush = "brush.foreground";
    public const string BackgroundBrush = "brush.background";
}

/// <summary>
/// The FG Brush and BG Brush buttons of the Brush's sub-toolbar (ADR 0043). They choose the same colour as the
/// Colors toolbar does. Keys 1 and 2 aren't their shortcuts: those would fire while typing in any text box,
/// so the Brush takes them from the canvas itself.
/// </summary>
internal sealed class BrushActions(PaintColors colors) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        yield return Create(BrushActionIds.ForegroundBrush, nameof(Strings.ActionForegroundBrush),
            PaintColorTarget.Foreground);
        yield return Create(BrushActionIds.BackgroundBrush, nameof(Strings.ActionBackgroundBrush),
            PaintColorTarget.Background);
    }

    private AppAction Create(string actionId, string labelKey, PaintColorTarget target)
    {
        var state = new ActionCheckedState(() => colors.Target == target);
        colors.TargetChanged += (_, _) => state.NotifyChanged();

        // Clicking the chosen brush's button changes nothing, but the button has already unticked itself, so it
        // needs telling.
        var choose = new RelayCommand(() =>
        {
            colors.Target = target;
            state.NotifyChanged();
        });

        return new AppAction(actionId, labelKey, choose, checkedState: state);
    }
}
