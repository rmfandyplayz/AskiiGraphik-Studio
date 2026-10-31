// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CanvasSizeTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(80, 25)]
    [InlineData(4096, 4096)]
    public void Constructor_AcceptsSizesWithinTheLimits(int width, int height)
    {
        var size = new CanvasSize(width, height);

        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
        Assert.Equal(width * height, size.CellCount);
        Assert.Equal(new CellRect(0, 0, width, height), size.Bounds);
        Assert.True(CanvasSize.IsValid(width, height));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-5, 10)]
    [InlineData(4097, 10)]
    [InlineData(10, 4097)]
    public void Constructor_RejectsSizesOutsideTheLimits(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CanvasSize(width, height));
        Assert.False(CanvasSize.IsValid(width, height));
    }

    [Theory]
    [InlineData(500, 500, false)]
    [InlineData(501, 10, true)]
    [InlineData(10, 501, true)]
    public void ExceedsRecommendedSize_IsTrueWhenEitherSideIsAboveTheSoftLimit(int width, int height, bool expected)
    {
        Assert.Equal(expected, new CanvasSize(width, height).ExceedsRecommendedSize);
    }

    [Fact]
    public void Contains_ChecksThePointIsACellOfTheCanvas()
    {
        var size = new CanvasSize(3, 2);

        Assert.True(size.Contains(new CellPoint(2, 1)));
        Assert.False(size.Contains(new CellPoint(3, 1)));
        Assert.False(size.Contains(new CellPoint(0, -1)));
    }
}
