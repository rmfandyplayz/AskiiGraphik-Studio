// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Tools.RampStrokes;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>The Debug menu's choice of how strict the Brush's edges are (ADR 0075).</summary>
public sealed class BrushEdgesChoiceTests
{
    [AvaloniaFact]
    public void TheCanvas_StartsWithStandardEdges_AndItsItemTicked()
    {
        using var canvas = new TestCanvas();
        var actions = BrushEdgesActions(canvas, includeDebugActions: true);

        Assert.Equal(EdgeStrictness.Standard, canvas.ViewModel.EdgeStrictness);
        Assert.True(actions[CanvasActionIds.BrushEdgesStandard].CheckedState!.IsChecked);
        Assert.False(actions[CanvasActionIds.BrushEdgesStrict].CheckedState!.IsChecked);
    }

    [AvaloniaFact]
    public void ChoosingStrict_SetsIt_AndMovesTheTick()
    {
        using var canvas = new TestCanvas();
        var actions = BrushEdgesActions(canvas, includeDebugActions: true);
        var standardChanges = 0;
        actions[CanvasActionIds.BrushEdgesStandard].CheckedState!.Changed += (_, _) => standardChanges++;

        actions[CanvasActionIds.BrushEdgesStrict].Command.Execute(null);

        Assert.Equal(EdgeStrictness.Strict, canvas.ViewModel.EdgeStrictness);
        Assert.True(actions[CanvasActionIds.BrushEdgesStrict].CheckedState!.IsChecked);
        Assert.False(actions[CanvasActionIds.BrushEdgesStandard].CheckedState!.IsChecked);
        Assert.Equal(1, standardChanges);
    }

    [AvaloniaFact]
    public void ChoosingTheTickedItem_KeepsItTicked()
    {
        using var canvas = new TestCanvas();
        var actions = BrushEdgesActions(canvas, includeDebugActions: true);
        var standard = actions[CanvasActionIds.BrushEdgesStandard];
        var changes = 0;
        standard.CheckedState!.Changed += (_, _) => changes++;

        standard.Command.Execute(null);

        Assert.Equal(EdgeStrictness.Standard, canvas.ViewModel.EdgeStrictness);
        Assert.True(standard.CheckedState.IsChecked);
        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void TheChoice_IsOnlyInDebugBuilds()
    {
        using var canvas = new TestCanvas();

        Assert.Empty(BrushEdgesActions(canvas, includeDebugActions: false));
    }

    private static Dictionary<string, AppAction> BrushEdgesActions(TestCanvas canvas, bool includeDebugActions) =>
        new CanvasActions(canvas.ViewModel, includeDebugActions).CreateActions()
            .Where(action => action.Id is CanvasActionIds.BrushEdgesStandard or CanvasActionIds.BrushEdgesStrict)
            .ToDictionary(action => action.Id);
}
