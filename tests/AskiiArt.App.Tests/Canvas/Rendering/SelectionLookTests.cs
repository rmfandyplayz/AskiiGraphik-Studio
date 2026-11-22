// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class SelectionLookTests
{
    private static readonly SelectionLook _look = new(SKColors.Black, SKColors.White, SKColors.Red, SKColors.DarkRed,
        SKColors.Blue, SKColors.DarkBlue, SKColors.Red, SKColors.DarkRed, SKColors.Blue, SKColors.DarkBlue,
        HatchBandWidth: 3, HatchSpeed: 0.6, SubtractHatchSpeed: 1,
        IntersectHatchSpeed: 0.4, HatchStepsPerPixel: 4, FaintOpacity: 0.3, VanishingOpacity: 0.1,
        LostHatchOpacity: 0.7, GridUnderHatchOpacity: 0.4, GridUnderLostHatchOpacity: 0.7, ClosingLineOpacity: 0.5,
        CornerMarkerRadius: 3, FinishMarkerRadius: 8, CornerMarkerOpacity: 0.9);

    [AvaloniaFact]
    public void TheThemeTokens_GiveALookThatHatchesAndHasMarkers_InBothThemes()
    {
        foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var look = SelectionLook.FromResources(Application.Current!, theme);

            Assert.True(look.HatchBandWidth > 0);
            Assert.True(look.FinishMarkerRadius > look.CornerMarkerRadius);
            Assert.InRange(look.FaintOpacity, 0.01, 0.99);
            Assert.InRange(look.VanishingOpacity, 0.01, look.FaintOpacity);
            Assert.InRange(look.OffCanvasOpacity, 0.01, 0.99);
            Assert.InRange(look.DroppedOffCanvasOpacity, 0.01, look.OffCanvasOpacity);
            Assert.InRange(look.ClosingLineOpacity, 0.01, 0.99);
            Assert.InRange(look.GridUnderHatchOpacity, 0, 1);
            Assert.InRange(look.GridUnderLostHatchOpacity, 0, 1);
            Assert.True(look.HatchSpeed > 0);
            Assert.True(look.SubtractHatchSpeed > 0);
            Assert.True(look.IntersectHatchSpeed > 0);
            Assert.True(look.HatchSteps > 1);
            Assert.NotEqual(look.SubtractLine, look.IntersectLine);
        }
    }

    [Fact]
    public void Scaling_ChangesTheLengths_ButNotHowStronglyThingsShow()
    {
        var scaled = _look.ScaledBy(2);

        var expected = _look with
        {
            HatchBandWidth = 6,
            CornerMarkerRadius = 6,
            FinishMarkerRadius = 16
        };
        Assert.Equal(expected, scaled);
    }

    [Fact]
    public void TheHatchingTile_HoldsADarkAndALightStripe_AlongARow_InWholePixels_AtLeastTwo()
    {
        // Two stripes 3 pixels wide at 45° take 2 × 3 × √2 ≈ 8.49 pixels along a row.
        Assert.Equal(8, _look.HatchTilePeriod);
        Assert.Equal(34, (_look with { HatchBandWidth = 12 }).HatchTilePeriod);
        Assert.Equal(2, (_look with { HatchBandWidth = 0.2 }).HatchTilePeriod);
    }

    [Fact]
    public void TheHatchingOverCellsAboutToBeDeselected_HasItsOwnColours_NotTheDashes()
    {
        var look = _look with { SubtractHatchLine = SKColors.Orange, IntersectHatchBackdrop = SKColors.Pink };

        Assert.Equal(SKColors.Orange, look.HatchColoursOf(SelectionLineStyle.Subtract).Line);
        Assert.Equal(SKColors.Pink, look.HatchColoursOf(SelectionLineStyle.Intersect).Backdrop);
        Assert.Equal(SKColors.Red, look.ColoursOf(SelectionLineStyle.Subtract, default!).Line);
    }

    [Fact]
    public void TheHatchingsSteps_AreAWholeNumberAtLeastOne()
    {
        Assert.Equal(4, _look.HatchSteps);
        Assert.Equal(3, (_look with { HatchStepsPerPixel = 2.6 }).HatchSteps);
        Assert.Equal(1, (_look with { HatchStepsPerPixel = 0 }).HatchSteps);
    }

    [Fact]
    public void TheHatchingOverCellsAboutToBeDeselected_MovesAtItsModesSpeed()
    {
        Assert.Equal(1, _look.LostHatchSpeedOf(SelectionLineStyle.Subtract));
        Assert.Equal(0.4, _look.LostHatchSpeedOf(SelectionLineStyle.Intersect));
        Assert.Equal(0.6, _look.LostHatchSpeedOf(SelectionLineStyle.Plain));
    }

    [Fact]
    public void Fading_ScalesTheAlpha_AndKeepsTheColour()
    {
        Assert.Equal(new SKColor(10, 20, 30, 100), SelectionLook.Faded(new SKColor(10, 20, 30, 200), 0.5));
        Assert.Equal(new SKColor(10, 20, 30, 0), SelectionLook.Faded(new SKColor(10, 20, 30, 200), -1));
    }
}
