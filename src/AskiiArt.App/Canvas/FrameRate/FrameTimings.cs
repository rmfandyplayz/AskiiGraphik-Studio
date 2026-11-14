// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;

namespace AskiiArt.App.Canvas.FrameRate;

/// <summary>
/// The canvas's recent frames, for the Debug frame-rate readout. Avalonia's render thread records each frame as it
/// paints and the UI thread summarizes them, so both take a lock. Recording only writes into a fixed ring, so it
/// allocates nothing, and it does nothing at all unless the readout is showing.
/// </summary>
internal sealed class FrameTimings
{
    /// <summary>How many frames are kept: the summary's whole window at up to 2,000 frames a second.</summary>
    internal const int Capacity = 4096;

    private readonly Lock _gate = new();
    private readonly FrameTiming[] _frames = new FrameTiming[Capacity];
    private int _next;
    private int _count;
    private volatile bool _isRecording;

    /// <summary>
    /// Whether frames are kept. Turning it on forgets any from before, so a new readout starts afresh.
    /// </summary>
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (value && !_isRecording)
                Clear();

            _isRecording = value;
        }
    }

    /// <summary>
    /// Keeps a frame that finished painting at <paramref name="paintedAt"/> after <paramref name="drawTicks"/> of the
    /// canvas's own work, both in <see cref="Stopwatch"/> ticks. Does nothing while not recording.
    /// </summary>
    public void Record(long paintedAt, long drawTicks)
    {
        if (!_isRecording)
            return;

        lock (_gate)
        {
            _frames[_next] = new FrameTiming(paintedAt, drawTicks);
            _next = (_next + 1) % Capacity;
            _count = Math.Min(_count + 1, Capacity);
        }
    }

    /// <summary>
    /// The frame rate as of <paramref name="now"/>, in <see cref="Stopwatch"/> ticks; null while the canvas is idle.
    /// </summary>
    public FrameRateSummary? Summarize(long now) => FrameRateSummary.Calculate(Snapshot(), now, Stopwatch.Frequency);

    // The frames kept, oldest first.
    private FrameTiming[] Snapshot()
    {
        lock (_gate)
        {
            var frames = new FrameTiming[_count];
            var oldest = (_next - _count + Capacity) % Capacity;
            for (var index = 0; index < _count; index++)
                frames[index] = _frames[(oldest + index) % Capacity];

            return frames;
        }
    }

    private void Clear()
    {
        lock (_gate)
        {
            _next = 0;
            _count = 0;
        }
    }
}
