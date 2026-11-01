// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;

namespace AskiiArt.App.Tests.Canvas;

public sealed class WheelGestureTrackerTests
{
    // Close enough together to be one gesture, like a touchpad's glide.
    private const ulong Glide = 16;
    private const ulong Pause = WheelGestureTracker.GestureGapMilliseconds + 1;

    private readonly WheelGestureTracker _tracker = new();

    [Fact]
    public void ZoomKeyPressedDuringAScrollGlide_KeepsScrolling()
    {
        Assert.Equal(WheelAction.Scroll, Track(1000));

        Assert.Equal(WheelAction.Scroll, Track(1000 + Glide, zoomKey: true));
        Assert.Equal(WheelAction.Scroll, Track(1000 + 2 * Glide, zoomKey: true));
    }

    [Fact]
    public void ZoomKeyHeldAfterAPause_Zooms()
    {
        Track(1000);

        Assert.Equal(WheelAction.Zoom, Track(1000 + Pause, zoomKey: true));
    }

    [Fact]
    public void ZoomKeyReleasedDuringAZoomGlide_IgnoresTheRestOfIt_ThenScrollsAfterAPause()
    {
        Assert.Equal(WheelAction.Zoom, Track(1000, zoomKey: true));

        Assert.Equal(WheelAction.Ignore, Track(1000 + Glide));
        Assert.Equal(WheelAction.Ignore, Track(1000 + 2 * Glide));
        Assert.Equal(WheelAction.Scroll, Track(1000 + 2 * Glide + Pause));
    }

    [Fact]
    public void ShiftDecidesSidewaysScrollingAtTheStartOfAGesture()
    {
        Assert.Equal(WheelAction.ScrollSideways, Track(1000, sidewaysKey: true));
        Assert.Equal(WheelAction.ScrollSideways, Track(1000 + Glide));

        Assert.Equal(WheelAction.Scroll, Track(1000 + Glide + Pause));
        Assert.Equal(WheelAction.Scroll, Track(1000 + 2 * Glide + Pause, sidewaysKey: true));
    }

    [Fact]
    public void ATimestampGoingBackwards_StartsANewGesture()
    {
        Track(5000);

        Assert.Equal(WheelAction.Zoom, Track(10, zoomKey: true));
    }

    [Fact]
    public void TakeZoomSteps_FirstEventOfAGestureIsAlwaysOneStep()
    {
        Track(1000, zoomKey: true);

        Assert.Equal(1, _tracker.TakeZoomSteps(0.1));
    }

    [Fact]
    public void TakeZoomSteps_ThenSmallDeltasAddUpToOneStepPerNotch()
    {
        Track(1000, zoomKey: true);
        _tracker.TakeZoomSteps(-0.1);

        var steps = Enumerable.Repeat(-0.4, 5).Select(_tracker.TakeZoomSteps).ToArray();

        Assert.Equal([0, 0, -1, 0, -1], steps);
    }

    [Fact]
    public void TakeZoomSteps_WholeNotchesStepOncePerNotch()
    {
        Track(1000, zoomKey: true);

        Assert.Equal(1, _tracker.TakeZoomSteps(1));
        Assert.Equal(1, _tracker.TakeZoomSteps(1));
        Assert.Equal(2, _tracker.TakeZoomSteps(2));
    }

    [Fact]
    public void TakeZoomSteps_ANewGestureForgetsTheLeftover()
    {
        Track(1000, zoomKey: true);
        _tracker.TakeZoomSteps(1);
        _tracker.TakeZoomSteps(0.9);

        Track(1000 + Pause, zoomKey: true);

        Assert.Equal(-1, _tracker.TakeZoomSteps(-0.2));
        Assert.Equal(0, _tracker.TakeZoomSteps(0.5));
    }

    private WheelAction Track(ulong timestamp, bool zoomKey = false, bool sidewaysKey = false) =>
        _tracker.Track(timestamp, zoomKey, sidewaysKey);
}
