// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Text;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Canvas;

internal static class ToolActionIds
{
    public const string Text = Prefix + TextTool.Id;
    public const string Eraser = Prefix + EraserTool.Id;

    private const string Prefix = "tool.";

    public static string For(string toolId) => Prefix + toolId;
}

/// <summary>
/// One action per registered tool, which makes it the active one. They come from the registry, so a
/// tool registered from elsewhere gets its action the same way. Their letter keys aren't action
/// shortcuts: those would fire while typing in any text box, so the canvas handles them (ADR 0033).
/// </summary>
internal sealed class ToolActions(CanvasViewModel canvas) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        foreach (var definition in canvas.Tools)
        {
            var toolId = definition.Descriptor.Id;
            var state = new ActionCheckedState(() => canvas.ActiveTool.Descriptor.Id == toolId);
            canvas.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(CanvasViewModel.ActiveTool))
                    state.NotifyChanged();
            };

            // Clicking the active tool's toolbar button changes nothing, but the button has already
            // unticked itself, so it needs telling.
            var select = new RelayCommand(() =>
            {
                canvas.SelectTool(toolId);
                state.NotifyChanged();
            });

            yield return new AppAction(ToolActionIds.For(toolId), definition.Descriptor.NameKey, select, checkedState: state);
        }
    }
}
