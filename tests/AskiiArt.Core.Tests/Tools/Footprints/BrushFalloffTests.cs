// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.Core.Tests.Tools.Footprints;

public sealed class BrushFalloffTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void AHardTip_PaintsEveryCellFully()
    {
        var falloff = BrushFalloff.Create(Footprint(5, BrushShape.Circle), softness: 0);

        Assert.All(Cells(falloff.Footprint), cell => Assert.Equal(1, falloff.WeightAt(cell.Column, cell.Row)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void AOneCellTip_PaintsFully_HoweverSoft(double softness)
    {
        Assert.Equal(1, BrushFalloff.Create(Footprint(1, BrushShape.Square), softness).WeightAt(0, 0));
    }

    [Fact]
    public void AFullySoftTip_FadesFromTheMiddle()
    {
        var falloff = BrushFalloff.Create(Footprint(7, BrushShape.Square), softness: 1);

        var row = Enumerable.Range(0, 7).Select(column => falloff.WeightAt(column, 3)).ToArray();

        Assert.Equal(1, row[3]);
        Assert.True(row[0] < row[1] && row[1] < row[2] && row[2] < row[3]);
        Assert.True(row[0] > 0);
        Assert.Equal(row[0], row[6], Tolerance);
    }

    [Fact]
    public void AHalfSoftTip_PaintsTheInnerHalfFully()
    {
        var falloff = BrushFalloff.Create(Footprint(7, BrushShape.Square), softness: 0.5);

        Assert.Equal(1, falloff.WeightAt(2, 3));
        Assert.True(falloff.WeightAt(1, 3) < 1);
    }

    [Fact]
    public void ASquare_FadesEvenlyTowardsItsSides()
    {
        var falloff = BrushFalloff.Create(Footprint(5, BrushShape.Square), softness: 1);

        Assert.Equal(falloff.WeightAt(0, 2), falloff.WeightAt(0, 0), Tolerance);
        Assert.Equal(falloff.WeightAt(1, 2), falloff.WeightAt(1, 1), Tolerance);
    }

    [Fact]
    public void ACircle_FadesRound()
    {
        var falloff = BrushFalloff.Create(Footprint(9, BrushShape.Circle), softness: 1);

        // The same number of cells from the middle, but further away on the diagonal.
        Assert.True(falloff.WeightAt(2, 2) < falloff.WeightAt(2, 4));
        Assert.Equal(falloff.WeightAt(2, 4), falloff.WeightAt(4, 2), Tolerance);
    }

    [Fact]
    public void APerceptualCircle_FadesRoundOnScreen()
    {
        // Cells a third as wide as they're tall: 9 cells wide and 3 tall looks round.
        var footprint = BrushFootprint.Create(new BrushTip(9, BrushShape.Circle, BrushProportions.Perceptual), 1 / 3.0);
        var falloff = BrushFalloff.Create(footprint, softness: 1);

        // Three cells across from the middle is as far on screen as one cell up, and one cell across is nearer.
        Assert.Equal(3, footprint.Height);
        Assert.Equal(falloff.WeightAt(4, 0), falloff.WeightAt(1, 1), Tolerance);
        Assert.True(falloff.WeightAt(3, 1) > falloff.WeightAt(4, 0));
    }

    [Fact]
    public void ACellTheFootprintDoesntCover_WeighsNothing()
    {
        var falloff = BrushFalloff.Create(Footprint(5, BrushShape.Circle), softness: 0);

        Assert.Equal(0, falloff.WeightAt(0, 0));
        Assert.Equal(0, falloff.WeightAt(-1, 2));
        Assert.Equal(0, falloff.WeightAt(2, 5));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void ASoftnessOutsideZeroToOne_IsRejected(double softness)
    {
        var footprint = Footprint(3, BrushShape.Square);

        Assert.Throws<ArgumentOutOfRangeException>(() => BrushFalloff.Create(footprint, softness));
    }

    [Fact]
    public void AHardSquare_PaintsEveryCellEvenly()
    {
        var falloff = BrushFalloff.Create(Footprint(5, BrushShape.Square), softness: 0);

        Assert.False(falloff.HasShapedCells);
        Assert.All(Cells(falloff.Footprint), cell => Assert.True(falloff.PartWeightsAt(cell.Column, cell.Row).IsEmpty));
    }

    // The circle's edge passes through the top of its top middle cell, so the parts above it get nothing.
    [Fact]
    public void AHardCircle_ShapesItsEdgeCells_TowardsTheMiddle()
    {
        var falloff = BrushFalloff.Create(Footprint(9, BrushShape.Circle), softness: 0);

        var parts = falloff.PartWeightsAt(4, 0);

        Assert.True(falloff.HasShapedCells);
        Assert.Equal(GlyphShape.PartCount, parts.Length);
        Assert.Equal(0, PartAt(parts, 1, 0));
        Assert.Equal(1, PartAt(parts, 1, GlyphShape.Rows - 1));
        Assert.True(falloff.PartWeightsAt(4, 4).IsEmpty);
    }

    [Fact]
    public void ASoftTip_ShapesTheCellsItsFadeChangesAcross()
    {
        var falloff = BrushFalloff.Create(Footprint(7, BrushShape.Square), softness: 1);

        // The left side's middle cell, fading towards the tip's left edge.
        var parts = falloff.PartWeightsAt(0, 3);

        Assert.True(PartAt(parts, 0, 4) < PartAt(parts, GlyphShape.Columns - 1, 4));
        Assert.Equal(PartAt(parts, 0, 3), PartAt(parts, 0, 4), Tolerance);
    }

    // Each of these tips' cells is on its edge: none has all four neighbours in the footprint.
    [Theory]
    [InlineData(1, BrushShape.Square)]
    [InlineData(2, BrushShape.Square)]
    [InlineData(2, BrushShape.Circle)]
    public void ATipWithNoMiddleCell_PaintsEveryCellEvenly(int size, BrushShape shape)
    {
        var falloff = BrushFalloff.Create(Footprint(size, shape), softness: 1);

        Assert.False(falloff.HasShapedCells);
    }

    [Fact]
    public void TheCellsTheMiddleTouches_PaintEvenly_SoTheMiddlePaintsFully()
    {
        var odd = BrushFalloff.Create(Footprint(3, BrushShape.Square), softness: 1);
        var even = BrushFalloff.Create(Footprint(4, BrushShape.Square), softness: 1);

        Assert.True(odd.PartWeightsAt(1, 1).IsEmpty);
        Assert.Equal(1, odd.WeightAt(1, 1));
        Assert.False(odd.PartWeightsAt(0, 1).IsEmpty);
        Assert.All([(1, 1), (2, 1), (1, 2), (2, 2)],
            cell => Assert.True(even.PartWeightsAt(cell.Item1, cell.Item2).IsEmpty));
        Assert.False(even.PartWeightsAt(0, 1).IsEmpty);
    }

    [Fact]
    public void ACellsPeakWeight_IsItsHeaviestPart_OrItsWeightWhenEven()
    {
        var falloff = BrushFalloff.Create(Footprint(7, BrushShape.Square), softness: 1);

        Assert.Equal(falloff.PartWeightsAt(0, 3).ToArray().Max(), falloff.PeakWeightAt(0, 3));
        Assert.Equal((float)falloff.WeightAt(3, 3), falloff.PeakWeightAt(3, 3));
        Assert.Equal(0, falloff.PeakWeightAt(-1, 3));
    }

    [Fact]
    public void ACellTheFootprintDoesntCover_HasNoParts()
    {
        var falloff = BrushFalloff.Create(Footprint(9, BrushShape.Circle), softness: 1);

        Assert.True(falloff.PartWeightsAt(0, 0).IsEmpty);
        Assert.True(falloff.PartWeightsAt(9, 4).IsEmpty);
    }

    private static float PartAt(ReadOnlySpan<float> parts, int column, int row) =>
        parts[row * GlyphShape.Columns + column];

    private static BrushFootprint Footprint(int size, BrushShape shape) =>
        BrushFootprint.Create(new BrushTip(size, shape, BrushProportions.Mathematical), 1);

    private static IEnumerable<(int Column, int Row)> Cells(BrushFootprint footprint) =>
        footprint.Rows.SelectMany((cells, row) =>
            Enumerable.Range(cells.Start, cells.Length).Select(column => (column, row)));
}
