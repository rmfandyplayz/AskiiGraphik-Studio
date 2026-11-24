// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Tests.Canvas;

public sealed class PasteFitTests
{
    private static readonly PixelSize _usable = new(400, 300);

    // Cells of 10 x 20 pixels at 100%, rounded down at other zooms, as a font's metrics round them.
    private static PixelSize CellSizeAt(double factor) =>
        new(Math.Max(1, (int)(10 * factor)), Math.Max(1, (int)(20 * factor)));

    [Fact]
    public void ASmallPaste_ZoomsInNoFurtherThan150Percent()
    {
        var factor = PasteFit.ZoomFor(new CellRect(0, 0, 2, 1), _usable, room: 10, CellSizeAt);

        Assert.Equal(PasteFit.LargestZoom, factor);
    }

    // At 125%, cells are 12 x 25: 30 x 10 cells take 360 x 250 pixels, plus 10 on each side, inside 400 x 300. At 150%
    // they'd take 450 pixels across.
    [Fact]
    public void ThePaste_SnapsToTheLargestStepAtWhichItFits()
    {
        var factor = PasteFit.ZoomFor(new CellRect(5, 5, 30, 10), _usable, room: 10, CellSizeAt);

        Assert.Equal(1.25, factor);
    }

    // With 30 on each side, 125% would need 420 pixels across, so 100% is the largest step that fits.
    [Fact]
    public void TheRoomRoundThePaste_CountsTowardsWhetherItFits()
    {
        var factor = PasteFit.ZoomFor(new CellRect(5, 5, 30, 10), _usable, room: 30, CellSizeAt);

        Assert.Equal(1, factor);
    }

    [Fact]
    public void APasteTooBigForEveryStep_ZoomsOutTo25Percent()
    {
        var factor = PasteFit.ZoomFor(new CellRect(0, 0, 5000, 10), _usable, room: 10, CellSizeAt);

        Assert.Equal(ZoomLevels.Minimum, factor);
    }

    // Tall pastes are held back by their height, not their width.
    [Fact]
    public void ATallPaste_FitsByItsHeight()
    {
        // At 50%, cells are 10 high, so 40 rows take 400 pixels; at 1/3 they're 6 high and take 240.
        var factor = PasteFit.ZoomFor(new CellRect(0, 0, 2, 40), _usable, room: 10, CellSizeAt);

        Assert.Equal(1.0 / 3, factor, 1e-9);
    }
}
