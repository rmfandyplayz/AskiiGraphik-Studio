// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Selecting;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class SelectionMotionTests
{
    private const double DashSpeed = 12;
    private const int DashLength = 4;

    private static readonly CanvasSize _size = new(4, 4);

    private static readonly SelectionLook _look = new(SKColors.Black, SKColors.White, SKColors.Red, SKColors.DarkRed,
        SKColors.Blue, SKColors.DarkBlue, SKColors.Red, SKColors.DarkRed, SKColors.Blue, SKColors.DarkBlue,
        HatchBandWidth: 40, HatchSpeed: 0.6, SubtractHatchSpeed: 1,
        IntersectHatchSpeed: 0.6, HatchStepsPerPixel: 1, FaintOpacity: 0.3, VanishingOpacity: 0.1,
        LostHatchOpacity: 0.7, GridUnderHatchOpacity: 0.4, GridUnderLostHatchOpacity: 0.7, ClosingLineOpacity: 0.5,
        CornerMarkerRadius: 3, FinishMarkerRadius: 8, CornerMarkerOpacity: 0.9);

    [Fact]
    public void TheDashes_RunAtTheirSpeed_WithinADashAndAGap()
    {
        Assert.Equal(6, At(Seconds(0.5), display: null).DashRun);
        Assert.Equal(4, At(Seconds(1), display: null).DashRun);
    }

    [Fact]
    public void TheHatchingOverCellsNotSelected_MovesAcrossItsLinesAtItsShareOfTheDashSpeed()
    {
        // 0.6 of 12 pixels a second across lines at 45 degrees is 0.6 × 12 × √2 ≈ 10.18 pixels a second along a row.
        var motion = SelectionMotion.At(Seconds(9), Seconds(2), display: null, _look, DashSpeed, DashLength);

        Assert.Equal(20, motion.UnselectedHatchShift);
    }

    [Fact]
    public void TheHatching_MovesInStepsOfAPixel()
    {
        // 0.6 × 12 × √2 × 2 seconds ≈ 20.36 pixels, rounded down to a quarter of a pixel.
        var motion = SelectionMotion.At(Seconds(9), Seconds(2), display: null, _look with { HatchStepsPerPixel = 4 },
            DashSpeed, DashLength);

        Assert.Equal(20.25, motion.UnselectedHatchShift);
    }

    [Fact]
    public void TheHatchingsShift_StaysWithinOneTile()
    {
        var motion = SelectionMotion.At(Seconds(9), Seconds(2), display: null, _look with { HatchBandWidth = 3 },
            DashSpeed, DashLength);

        Assert.Equal(20 % 8, motion.UnselectedHatchShift);
    }

    [Theory]
    [InlineData(SelectionCombineMode.Subtract, 16)]
    [InlineData(SelectionCombineMode.Intersect, 10)]
    public void TheHatchingOverCellsAboutToBeDeselected_MovesAtItsModesSpeed(SelectionCombineMode mode, double shift)
    {
        var before = SelectionMask.Rectangle(_size, new CellRect(0, 0, 2, 2));
        var preview = new SelectionPreview(before, mode, SelectionMask.Rectangle(_size, new CellRect(1, 0, 3, 3)));

        var motion = At(Seconds(1), SelectionDisplay.For(before, preview));

        Assert.Equal(shift, motion.LostHatchShift);
    }

    [Fact]
    public void WithNoCellsAboutToBeDeselected_ThatHatchingDoesntMove()
    {
        var before = SelectionMask.Rectangle(_size, new CellRect(0, 0, 2, 2));

        Assert.Equal(0, At(Seconds(1), SelectionDisplay.For(before, null)).LostHatchShift);
    }

    private static SelectionMotion At(TimeSpan time, SelectionDisplay? display) =>
        SelectionMotion.At(time, time, display, _look, DashSpeed, DashLength);

    private static TimeSpan Seconds(double seconds) => TimeSpan.FromSeconds(seconds);
}
