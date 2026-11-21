// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class PausableClockTests
{
    [Fact]
    public void ItRunsFromZero_AsTheOtherClockDoes()
    {
        var clock = new PausableClock();

        Assert.Equal(Seconds(3), clock.Elapsed(Seconds(3), keepsRunning: true));
        Assert.Equal(Seconds(5), clock.Elapsed(Seconds(5), keepsRunning: true));
    }

    [Fact]
    public void StoppedIt_StandsStill_ThenCarriesOnFromWhereItStopped()
    {
        var clock = new PausableClock();

        Assert.Equal(Seconds(2), clock.Elapsed(Seconds(2), keepsRunning: false));
        Assert.Equal(Seconds(2), clock.Elapsed(Seconds(7), keepsRunning: true));
        Assert.Equal(Seconds(3), clock.Elapsed(Seconds(8), keepsRunning: true));
    }

    [Fact]
    public void ReadingTheSameMomentAgain_ChangesNothing()
    {
        var clock = new PausableClock();
        clock.Elapsed(Seconds(4), keepsRunning: true);

        Assert.Equal(Seconds(4), clock.Elapsed(Seconds(4), keepsRunning: true));
        Assert.Equal(Seconds(4), clock.Elapsed(Seconds(3), keepsRunning: true));
    }

    private static TimeSpan Seconds(double seconds) => TimeSpan.FromSeconds(seconds);
}
