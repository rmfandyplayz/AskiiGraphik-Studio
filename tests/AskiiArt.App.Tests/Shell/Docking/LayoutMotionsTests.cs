// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using AskiiArt.Docking.Solving;

namespace AskiiArt.App.Tests.Shell.Docking;

public sealed class LayoutMotionsTests
{
    private static readonly TimeSpan _duration = TimeSpan.FromMilliseconds(100);
    private static readonly LayoutRect _here = new(0, 0, 100, 40);
    private static readonly LayoutRect _there = new(200, 100, 100, 40);

    private readonly LayoutMotions _motions = new();

    [Fact]
    public void SomethingNew_AppearsWhereItBelongs()
    {
        Assert.Equal(_here, Advance(_here, 0, animate: true));
        Assert.False(_motions.IsMoving);
    }

    [Fact]
    public void AnAnimatedChange_MovesOverTime_AndArrives()
    {
        Advance(_here, 0, animate: false);

        var started = Advance(_there, 0, animate: true);
        Assert.Equal(_here, started);
        Assert.True(_motions.IsMoving);

        var halfway = Advance(_there, 50, animate: false);
        Assert.InRange(halfway.X, 1, _there.X - 1);

        Assert.Equal(_there, Advance(_there, 100, animate: false));
        Assert.False(_motions.IsMoving);
    }

    [Fact]
    public void AChangeThatIsntAnimated_Snaps()
    {
        Advance(_here, 0, animate: false);

        Assert.Equal(_there, Advance(_there, 0, animate: false));
        Assert.False(_motions.IsMoving);
    }

    [Fact]
    public void ANewTargetInMidMove_StartsFromWhereItHasGotTo()
    {
        Advance(_here, 0, animate: false);
        Advance(_there, 0, animate: true);
        var halfway = Advance(_there, 50, animate: false);

        var other = new LayoutRect(0, 500, 100, 40);
        Assert.Equal(halfway, Advance(other, 50, animate: true));
    }

    [Fact]
    public void ResizingTheWindowInMidMove_Snaps()
    {
        Advance(_here, 0, animate: false);
        Advance(_there, 0, animate: true);

        var resized = new LayoutRect(300, 100, 100, 40);

        Assert.Equal(resized, Advance(resized, 10, animate: false));
        Assert.False(_motions.IsMoving);
    }

    [Fact]
    public void NoDuration_MeansNoMove()
    {
        Advance(_here, 0, animate: false);

        Assert.Equal(_there, _motions.Advance("toolbar", _there, TimeSpan.Zero, TimeSpan.Zero, animateChanges: true));
        Assert.False(_motions.IsMoving);
    }

    [Fact]
    public void SomethingPlaced_MovesOnFromThere()
    {
        Advance(_here, 0, animate: false);
        _motions.Place("toolbar", _there);

        Assert.Equal(_there, Advance(_here, 0, animate: true));
        Assert.True(_motions.IsMoving);
    }

    [Fact]
    public void SomethingForgotten_DoesntSlideInWhenItsBack()
    {
        Advance(_here, 0, animate: false);
        _motions.Forget("toolbar");

        Assert.Equal(_there, Advance(_there, 0, animate: true));
        Assert.False(_motions.IsMoving);
    }

    private LayoutRect Advance(LayoutRect target, int milliseconds, bool animate) =>
        _motions.Advance("toolbar", target, TimeSpan.FromMilliseconds(milliseconds), _duration, animate);
}
