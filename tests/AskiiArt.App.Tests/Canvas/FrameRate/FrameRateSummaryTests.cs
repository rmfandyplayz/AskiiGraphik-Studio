// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.FrameRate;

namespace AskiiArt.App.Tests.Canvas.FrameRate;

public sealed class FrameRateSummaryTests
{
    // A tick is a millisecond here, to keep the numbers readable.
    private const long TicksPerSecond = 1000;

    [Fact]
    public void Calculate_SteadyFrames_GiveTheirRate()
    {
        var summary = FrameRateSummary.Calculate([.. Frames(from: 0, frameTime: 20, count: 51)], 1000, TicksPerSecond);

        Assert.NotNull(summary);
        Assert.Equal(50, summary.Value.AverageFramesPerSecond, 6);
        Assert.Equal(50, summary.Value.OnePercentLowFramesPerSecond, 6);
    }

    [Fact]
    public void Calculate_OneSlowFrame_LowersTheOnePercentLow()
    {
        // 100 frames at 100 a second, then one that took 50 ms.
        List<FrameTiming> frames = [.. Frames(from: 0, frameTime: 10, count: 101), new(1050, 0)];

        var summary = FrameRateSummary.Calculate(frames, 1050, TicksPerSecond)!.Value;

        Assert.Equal(20, summary.OnePercentLowFramesPerSecond, 6);
        Assert.Equal(101 / 1.05, summary.AverageFramesPerSecond, 6);
    }

    [Fact]
    public void Calculate_APauseWhenNothingChanged_IsntASlowFrame()
    {
        List<FrameTiming> frames = [.. Frames(from: 0, frameTime: 20, count: 11), .. Frames(from: 700, 20, 11)];

        var summary = FrameRateSummary.Calculate(frames, 900, TicksPerSecond)!.Value;

        Assert.Equal(50, summary.AverageFramesPerSecond, 6);
        Assert.Equal(50, summary.OnePercentLowFramesPerSecond, 6);
    }

    [Fact]
    public void Calculate_LeavesOutFramesFromBeforeTheWindow()
    {
        // Slow frames more than 2 seconds ago, then quick ones.
        List<FrameTiming> frames = [.. Frames(from: 0, frameTime: 50, count: 11), .. Frames(from: 3000, 20, 51)];

        var summary = FrameRateSummary.Calculate(frames, 4000, TicksPerSecond)!.Value;

        Assert.Equal(50, summary.OnePercentLowFramesPerSecond, 6);
        Assert.Null(FrameRateSummary.Calculate(frames, 7000, TicksPerSecond));
    }

    [Fact]
    public void Calculate_WithoutTwoFramesToMakeARate_IsIdle()
    {
        Assert.Null(FrameRateSummary.Calculate([], 1000, TicksPerSecond));
        Assert.Null(FrameRateSummary.Calculate([new(900, 0)], 1000, TicksPerSecond));
        Assert.Null(FrameRateSummary.Calculate([new(100, 0), new(900, 0)], 1000, TicksPerSecond));
    }

    [Fact]
    public void Calculate_FramesPaintedAtTheSameMoment_DontCountAsInfinitelyQuick()
    {
        var summary = FrameRateSummary.Calculate([new(0, 0), new(0, 0), new(20, 0), new(40, 0)], 40,
            TicksPerSecond)!.Value;

        Assert.Equal(50, summary.AverageFramesPerSecond, 6);
    }

    [Fact]
    public void Calculate_GivesTheLongestDraw()
    {
        var summary = FrameRateSummary.Calculate([new(0, 2), new(20, 7), new(40, 3)], 40, TicksPerSecond)!.Value;

        Assert.Equal(7, summary.SlowestDrawMilliseconds, 6);
    }

    private static IEnumerable<FrameTiming> Frames(long from, long frameTime, int count) =>
        Enumerable.Range(0, count).Select(index => new FrameTiming(from + index * frameTime, 0));
}
