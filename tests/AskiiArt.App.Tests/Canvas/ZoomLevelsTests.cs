// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;

namespace AskiiArt.App.Tests.Canvas;

public sealed class ZoomLevelsTests
{
    [Fact]
    public void Factors_AreAscendingAndIncludeActualSize()
    {
        Assert.Equal(ZoomLevels.Factors.Order(), ZoomLevels.Factors);
        Assert.Contains(ZoomLevels.ActualSize, ZoomLevels.Factors);
    }

    [Theory]
    [InlineData(1, 1.25)]
    [InlineData(1.1, 1.25)] // between steps: the next one up
    [InlineData(0.1, 0.25)]
    [InlineData(8, 8)] // already at the top
    public void StepIn_GoesToTheNextLargerStep(double factor, double expected)
    {
        Assert.Equal(expected, ZoomLevels.StepIn(factor), 6);
    }

    [Theory]
    [InlineData(1, 0.75)]
    [InlineData(0.9, 0.75)]
    [InlineData(20, 8)]
    [InlineData(0.25, 0.25)] // already at the bottom
    public void StepOut_GoesToTheNextSmallerStep(double factor, double expected)
    {
        Assert.Equal(expected, ZoomLevels.StepOut(factor), 6);
    }

    [Fact]
    public void SteppingInThenOut_ReturnsToTheSameStep()
    {
        foreach (var factor in ZoomLevels.Factors.Skip(1).SkipLast(1))
            Assert.Equal(factor, ZoomLevels.StepOut(ZoomLevels.StepIn(factor)), 6);
    }
}
