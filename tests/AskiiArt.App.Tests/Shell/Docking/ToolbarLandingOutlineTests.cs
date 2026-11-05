// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;

namespace AskiiArt.App.Tests.Shell.Docking;

public sealed class ToolbarLandingOutlineTests
{
    private const double Tolerance = 0.001;
    private const double Length = 4;

    private static readonly TimeSpan _period = TimeSpan.FromSeconds(2);

    [Fact]
    public void ItStartsAtItsBounds()
    {
        Assert.Equal(0, ToolbarLandingOutline.PulseAt(TimeSpan.Zero, _period, Length), Tolerance);
    }

    [Fact]
    public void ItsFurthestOut_HalfwayThroughAPeriod()
    {
        Assert.Equal(Length, ToolbarLandingOutline.PulseAt(_period / 2, _period, Length), Tolerance);
    }

    [Fact]
    public void ItsBack_AfterAWholePeriod_AndKeepsGoing()
    {
        Assert.Equal(0, ToolbarLandingOutline.PulseAt(_period, _period, Length), Tolerance);
        Assert.Equal(Length, ToolbarLandingOutline.PulseAt(_period * 2.5, _period, Length), Tolerance);
    }

    [Fact]
    public void ItNeverGoesInsideItsBounds_OrFurtherOutThanItsLength()
    {
        for (var step = 0; step < 100; step++)
            Assert.InRange(ToolbarLandingOutline.PulseAt(_period * (step * 0.037), _period, Length), 0, Length);
    }

    [Fact]
    public void WithNoPeriod_ItHoldsStill()
    {
        Assert.Equal(0, ToolbarLandingOutline.PulseAt(TimeSpan.FromSeconds(1), TimeSpan.Zero, Length));
    }
}
