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
    public void WithNoGesture_TheSelectionHasARunningOutline()
    {
        var display = SelectionDisplay.For(_before, null)!;

        Assert.Equal(_before, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Bright(_before)], display.Outlines);
        Assert.True(display.HasRunningOutline);
    }

    [Fact]
    public void Replacing_ShowsOnlyTheShape()
    {
        var display = Preview(SelectionCombineMode.Replace);

        Assert.Equal(_shape, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Bright(_shape)], display.Outlines);
    }

    [Fact]
    public void Adding_ShowsBothOutlinesFaintly_UnderTheResultsBrightOne()
    {
        var display = Preview(SelectionCombineMode.Add);

        var result = _before.CombinedWith(_shape, SelectionCombineMode.Add);
        Assert.Equal(result, display.Selected);
        Assert.Null(display.Lost);
        Assert.Equal([Faint(_before), Faint(_shape), Bright(result)], display.Outlines);
    }

    [Fact]
    public void Subtracting_ShowsTheSelectionFaintly_TheShapeInSubtractsColours_AndWhatItWouldDeselect()
    {
        var display = Preview(SelectionCombineMode.Subtract);

        Assert.Equal(_before.CombinedWith(_shape, SelectionCombineMode.Subtract), display.Selected);
        Assert.Equal(Rectangle(1, 1, 1, 1), display.Lost);
        Assert.Equal(SelectionLineStyle.Subtract, display.LostStyle);
        Assert.Equal([Faint(_before), Bright(_shape, SelectionLineStyle.Subtract)], display.Outlines);
    }

    [Fact]
    public void Intersecting_ShowsTheShapeFaintly_WhatStaysBrightly_BothInIntersectsColours_AndWhatItWouldDeselect()
    {
        var display = Preview(SelectionCombineMode.Intersect);

        var result = Rectangle(1, 1, 1, 1);
        Assert.Equal(result, display.Selected);
        Assert.Equal(_before.CombinedWith(_shape, SelectionCombineMode.Subtract), display.Lost);
        Assert.Equal(SelectionLineStyle.Intersect, display.LostStyle);
        Assert.Equal(
            [Faint(_shape, SelectionLineStyle.Intersect), Bright(result, SelectionLineStyle.Intersect)],
            display.Outlines);
    }

    [Fact]
    public void AnIntersectionThatLeavesNothing_HasNoRunningOutline()
    {
        var preview = new SelectionPreview(_before, SelectionCombineMode.Intersect, Rectangle(3, 3, 1, 1));

        var display = SelectionDisplay.For(_before, preview)!;

        Assert.True(display.Selected.IsEmpty);
        Assert.False(display.HasRunningOutline);
    }

    private static SelectionDisplay Preview(SelectionCombineMode mode) =>
        SelectionDisplay.For(_before, new SelectionPreview(_before, mode, _shape))!;

    private static SelectionOutlineLayer Bright(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, IsFaint: false);

    private static SelectionOutlineLayer Faint(SelectionMask mask,
        SelectionLineStyle style = SelectionLineStyle.Plain) => new(mask, style, IsFaint: true);

    private static SelectionMask Rectangle(int left, int top, int width, int height) =>
        SelectionMask.Rectangle(_size, new CellRect(left, top, width, height));
}
