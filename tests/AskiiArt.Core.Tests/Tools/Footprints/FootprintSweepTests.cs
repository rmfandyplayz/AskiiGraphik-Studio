// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.Core.Tests.Tools.Footprints;

public sealed class FootprintSweepTests
{
    private static readonly CanvasSize _canvas = new(10, 5);

    [Fact]
    public void AStamp_CoversTheFootprint_RowByRow()
    {
        var cells = FootprintSweep.Cover(Square(2), new CellPoint(3, 1), new CellPoint(3, 1), _canvas);

        Assert.Equal([new(3, 1), new(4, 1), new(3, 2), new(4, 2)], cells);
    }

    [Fact]
    public void ASweep_CoversEveryCellOnce()
    {
        var cells = FootprintSweep.Cover(Square(3), new CellPoint(0, 0), new CellPoint(3, 0), _canvas);

        Assert.Equal(18, cells.Count);
        Assert.Equal(cells.Count, cells.Distinct().Count());
        Assert.All(cells, cell => Assert.InRange(cell.X, 0, 5));
        Assert.All(cells, cell => Assert.InRange(cell.Y, 0, 2));
    }

    [Fact]
    public void ADiagonalSweep_LeavesNoGaps()
    {
        var cells = FootprintSweep.Cover(Square(1), new CellPoint(0, 0), new CellPoint(2, 2), _canvas);

        Assert.Equal([new(0, 0), new(1, 1), new(2, 2)], cells);
    }

    [Fact]
    public void ASweep_IsClippedToTheCanvas()
    {
        var cells = FootprintSweep.Cover(Square(3), new CellPoint(-1, -1), new CellPoint(-1, -1), _canvas);

        Assert.Equal([new(0, 0), new(1, 0), new(0, 1), new(1, 1)], cells);
    }

    [Fact]
    public void ASweepOffTheCanvas_CoversNothing()
    {
        Assert.Empty(FootprintSweep.Cover(Square(3), new CellPoint(20, 0), new CellPoint(30, 9), _canvas));
        Assert.Empty(FootprintSweep.Cover(Square(3), new CellPoint(-5, -5), new CellPoint(-5, -5), _canvas));
    }

    [Fact]
    public void ACircle_LeavesItsCornersAlone()
    {
        var circle = BrushFootprint.Create(new BrushTip(3, BrushShape.Circle, BrushProportions.Mathematical), 1);

        var cells = FootprintSweep.Cover(circle, new CellPoint(0, 0), new CellPoint(0, 0), _canvas);

        Assert.Equal([new(1, 0), new(0, 1), new(1, 1), new(2, 1), new(1, 2)], cells);
    }

    private static BrushFootprint Square(int size) =>
        BrushFootprint.Create(new BrushTip(size, BrushShape.Square, BrushProportions.Mathematical), 1);
}
