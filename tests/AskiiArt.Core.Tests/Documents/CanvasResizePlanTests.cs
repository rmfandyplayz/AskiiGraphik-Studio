// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CanvasResizePlanTests
{
    [Theory]
    [InlineData(ResizeAnchor.TopLeft, 0, 0)]
    [InlineData(ResizeAnchor.Top, 2, 0)]
    [InlineData(ResizeAnchor.TopRight, 4, 0)]
    [InlineData(ResizeAnchor.Left, 0, 3)]
    [InlineData(ResizeAnchor.Center, 2, 3)]
    [InlineData(ResizeAnchor.Right, 4, 3)]
    [InlineData(ResizeAnchor.BottomLeft, 0, 6)]
    [InlineData(ResizeAnchor.Bottom, 2, 6)]
    [InlineData(ResizeAnchor.BottomRight, 4, 6)]
    public void Growing_MovesTheCellsAwayFromTheAnchor(ResizeAnchor anchor, int expectedX, int expectedY)
    {
        var plan = CanvasResizePlan.Create(new CanvasSize(10, 10), new CanvasSize(14, 16), anchor);

        Assert.Equal(new CellPoint(expectedX, expectedY), plan.ContentOffset);
        Assert.Equal(new CellRect(0, 0, 10, 10), plan.KeptRegion);
        Assert.Empty(plan.CroppedRegions);
    }

    [Theory]
    [InlineData(ResizeAnchor.TopLeft, 0, 0)]
    [InlineData(ResizeAnchor.Center, -2, -3)]
    [InlineData(ResizeAnchor.BottomRight, -4, -6)]
    public void Shrinking_CropsAwayFromTheAnchor(ResizeAnchor anchor, int expectedX, int expectedY)
    {
        var plan = CanvasResizePlan.Create(new CanvasSize(14, 16), new CanvasSize(10, 10), anchor);

        Assert.Equal(new CellPoint(expectedX, expectedY), plan.ContentOffset);
        Assert.Equal(new CellRect(-expectedX, -expectedY, 10, 10), plan.KeptRegion);
    }

    [Fact]
    public void ACenteredOddChange_PutsTheExtraCellAtTheRightAndBottom()
    {
        var grow = CanvasResizePlan.Create(new CanvasSize(10, 10), new CanvasSize(13, 11), ResizeAnchor.Center);
        var shrink = CanvasResizePlan.Create(new CanvasSize(13, 11), new CanvasSize(10, 10), ResizeAnchor.Center);

        // Growing by 3 adds 1 column at the left and 2 at the right; growing by 1 adds the row at the bottom.
        Assert.Equal(new CellPoint(1, 0), grow.ContentOffset);
        // Shrinking back crops the same columns and row, so the cells return to where they were.
        Assert.Equal(new CellPoint(-1, 0), shrink.ContentOffset);
    }

    [Fact]
    public void ResizingBackWithTheSameAnchor_ExactlyReversesTheMove()
    {
        int[] lengths = [1, 2, 3, 7, 10];

        foreach (var anchor in Enum.GetValues<ResizeAnchor>())
        {
            foreach (var oldWidth in lengths)
            {
                foreach (var newWidth in lengths)
                {
                    var oldSize = new CanvasSize(oldWidth, 5);
                    var newSize = new CanvasSize(newWidth, 3);

                    var forward = CanvasResizePlan.Create(oldSize, newSize, anchor);
                    var back = CanvasResizePlan.Create(newSize, oldSize, anchor);

                    Assert.Equal(forward.ContentOffset.Negate(), back.ContentOffset);
                }
            }
        }
    }

    [Fact]
    public void KeptAndCroppedRegions_CoverTheOldCanvasExactlyOnce()
    {
        int[] lengths = [1, 2, 3, 6];

        foreach (var anchor in Enum.GetValues<ResizeAnchor>())
        {
            foreach (var oldWidth in lengths)
            {
                foreach (var newWidth in lengths)
                {
                    foreach (var newHeight in lengths)
                    {
                        var oldSize = new CanvasSize(oldWidth, 4);
                        var plan = CanvasResizePlan.Create(oldSize, new CanvasSize(newWidth, newHeight), anchor);
                        AssertCoveredExactlyOnce(oldSize, [plan.KeptRegion, .. plan.CroppedRegions]);
                        Assert.Equal(Math.Min(oldWidth, newWidth) * Math.Min(4, newHeight), plan.KeptRegion.Area);
                    }
                }
            }
        }
    }

    [Fact]
    public void CroppedRegions_AreTheStripsOutsideTheKeptRegion()
    {
        var plan = CanvasResizePlan.Create(new CanvasSize(5, 5), new CanvasSize(3, 3), ResizeAnchor.Center);

        Assert.Equal(new CellRect(1, 1, 3, 3), plan.KeptRegion);
        Assert.Equal(
            [new CellRect(0, 0, 5, 1), new CellRect(0, 4, 5, 1), new CellRect(0, 1, 1, 3), new CellRect(4, 1, 1, 3)],
            plan.CroppedRegions);
    }

    private static void AssertCoveredExactlyOnce(CanvasSize size, IEnumerable<CellRect> rects)
    {
        var coverCount = new int[size.Width, size.Height];
        foreach (var rect in rects)
        {
            Assert.True(size.Bounds.Contains(rect), $"{rect} is outside the canvas.");
            for (var y = rect.Top; y < rect.Bottom; y++)
            {
                for (var x = rect.Left; x < rect.Right; x++)
                    coverCount[x, y]++;
            }
        }

        Assert.All(coverCount.Cast<int>(), count => Assert.Equal(1, count));
    }
}
