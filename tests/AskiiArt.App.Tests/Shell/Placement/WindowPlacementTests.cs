// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Placement;
using Avalonia;

namespace AskiiArt.App.Tests.Shell.Placement;

public sealed class WindowPlacementTests
{
    private static readonly ScreenArea _left = new(new PixelRect(0, 0, 2560, 1400), 1, IsPrimary: true);
    private static readonly ScreenArea _right = new(new PixelRect(2560, 0, 3840, 2100), 1.5, IsPrimary: false);

    [Fact]
    public void APlacement_ReadsBack_AsItWasSaved()
    {
        var placement = new WindowPlacement(new PixelPoint(-7, 120), new Size(1234.5, 800), IsMaximized: true);

        Assert.Equal(placement, WindowPlacement.FromJson(placement.ToJson()));
    }

    [Fact]
    public void AFullScreenPlacement_ReadsBack_AsItWasSaved()
    {
        var placement = new WindowPlacement(new PixelPoint(0, 0), new Size(900, 600), IsMaximized: false,
            IsFullScreen: true);

        Assert.Equal(placement, WindowPlacement.FromJson(placement.ToJson()));
    }

    [Fact]
    public void APlacementSavedBeforeFullScreenWasKept_IsNotFullScreen()
    {
        var placement = WindowPlacement.FromJson("{\"x\":1,\"y\":2,\"width\":300,\"height\":200,\"maximized\":true}");

        Assert.Equal(new WindowPlacement(new PixelPoint(1, 2), new Size(300, 200), IsMaximized: true), placement);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("{\"x\":1,\"y\":2,\"width\":300}")]
    [InlineData("{\"x\":1,\"y\":2,\"width\":0,\"height\":300,\"maximized\":false}")]
    [InlineData("{\"x\":\"left\",\"y\":2,\"width\":300,\"height\":300,\"maximized\":false}")]
    public void ABrokenPlacement_IsNone(string json) => Assert.Null(WindowPlacement.FromJson(json));

    [Fact]
    public void OnTheSecondScreen_ItStaysThere()
    {
        var placement = new WindowPlacement(new PixelPoint(3000, 100), new Size(1000, 700), IsMaximized: false);

        Assert.Equal(placement, placement.OnScreens([_left, _right]));
    }

    [Fact]
    public void BiggerThanItsScreen_ItShrinksToFit_InThatScreensPixels()
    {
        var placement = new WindowPlacement(new PixelPoint(2600, 0), new Size(4000, 4000), IsMaximized: false);

        Assert.Equal(new Size(2560, 1400), placement.OnScreens([_left, _right])?.Size);
    }

    [Fact]
    public void WhenItsScreenHasGone_ItHasNoPlace()
    {
        var placement = new WindowPlacement(new PixelPoint(3000, 100), new Size(1000, 700), IsMaximized: false);

        Assert.Null(placement.OnScreens([_left]));
    }

    [Fact]
    public void WithItsTitleBarAboveTheScreen_ItHasNoPlace()
    {
        var placement = new WindowPlacement(new PixelPoint(100, -200), new Size(1000, 700), IsMaximized: false);

        Assert.Null(placement.OnScreens([_left, _right]));
    }

    [Fact]
    public void SizeWithin_KeepsAWindowThatFits()
    {
        var placement = new WindowPlacement(new PixelPoint(0, 0), new Size(800, 600), IsMaximized: false);

        Assert.Equal(new Size(800, 600), placement.SizeWithin(_left));
    }

    [Fact]
    public void MovedOntoTheScreenItIsOn_ItStaysPut()
    {
        var placement = new WindowPlacement(new PixelPoint(3000, 100), new Size(1000, 700), IsMaximized: true);

        Assert.Equal(placement, placement.MovedOnto(_right, [_left, _right]));
    }

    [Fact]
    public void MovedOntoAnotherScreen_ItKeepsItsDistanceFromTheScreensCorner()
    {
        // As when a window maximized on the left screen is sent to the right one with Win+Shift+Right.
        var placement = new WindowPlacement(new PixelPoint(200, 100), new Size(1000, 700), IsMaximized: true);

        Assert.Equal(placement with { Position = new PixelPoint(2760, 100) },
            placement.MovedOnto(_right, [_left, _right]));
    }

    [Fact]
    public void MovedOntoAScreenTooSmallForThatDistance_ItGoesInTheMiddle()
    {
        var placement = new WindowPlacement(new PixelPoint(2560 + 3000, 1500), new Size(600, 400), IsMaximized: true);

        Assert.Equal(new PixelPoint(980, 500), placement.MovedOnto(_left, [_left, _right]).Position);
    }

    [Fact]
    public void MovedOntoASmallerScreen_ItShrinksToFit()
    {
        var placement = new WindowPlacement(new PixelPoint(2600, 0), new Size(2400, 1500), IsMaximized: true);

        Assert.Equal(new Size(2400, 1400), placement.MovedOnto(_left, [_left, _right]).Size);
    }

    [Fact]
    public void MovedFromNoScreen_ItGoesInTheMiddle()
    {
        var placement = new WindowPlacement(new PixelPoint(-5000, -5000), new Size(1000, 600), IsMaximized: true);

        Assert.Equal(new PixelPoint(780, 400), placement.MovedOnto(_left, [_left, _right]).Position);
    }
}
