// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas.FrameRate;

/// <summary>
/// The canvas's frame rate over the last couple of seconds, for the Debug readout (ADR 0040): the average, the 1% low
/// (the average rate of the slowest 1% of frames, which shows stutter that an average hides), and the longest the
/// canvas spent drawing one frame.
/// </summary>
internal readonly record struct FrameRateSummary(
    double AverageFramesPerSecond, double OnePercentLowFramesPerSecond, double SlowestDrawMilliseconds)
{
    /// <summary>How far back the summary looks.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The longest wait between two frames that still counts as a frame. The canvas redraws only when something
    /// changes, so a longer wait means nothing changed, not that a frame was slow.
    /// </summary>
    public static readonly TimeSpan LongestFrame = TimeSpan.FromMilliseconds(100);

    // The 1% low averages the slowest frame in every this many, and at least one.
    private const int FramesPerSlowFrame = 100;

    /// <summary>
    /// Summarizes <paramref name="frames"/>, oldest first, as of <paramref name="now"/>, with times in ticks of which
    /// there are <paramref name="ticksPerSecond"/> a second. Null while the canvas is idle: when the window has no two
    /// frames close enough together to make a rate.
    /// </summary>
    public static FrameRateSummary? Calculate(IReadOnlyList<FrameTiming> frames, long now, long ticksPerSecond)
    {
        ArgumentNullException.ThrowIfNull(frames);

        var windowStart = now - ToTicks(Window, ticksPerSecond);
        var longestFrame = ToTicks(LongestFrame, ticksPerSecond);
        var frameTimes = new List<long>();
        long slowestDraw = 0;
        long? previousPaintedAt = null;
        foreach (var frame in frames)
        {
            if (frame.PaintedAt < windowStart)
                continue;

            if (previousPaintedAt is { } previous && frame.PaintedAt - previous is > 0 and var frameTime &&
                frameTime <= longestFrame)
            {
                frameTimes.Add(frameTime);
            }

            slowestDraw = Math.Max(slowestDraw, frame.DrawTicks);
            previousPaintedAt = frame.PaintedAt;
        }

        if (frameTimes.Count == 0)
            return null;

        frameTimes.Sort();
        var slowestFrameTimes = frameTimes.TakeLast(Math.Max(1, frameTimes.Count / FramesPerSlowFrame)).ToList();
        return new FrameRateSummary(
            FramesPerSecond(frameTimes, ticksPerSecond),
            FramesPerSecond(slowestFrameTimes, ticksPerSecond),
            slowestDraw * 1000.0 / ticksPerSecond);
    }

    private static double FramesPerSecond(List<long> frameTimes, long ticksPerSecond) =>
        frameTimes.Count * (double)ticksPerSecond / frameTimes.Sum();

    private static long ToTicks(TimeSpan time, long ticksPerSecond) => (long)(time.TotalSeconds * ticksPerSecond);
}
