// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.App.Canvas.FrameRate;

namespace AskiiArt.App.Tests.Canvas.FrameRate;

public sealed class FrameTimingsTests
{
    private static readonly long _frameTime = Stopwatch.Frequency / 50;

    [Fact]
    public void Summarize_GivesTheRateOfTheFramesRecorded()
    {
        var timings = new FrameTimings { IsRecording = true };

        var now = RecordFrames(timings, from: 0, count: 51, drawTicks: 0);

        Assert.Equal(50, timings.Summarize(now)!.Value.AverageFramesPerSecond, 3);
    }

    [Fact]
    public void Record_WhileNotRecording_KeepsNothing()
    {
        var timings = new FrameTimings();

        var now = RecordFrames(timings, from: 0, count: 51, drawTicks: 0);

        Assert.Null(timings.Summarize(now));
    }

    [Fact]
    public void Record_PastItsCapacity_KeepsTheNewestFrames()
    {
        var timings = new FrameTimings { IsRecording = true };

        // The oldest frames took a whole second to draw; the newest, which fill the ring, took none.
        var afterOldest = RecordFrames(timings, from: 0, count: 10, drawTicks: Stopwatch.Frequency);
        var now = RecordFrames(timings, from: afterOldest, count: FrameTimings.Capacity, drawTicks: 0);

        Assert.Equal(0, timings.Summarize(now)!.Value.SlowestDrawMilliseconds);
    }

    [Fact]
    public void IsRecording_TurnedOnAgain_ForgetsTheOldFrames()
    {
        var timings = new FrameTimings { IsRecording = true };
        var now = RecordFrames(timings, from: 0, count: 51, drawTicks: 0);

        timings.IsRecording = false;
        timings.IsRecording = true;

        Assert.Null(timings.Summarize(now));
    }

    // Records frames 50 a second from the given time, and returns when the last one was painted.
    private static long RecordFrames(FrameTimings timings, long from, int count, long drawTicks)
    {
        var paintedAt = from;
        for (var index = 0; index < count; index++)
        {
            paintedAt = from + index * _frameTime;
            timings.Record(paintedAt, drawTicks);
        }

        return paintedAt;
    }
}
