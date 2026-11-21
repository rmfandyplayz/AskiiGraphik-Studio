// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Canvas;

internal static class ToolActionIds
{
    public const string Hand = Prefix + HandTool.Id;
    public const string Move = Prefix + MoveTool.Id;
    public const string RectangleSelect = Prefix + RectangleSelectTool.Id;
    public const string EllipseSelect = Prefix + EllipseSelectTool.Id;
    public const string Lasso = Prefix + LassoTool.Id;
    public const string PolygonalLasso = Prefix + PolygonalLassoTool.Id;
    public const string MagicWand = Prefix + MagicWandTool.Id;
    public const string Text = Prefix + TextTool.Id;
    public const string Brush = Prefix + BrushTool.Id;
    public const string ForegroundBrush = Prefix + ColorBrushTool.ForegroundId;
    public const string BackgroundBrush = Prefix + ColorBrushTool.BackgroundId;
    public const string CharacterBrush = Prefix + CharacterBrushTool.Id;
    public const string Eraser = Prefix + EraserTool.Id;
    public const string SoftEraser = Prefix + SoftEraserTool.Id;
    public const string BackgroundEraser = Prefix + EraserTool.BackgroundId;

    private const string Prefix = "tool.";

    public static string For(string toolId) => Prefix + toolId;
}

internal static class ToolGroupActionIds
{
    private const string Prefix = "toolGroup.";

    public static string Hand { get; } = For(BuiltInToolGroups.Hand.Id);
    public static string Move { get; } = For(BuiltInToolGroups.Move.Id);
    public static string Select { get; } = For(BuiltInToolGroups.Select.Id);
    public static string Text { get; } = For(BuiltInToolGroups.Text.Id);
    public static string Brush { get; } = For(BuiltInToolGroups.Brush.Id);
    public static string Eraser { get; } = For(BuiltInToolGroups.Eraser.Id);

    public static string For(string groupId) => Prefix + groupId;
}

/// <summary>
/// One action per registered tool, which makes it the active one, and one per tool group, which makes the group's
/// last-used tool the active one (ADR 0049). They come from the registry, so a tool registered from elsewhere gets
/// its actions the same way. The letter and number keys aren't action shortcuts: those would fire while typing in
/// any text box, so the canvas handles them (ADR 0033).
/// </summary>
internal sealed class ToolActions(CanvasViewModel canvas) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        foreach (var definition in canvas.Tools)
        {
            var toolId = definition.Descriptor.Id;
            yield return Create(ToolActionIds.For(toolId), definition.Descriptor.NameKey,
                () => canvas.ActiveTool.Descriptor.Id == toolId, () => canvas.SelectTool(toolId));
        }

        foreach (var group in canvas.ToolGroups)
        {
            var groupId = group.Id;
            yield return Create(ToolGroupActionIds.For(groupId), group.NameKey,
                () => canvas.SelectedGroup.Id == groupId, () => canvas.SelectToolGroup(groupId));
        }
    }

    private AppAction Create(string actionId, string labelKey, Func<bool> isChecked, Action select)
    {
        var state = new ActionCheckedState(isChecked);
        canvas.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CanvasViewModel.ActiveTool))
                state.NotifyChanged();
        };

        // Clicking the active tool's toolbar button changes nothing, but the button has already unticked itself, so
        // it needs telling.
        var command = new RelayCommand(() =>
        {
            select();
            state.NotifyChanged();
        });

        return new AppAction(actionId, labelKey, command, checkedState: state);
    }
}
