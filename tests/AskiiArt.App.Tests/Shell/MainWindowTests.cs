// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Shell;

public sealed class MainWindowTests
{
    [AvaloniaFact]
    public void MainWindow_CanBeShown()
    {
        var window = new MainWindow();

        window.Show();

        Assert.True(window.IsVisible);
    }
}
