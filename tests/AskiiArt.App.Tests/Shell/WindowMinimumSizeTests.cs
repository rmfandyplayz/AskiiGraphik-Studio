// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell;
using Avalonia;

namespace AskiiArt.App.Tests.Shell;

public sealed class WindowMinimumSizeTests
{
    private static readonly Size _wanted = new(1280, 720);
    private static readonly Size _frame = new(0, 31);

    [Fact]
    public void WhenTheScreenHasRoom_TheWantedSizeApplies()
    {
        var minimum = WindowMinimumSize.Fit(_wanted, new PixelSize(1920, 1040), scaling: 1, _frame);

        Assert.Equal(_wanted, minimum);
    }

    [Fact]
    public void A1080pScreenAt150Percent_LowersTheHeight_SoTheWindowStaysOnScreen()
    {
        var minimum = WindowMinimumSize.Fit(_wanted, new PixelSize(1920, 1008), scaling: 1.5, _frame);

        Assert.Equal(1280, minimum.Width);
        Assert.Equal(1008 / 1.5 - 31, minimum.Height);
    }

    [Fact]
    public void ASmallScreen_LowersBoth()
    {
        var minimum = WindowMinimumSize.Fit(_wanted, new PixelSize(1024, 600), scaling: 1, _frame);

        Assert.Equal(new Size(1024, 600 - 31), minimum);
    }
}
