// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class SelectionDisplayTests
{
    private static readonly CanvasSize _size = new(4, 4);
    private static readonly SelectionMask _before = Rectangle(0, 0, 2, 2);
    private static readonly SelectionMask _shape = Rectangle(1, 1, 2, 2);

    [Fact]
    public void WithNothingSelected_AndNoGesture_ThereIsNothingToShow()
    {
        Assert.Null(SelectionDisplay.For(null, null));
        Assert.Null(SelectionDisplay.For(SelectionMask.Empty(_size), null));
    }

    [Fact]
    public void WithNoGesture_TheSelectionHasARunningOutline_AndMovingHatchingAroundIt()
    {
        var display = SelectionDisplay.For(_before, null)!;

        Assert.Equal(_before, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Bright(_before)], display.Outlines);
        Assert.True(display.HasRunningOutline);
        Assert.True(display.HatchesUnselected);
        Assert.True(display.UnselectedHatchMoves);
    }

    [Fact]
    public void Replacing_ShowsTheShape_OverTheOldOutlineVanishing()
    {
        var display = Preview(SelectionCombineMode.Replace);

        Assert.Equal(_shape, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Vanishing(_before), Bright(_shape)], display.Outlines);
        Assert.True(display.UnselectedHatchMoves);
    }

    [Fact]
    public void Adding_ShowsTheResultsOutline_OverTheShapesFaintOne_AndTheOldOneVanishing()
    {
        var display = Preview(SelectionCombineMode.Add);

        var result = _before.CombinedWith(_shape, SelectionCombineMode.Add);
        Assert.Equal(result, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Vanishing(_before), Faint(_shape), Bright(result)], display.Outlines);
        Assert.True(display.UnselectedHatchMoves);
    }

    [Fact]
    public void Subtracting_RunsTheShapeInSubtractsColours_OverTheOldOutline_FaintWhereItStays_VanishingWhereNot()
    {
        var display = Preview(SelectionCombineMode.Subtract);

        var result = _before.CombinedWith(_shape, SelectionCombineMode.Subtract);
        var lost = Rectangle(1, 1, 1, 1);
        Assert.Equal(result, display.Selected);
        Assert.Equal(lost, display.Lost);
        Assert.Equal(SelectionLineStyle.Subtract, display.LostStyle);
        Assert.Equal([Vanishing(lost), Faint(result), Bright(_shape, SelectionLineStyle.Subtract)], display.Outlines);
        Assert.False(display.UnselectedHatchMoves);
    }

    [Fact]
    public void Intersecting_RunsWhatStays_OverTheShapesFaintOutline_BothInIntersectsColours_AndTheOldOneVanishing()
    {
        var display = Preview(SelectionCombineMode.Intersect);

        var result = Rectangle(1, 1, 1, 1);
        Assert.Equal(result, display.Selected);
        Assert.Equal(_before.CombinedWith(_shape, SelectionCombineMode.Subtract), display.Lost);
        Assert.Equal(SelectionLineStyle.Intersect, display.LostStyle);
        Assert.Equal(
            [
                Vanishing(_before), Faint(_shape, SelectionLineStyle.Intersect),
                Bright(result, SelectionLineStyle.Intersect)
            ],
            display.Outlines);
        Assert.False(display.UnselectedHatchMoves);
    }

    [Fact]
    public void AnIntersectionThatLeavesNothing_HasNoRunningOutline_ButStillHatchesTheRest_AndMoves()
    {
        var preview = new SelectionPreview(_before, SelectionCombineMode.Intersect, Rectangle(3, 3, 1, 1));

        var display = SelectionDisplay.For(_before, preview)!;

        Assert.True(display.Selected.IsEmpty);
        Assert.False(display.HasRunningOutline);
        Assert.True(display.HatchesUnselected);
        Assert.True(display.IsMoving);
    }

    [Theory]
    [InlineData(SelectionCombineMode.Subtract)]
    [InlineData(SelectionCombineMode.Intersect)]
    public void AGestureWithNothingSelectedBeforeOrAfter_HatchesNothing(SelectionCombineMode mode)
    {
        var nothing = SelectionMask.Empty(_size);
        var preview = new SelectionPreview(nothing, mode, _shape);

        var display = SelectionDisplay.For(nothing, preview)!;

        Assert.False(display.HatchesUnselected);
        Assert.True(display.Lost is null || display.Lost.IsEmpty);
    }

    private static SelectionDisplay Preview(SelectionCombineMode mode) =>
        SelectionDisplay.For(_before, new SelectionPreview(_before, mode, _shape))!;

    private static SelectionOutlineLayer Bright(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, SelectionOutlineStrength.Bright);

    private static SelectionOutlineLayer Faint(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, SelectionOutlineStrength.Faint);

    private static SelectionOutlineLayer Vanishing(SelectionMask mask) =>
        new(mask, SelectionLineStyle.Plain, SelectionOutlineStrength.Vanishing);

    private static SelectionMask Rectangle(int left, int top, int width, int height) =>
        SelectionMask.Rectangle(_size, new CellRect(left, top, width, height));
}
