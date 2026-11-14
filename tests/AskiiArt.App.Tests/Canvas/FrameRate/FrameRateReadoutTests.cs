// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using System.Globalization;
using AskiiArt.App.Canvas;
using AskiiArt.App.Canvas.FrameRate;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Canvas.FrameRate;

public sealed class FrameRateReadoutTests
{
    private long _now;

    [AvaloniaFact]
    public void Toggle_ShowsTheReadoutAndRecordsFrames_ThenHidesItAndStops()
    {
        using var readout = new FrameRateReadout(() => _now);

        readout.Toggle();

        Assert.True(readout.IsVisible);
        Assert.True(readout.Timings.IsRecording);
        Assert.Equal(Strings.StatusFrameRateIdle, readout.Text);

        readout.Toggle();

        Assert.False(readout.IsVisible);
        Assert.False(readout.Timings.IsRecording);
    }

    [AvaloniaFact]
    public void Refresh_ShowsTheRateOfTheRecordedFrames()
    {
        using var readout = new FrameRateReadout(() => _now);
        readout.Toggle();
        var frameTime = Stopwatch.Frequency / 50;
        for (var index = 0; index <= 50; index++)
        {
            _now = index * frameTime;
            readout.Timings.Record(_now, Stopwatch.Frequency / 1000);
        }

        readout.Refresh();

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.StatusFrameRateFormat, 50, 50, 1.0),
            readout.Text);
    }

    [AvaloniaFact]
    public void ShowFrameRate_IsADebugActionWhoseCheckMarkFollowsTheReadout()
    {
        using var canvas = new TestCanvas();
        var showFrameRate = new CanvasActions(canvas.ViewModel, includeDebugActions: true).CreateActions()
            .Single(action => action.Id == CanvasActionIds.ShowFrameRate);
        var checkMarkChanges = 0;
        showFrameRate.CheckedState!.Changed += (_, _) => checkMarkChanges++;

        showFrameRate.Command.Execute(null);

        Assert.True(canvas.ViewModel.FrameRate.IsVisible);
        Assert.True(showFrameRate.CheckedState.IsChecked);
        Assert.Equal(1, checkMarkChanges);
        Assert.DoesNotContain(new CanvasActions(canvas.ViewModel, includeDebugActions: false).CreateActions(),
            action => action.Id == CanvasActionIds.ShowFrameRate);
    }
}
