// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Selection;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Tools;

public sealed class SelectionPreviewTests
{
    private static readonly SelectionMask _before = TestMasks.Parse("##···", "##···", "·····");
    private static readonly SelectionMask _shape = TestMasks.Parse("·····", "·###·", "·###·");

    [Theory]
    [InlineData(SelectionCombineMode.Replace)]
    [InlineData(SelectionCombineMode.Add)]
    [InlineData(SelectionCombineMode.Subtract)]
    [InlineData(SelectionCombineMode.Intersect)]
    public void TheResult_IsTheShapeCombinedWithTheSelectionBefore(SelectionCombineMode mode)
    {
        var preview = new SelectionPreview(_before, mode, _shape);

        Assert.Equal(_before.CombinedWith(_shape, mode), preview.Result);
    }

    [Fact]
    public void AnEmptyShape_LeavesNothing_WhenReplacing()
    {
        var preview = new SelectionPreview(_before, SelectionCombineMode.Replace, Empty());

        Assert.True(preview.Result.IsEmpty);
        Assert.Equal(_before, preview.Lost);
    }

    [Theory]
    [InlineData(SelectionCombineMode.Add)]
    [InlineData(SelectionCombineMode.Subtract)]
    [InlineData(SelectionCombineMode.Intersect)]
    public void AnEmptyShape_LeavesTheSelectionAlone_OtherwiseAsAClickDoes(SelectionCombineMode mode)
    {
        var preview = new SelectionPreview(_before, mode, Empty());

        Assert.Equal(_before, preview.Result);
        Assert.True(preview.Lost.IsEmpty);
    }

    [Fact]
    public void Lost_IsWhatWasSelectedAndWouldNotBe()
    {
        var preview = new SelectionPreview(_before, SelectionCombineMode.Intersect, _shape);

        Assert.Equal(["##···", "#····", "·····"], TestMasks.Describe(preview.Lost));
    }

    [Fact]
    public void TheShape_IsMadeOnlyWhenAskedFor_AndOnlyOnce()
    {
        var made = 0;
        var preview = new SelectionPreview(_before, SelectionCombineMode.Add, () =>
        {
            made++;
            return _shape;
        });

        Assert.Equal(0, made);

        _ = preview.Result;
        _ = preview.Shape;
        _ = preview.Lost;

        Assert.Equal(1, made);
    }

    private static SelectionMask Empty() => SelectionMask.Empty(_before.CanvasSize);
}
