// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;

namespace AskiiArt.App.Tests.Shell.Docking;

public sealed class MarchingAntsTests
{
    private const double Tolerance = 0.000001;

    [Theory]
    [InlineData(100, 6, 4)]
    [InlineData(103, 6, 4)]
    [InlineData(1234.5, 6, 4.5)]
    [InlineData(3, 6, 4)]
    public void ThePattern_FitsAWholeNumberOfTimesAroundTheLine_WithNoSeam(double length, double dash, double gap)
    {
        var ants = MarchingAnts.Around(length, dash, gap);

        var times = length / ants.Period;
        Assert.Equal(Math.Round(times), times, Tolerance);
    }

    [Theory]
    [InlineData(1000, 6, 4)]
    [InlineData(1003, 6, 4)]
    public void ThePattern_StaysCloseToTheOneAskedFor_AndKeepsItsProportions(double length, double dash, double gap)
    {
        var ants = MarchingAnts.Around(length, dash, gap);

        Assert.InRange(ants.Dash, dash * 0.99, dash * 1.01);
        Assert.Equal(dash / gap, ants.Dash / ants.Gap, Tolerance);
    }

    [Fact]
    public void NoLine_LeavesThePatternAlone()
    {
        Assert.Equal(new MarchingAnts(6, 4), MarchingAnts.Around(0, 6, 4));
    }

    [Fact]
    public void TheOffset_FallsAsTheDashesRun()
    {
        var ants = new MarchingAnts(6, 4);

        Assert.Equal(9, ants.OffsetAfter(1), Tolerance);
        Assert.Equal(7, ants.OffsetAfter(3), Tolerance);
    }

    [Fact]
    public void TheOffset_WrapsAroundThePattern_WithoutAJump()
    {
        var ants = new MarchingAnts(6, 4);

        Assert.Equal(0, ants.OffsetAfter(0), Tolerance);
        Assert.Equal(0, ants.OffsetAfter(10), Tolerance);
        Assert.Equal(9, ants.OffsetAfter(11), Tolerance);
        Assert.Equal(ants.OffsetAfter(3), ants.OffsetAfter(1003), Tolerance);
    }

    [Fact]
    public void AnEmptyPattern_HasNoOffset()
    {
        Assert.Equal(0, new MarchingAnts(0, 0).OffsetAfter(5));
    }
}
