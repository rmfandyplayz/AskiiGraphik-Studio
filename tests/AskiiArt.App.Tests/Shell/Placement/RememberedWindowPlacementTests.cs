// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Placement;
using AskiiArt.App.Tests.Fakes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Shell.Placement;

public sealed class RememberedWindowPlacementTests
{
    private readonly FakeAppSettings _settings = new();

    [AvaloniaFact]
    public void AWindow_OpensWhereTheLastOneClosed_AtTheSameSize()
    {
        var first = Open();
        first.Position = new PixelPoint(40, 50);
        first.Width = 700;
        first.Height = 500;
        Dispatcher.UIThread.RunJobs();
        first.Close();

        var second = Open();

        Assert.Equal(new PixelPoint(40, 50), second.Position);
        Assert.Equal(new Size(700, 500), second.ClientSize);
        second.Close();
    }

    [AvaloniaFact]
    public void AWindowClosedMaximized_OpensMaximized()
    {
        var first = Open();
        first.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();
        first.Close();

        var second = Open();

        Assert.Equal(WindowState.Maximized, second.WindowState);
        second.Close();
    }

    [AvaloniaFact]
    public void AWindowClosedFullScreen_OpensFullScreen()
    {
        var first = Open();
        first.WindowState = WindowState.FullScreen;
        Dispatcher.UIThread.RunJobs();
        first.Close();

        var second = Open();

        Assert.Equal(WindowState.FullScreen, second.WindowState);
        second.Close();
    }

    [AvaloniaFact]
    public void AWindowThatLeftFullScreen_OpensAsItWasThen()
    {
        var first = Open();
        first.WindowState = WindowState.FullScreen;
        Dispatcher.UIThread.RunJobs();
        first.WindowState = WindowState.Maximized;
        Dispatcher.UIThread.RunJobs();
        first.Close();

        var second = Open();

        Assert.Equal(WindowState.Maximized, second.WindowState);
        second.Close();
    }

    [AvaloniaFact]
    public void WithNothingSaved_TheWindowIsLeftAsItWas()
    {
        var window = new Window { Width = 321, Height = 123 };

        RememberedWindowPlacement.Follow(window, _settings);

        Assert.Equal(321, window.Width);
        Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);
    }

    private Window Open()
    {
        var window = new Window();
        RememberedWindowPlacement.Follow(window, _settings);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
