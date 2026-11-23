// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Tools.RampStrokes;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>Step 4.10a's temporary Debug menu choice of how the Brush picks edge glyphs (ADR 0074).</summary>
public sealed class PatsChoiceTests
{
    private static readonly string[] _choiceIds =
    [
        CanvasActionIds.PatsUnshaped,
        CanvasActionIds.PatsShipped,
        CanvasActionIds.PatsIdea1,
        CanvasActionIds.PatsIdea2,
        CanvasActionIds.PatsIdea3,
        CanvasActionIds.PatsIdeas1And3,
        CanvasActionIds.PatsIdeas1And2And3,
        CanvasActionIds.PatsIdea3Fixed,
        CanvasActionIds.PatsIdeas1And2And3Fixed
    ];

    [AvaloniaFact]
    public void TheCanvas_StartsWithTheShippedBrush_AndItsItemTicked()
    {
        using var canvas = new TestCanvas();
        var actions = new CanvasActions(canvas.ViewModel, includeDebugActions: true).CreateActions().ToList();

        Assert.Same(PatsVariant.Shipped, canvas.ViewModel.Pats);
        Assert.Equal([CanvasActionIds.PatsShipped],
            actions.Where(action => _choiceIds.Contains(action.Id) && action.CheckedState!.IsChecked)
                .Select(action => action.Id));
    }

    [AvaloniaFact]
    public void EachItem_ChoosesItsVariant_InTheOrderTheSheetsShowThem()
    {
        using var canvas = new TestCanvas();
        var actions = new CanvasActions(canvas.ViewModel, includeDebugActions: true).CreateActions()
            .ToDictionary(action => action.Id);

        for (var index = 0; index < _choiceIds.Length; index++)
        {
            var item = actions[_choiceIds[index]];
            item.Command.Execute(null);

            Assert.Same(PatsVariant.All[index], canvas.ViewModel.Pats);
            Assert.True(item.CheckedState!.IsChecked);
            Assert.Single(_choiceIds, id => actions[id].CheckedState!.IsChecked);
        }
    }

    [AvaloniaFact]
    public void ChoosingAnItem_TellsEveryItemsCheckMark()
    {
        using var canvas = new TestCanvas();
        var actions = new CanvasActions(canvas.ViewModel, includeDebugActions: true).CreateActions()
            .ToDictionary(action => action.Id);
        var shippedChanges = 0;
        actions[CanvasActionIds.PatsShipped].CheckedState!.Changed += (_, _) => shippedChanges++;

        actions[CanvasActionIds.PatsIdea3].Command.Execute(null);

        Assert.Equal(1, shippedChanges);
    }

    [AvaloniaFact]
    public void TheChoice_IsOnlyInDebugBuilds()
    {
        using var canvas = new TestCanvas();

        Assert.DoesNotContain(new CanvasActions(canvas.ViewModel, includeDebugActions: false).CreateActions(),
            action => _choiceIds.Contains(action.Id));
    }
}
