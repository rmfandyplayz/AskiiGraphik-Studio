// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas;

/// <summary>What one mouse-wheel event does to the canvas.</summary>
internal enum WheelAction
{
    Scroll,
    ScrollSideways,
    Zoom,
    Ignore
}

/// <summary>
/// Groups mouse-wheel events into gestures, so the keys held when a gesture starts decide what the
/// whole gesture does. Touchpads (and some free-spinning mouse wheels) keep sending wheel events
/// after the fingers lift, to glide to a stop; without this, pressing Cmd for Cmd+C during that
/// glide would turn the rest of it into a zoom. Avalonia doesn't say which events are the glide,
/// so a pause decides where one gesture ends and the next begins.
/// </summary>
internal sealed class WheelGestureTracker
{
    // Longer than the gaps between a glide's events or a fast-spinning wheel's notches, and
    // shorter than the pause between finishing a scroll and starting to zoom.
    internal const ulong GestureGapMilliseconds = 250;

    private ulong? _lastTimestamp;
    private WheelAction _gestureAction;
    private bool _hasZoomed;
    private double _zoomRemainder;

    /// <summary>
    /// What the wheel event at <paramref name="timestamp"/> (in milliseconds) does, given the keys
    /// held for it.
    /// </summary>
    public WheelAction Track(ulong timestamp, bool isZoomKeyHeld, bool isSidewaysKeyHeld)
    {
        if (_lastTimestamp is not { } last || timestamp < last || timestamp - last > GestureGapMilliseconds)
            StartGesture(isZoomKeyHeld, isSidewaysKeyHeld);

        _lastTimestamp = timestamp;

        // Letting go of the zoom key ends the zoom. The rest of the gesture is dropped rather than
        // turned into a sudden scroll.
        if (_gestureAction == WheelAction.Zoom && !isZoomKeyHeld)
            return WheelAction.Ignore;

        return _gestureAction;
    }

    /// <summary>
    /// How many zoom steps a zooming wheel event moves, in or out by sign. A gesture's first event
    /// always moves one step, so a single notch zooms however small a delta the platform reports
    /// for it. After that, steps follow the distance scrolled, one per notch's worth, so a
    /// touchpad's stream of small events doesn't race through every zoom level.
    /// </summary>
    public int TakeZoomSteps(double delta)
    {
        if (delta == 0)
            return 0;

        if (!_hasZoomed)
        {
            _hasZoomed = true;
            return Math.Sign(delta);
        }

        _zoomRemainder += delta;
        var steps = (int)Math.Truncate(_zoomRemainder);
        _zoomRemainder -= steps;
        return steps;
    }

    private void StartGesture(bool isZoomKeyHeld, bool isSidewaysKeyHeld)
    {
        if (isZoomKeyHeld)
            _gestureAction = WheelAction.Zoom;
        else if (isSidewaysKeyHeld)
            _gestureAction = WheelAction.ScrollSideways;
        else
            _gestureAction = WheelAction.Scroll;

        _hasZoomed = false;
        _zoomRemainder = 0;
    }
}
