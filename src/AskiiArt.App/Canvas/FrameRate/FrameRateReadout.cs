// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using System.Globalization;
using AskiiArt.App.Resources;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Canvas.FrameRate;

/// <summary>
/// Debug → Show Frame Rate's readout at the right of the status bar (ADR 0040). While it shows, the canvas records
/// its frames, and the text is refreshed twice a second from the last two seconds of them.
/// </summary>
internal sealed class FrameRateReadout : ObservableObject, IDisposable
{
    private static readonly TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(500);

    private readonly Func<long> _clock;
    private DispatcherTimer? _timer;

    public FrameRateReadout()
        : this(Stopwatch.GetTimestamp)
    {
    }

    /// <summary>For tests: <paramref name="clock"/> gives the time in <see cref="Stopwatch"/> ticks.</summary>
    internal FrameRateReadout(Func<long> clock)
    {
        _clock = clock;
    }

    /// <summary>Where the canvas records its frames while the readout shows.</summary>
    public FrameTimings Timings { get; } = new();

    public bool IsVisible
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string Text
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Updates <see cref="Text"/> from the frames recorded so far. A timer calls it while the readout shows.
    /// </summary>
    internal void Refresh()
    {
        if (Timings.Summarize(_clock()) is { } summary)
        {
            Text = string.Format(CultureInfo.CurrentCulture, Strings.StatusFrameRateFormat,
                summary.AverageFramesPerSecond, summary.OnePercentLowFramesPerSecond, summary.SlowestDrawMilliseconds);
        }
        else
        {
            Text = Strings.StatusFrameRateIdle;
        }
    }

    public void Dispose() => Hide();

    private void Show()
    {
        Timings.IsRecording = true;
        Refresh();
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = _refreshInterval };
        _timer.Tick += OnTimerTick;
        _timer.Start();
        IsVisible = true;
    }

    private void Hide()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        Timings.IsRecording = false;
        IsVisible = false;
    }

    private void OnTimerTick(object? sender, EventArgs e) => Refresh();
}
