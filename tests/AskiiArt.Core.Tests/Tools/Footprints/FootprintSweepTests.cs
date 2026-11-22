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

    // A tip one row tall has no middle cell for its edge to be around, so every cell paints evenly.
    [Fact]
    public void AWeightedStamp_GivesEachCellItsWeight()
    {
        var falloff = BrushFalloff.Create(OneRow(3), softness: 1);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(0, 0), new CellPoint(0, 0), _canvas);

        Assert.Equal(3, cells.Count);
        Assert.Equal(1, CellAt(cells, 1, 0).Weight);
        Assert.Equal(falloff.WeightAt(0, 0), CellAt(cells, 0, 0).Weight);
        Assert.All(cells, cell => Assert.Null(cell.PartWeights));
    }

    [Fact]
    public void AWeightedSweep_KeepsEachCellsGreatestWeight()
    {
        var falloff = BrushFalloff.Create(OneRow(3), softness: 1);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(0, 0), new CellPoint(2, 0), _canvas);

        // The middle of the first stamp and the middle of the last.
        Assert.Equal(1, CellAt(cells, 1, 0).Weight);
        Assert.Equal(1, CellAt(cells, 3, 0).Weight);
        Assert.Equal(cells.Count, cells.Select(cell => cell.Point).Distinct().Count());
        Assert.Equal(5, cells.Count);
    }

    [Fact]
    public void AShapedStamp_GivesAnEdgeCellItsPartWeights_AndTheLightestAsTheWholeCellsWeight()
    {
        var falloff = BrushFalloff.Create(Square(3), softness: 1);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(0, 0), new CellPoint(0, 0), _canvas);

        var corner = CellAt(cells, 0, 0);
        var stampParts = falloff.PartWeightsAt(0, 0).ToArray();
        Assert.Equal(stampParts, corner.PartWeights);
        Assert.Equal(stampParts.Min(), corner.Weight);
        Assert.Equal(new WeightedCell(new CellPoint(1, 1), 1, null), CellAt(cells, 1, 1));
    }

    [Fact]
    public void OverlappingShapedStamps_KeepEachPartsGreatestWeight()
    {
        var falloff = BrushFalloff.Create(Square(3), softness: 1);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(0, 0), new CellPoint(1, 0), _canvas);

        // The first stamp's top middle cell is the second stamp's top-left corner.
        var expected = falloff.PartWeightsAt(1, 0).ToArray().Zip(falloff.PartWeightsAt(0, 0).ToArray(), Math.Max);
        Assert.Equal(expected, CellAt(cells, 1, 0).PartWeights!);
    }

    [Fact]
    public void AStampThatPaintsACellFully_OutweighsOneThatShapesIt()
    {
        var falloff = BrushFalloff.Create(Square(3), softness: 1);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(0, 0), new CellPoint(1, 0), _canvas);

        // The first stamp's middle is the second stamp's left side.
        Assert.Equal(new WeightedCell(new CellPoint(1, 1), 1, null), CellAt(cells, 1, 1));
    }

    [Fact]
    public void AWeightedStampPartlyOffTheCanvas_KeepsOnlyTheCellsOnIt()
    {
        var falloff = BrushFalloff.Create(Square(3), softness: 0);

        var cells = FootprintSweep.CoverWeighted(falloff, new CellPoint(-1, -1), new CellPoint(-1, -1), _canvas);

        Assert.Equal([new(0, 0), new(1, 0), new(0, 1), new(1, 1)], cells.Select(cell => cell.Point));
    }

    private static BrushFootprint Square(int size) =>
        BrushFootprint.Create(new BrushTip(size, BrushShape.Square, BrushProportions.Mathematical), 1);

    // Cells three times as tall as they're wide make a perceptual square one row tall.
    private static BrushFootprint OneRow(int size) =>
        BrushFootprint.Create(new BrushTip(size, BrushShape.Square, BrushProportions.Perceptual), 1 / 3.0);

    private static WeightedCell CellAt(IEnumerable<WeightedCell> cells, int x, int y) =>
        cells.Single(cell => cell.Point == new CellPoint(x, y));
}
