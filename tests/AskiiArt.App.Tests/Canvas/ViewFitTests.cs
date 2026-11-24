// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Tests.Canvas;

public sealed class ViewFitTests
{
    private static readonly PixelSize _usable = new(400, 300);

    // Cells of 10 x 20 pixels at 100%, rounded down at other zooms, as a font's metrics round them.
    private static PixelSize CellSizeAt(double factor) =>
        new(Math.Max(1, (int)(10 * factor)), Math.Max(1, (int)(20 * factor)));

    [Theory]
    [InlineData(8)]
    [InlineData(1)]
    [InlineData(0.5)]
    public void CellsThatFitAtTheCurrentZoom_KeepIt(double currentZoom)
    {
        var factor = ViewFit.ZoomFor(new CellRect(0, 0, 2, 1), currentZoom, _usable, room: 10, CellSizeAt);

        Assert.Equal(currentZoom, factor);
    }

    // At 125%, cells are 12 x 25: 30 x 10 cells take 360 x 250 pixels, plus 10 on each side, inside 400 x 300. At 150%
    // they'd take 450 pixels across.
    [Fact]
    public void CellsTooBigForTheCurrentZoom_ZoomOutToTheLargestStepAtWhichTheyFit()
    {
        var factor = ViewFit.ZoomFor(new CellRect(5, 5, 30, 10), 8, _usable, room: 10, CellSizeAt);

        Assert.Equal(1.25, factor);
    }

    // With 30 on each side, 125% would need 420 pixels across, so 100% is the largest step that fits.
    [Fact]
    public void TheRoomRoundTheCells_CountsTowardsWhetherTheyFit()
    {
        var factor = ViewFit.ZoomFor(new CellRect(5, 5, 30, 10), 8, _usable, room: 30, CellSizeAt);

        Assert.Equal(1, factor);
    }

    [Fact]
    public void CellsTooBigForEveryStep_ZoomOutTo25Percent()
    {
        var factor = ViewFit.ZoomFor(new CellRect(0, 0, 5000, 10), 1, _usable, room: 10, CellSizeAt);

        Assert.Equal(ZoomLevels.Minimum, factor);
    }

    [Fact]
    public void TallCells_FitByTheirHeight()
    {
        // At 50%, cells are 10 high, so 40 rows take 400 pixels; at 1/3 they're 6 high and take 240.
        var factor = ViewFit.ZoomFor(new CellRect(0, 0, 2, 40), 1, _usable, room: 10, CellSizeAt);

        Assert.Equal(1.0 / 3, factor, 1e-9);
    }
}
