// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Shell;

public sealed class StatusBarViewTests
{
    [AvaloniaFact]
    public void FrameRate_ShowsAtTheRight_OnlyWhileTurnedOn()
    {
        using var canvas = new TestCanvas();
        var view = new StatusBarView { DataContext = canvas.ViewModel };
        var window = new Window { Content = view };
        window.Show();
        var frameRate = view.FindControl<TextBlock>("FrameRateText")!;

        Assert.False(frameRate.IsVisible);

        canvas.ViewModel.FrameRate.Toggle();

        Assert.True(frameRate.IsVisible);
        Assert.Equal(Strings.StatusFrameRateIdle, frameRate.Text);
        Assert.Equal(Dock.Right, DockPanel.GetDock(frameRate));
        window.Close();
    }
}
