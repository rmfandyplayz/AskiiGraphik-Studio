// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Footprints;

namespace AskiiArt.Core.Tests.Tools.Footprints;

public sealed class BrushFootprintTests
{
    [Fact]
    public void AMathematicalSquare_IsItsSizeEachWay()
    {
        var footprint = Mathematical(10, BrushShape.Square);

        Assert.Equal(10, footprint.Width);
        Assert.Equal(10, footprint.Height);
        Assert.Equal(100, footprint.CellCount);
    }

    [Fact]
    public void SmallCircles_LookRound()
    {
        Assert.Equal(["#"], Draw(Mathematical(1, BrushShape.Circle)));
        Assert.Equal(["##", "##"], Draw(Mathematical(2, BrushShape.Circle)));
        Assert.Equal([".#.", "###", ".#."], Draw(Mathematical(3, BrushShape.Circle)));
        Assert.Equal([".##.", "####", "####", ".##."], Draw(Mathematical(4, BrushShape.Circle)));
        Assert.Equal([".###.", "#####", "#####", "#####", ".###."], Draw(Mathematical(5, BrushShape.Circle)));
        Assert.Equal(
            ["..##..", ".####.", "######", "######", ".####.", "..##.."],
            Draw(Mathematical(6, BrushShape.Circle)));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(10, 5)]
    [InlineData(100, 50)]
    public void APerceptualTip_IsFewerCellsTall_InCellsTwiceAsTallAsWide(int size, int height)
    {
        var square = BrushFootprint.Create(new BrushTip(size, BrushShape.Square, BrushProportions.Perceptual), 0.5);
        var circle = BrushFootprint.Create(new BrushTip(size, BrushShape.Circle, BrushProportions.Perceptual), 0.5);

        Assert.Equal((size, height), (square.Width, square.Height));
        Assert.Equal((size, height), (circle.Width, circle.Height));
    }

    [Fact]
    public void APerceptualCircle_IsAnEllipseOfCells()
    {
        var footprint = BrushFootprint.Create(new BrushTip(6, BrushShape.Circle, BrushProportions.Perceptual), 0.5);

        Assert.Equal([".####.", "######", ".####."], Draw(footprint));
    }

    [Fact]
    public void APerceptualTip_StaysWithinTheSizeLimit_InWideCells()
    {
        var footprint = BrushFootprint.Create(new BrushTip(100, BrushShape.Square, BrushProportions.Perceptual), 2);

        Assert.Equal(100, footprint.Height);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void ACellShapeThatIsntAboveZero_IsRejected(double cellWidthToHeight)
    {
        var tip = new BrushTip(3, BrushShape.Square, BrushProportions.Perceptual);

        Assert.Throws<ArgumentOutOfRangeException>(() => BrushFootprint.Create(tip, cellWidthToHeight));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void ASizeOutsideTheRange_IsRejected(int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BrushTip(size, BrushShape.Square, BrushProportions.Mathematical));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.99, 0.99)]
    public void AnOddSize_IsCentredOnThePointersCell(double subCellX, double subCellY)
    {
        Assert.Equal(new CellPoint(5, 5), Mathematical(1, BrushShape.Square).TopLeftAt(new CellPoint(5, 5), subCellX,
            subCellY));
        Assert.Equal(new CellPoint(4, 4), Mathematical(3, BrushShape.Square).TopLeftAt(new CellPoint(5, 5), subCellX,
            subCellY));
    }

    [Fact]
    public void AnEvenSize_LeansTowardsTheHalfOfTheCellThePointerIsIn()
    {
        var footprint = Mathematical(4, BrushShape.Square);

        Assert.Equal(new CellPoint(3, 3), footprint.TopLeftAt(new CellPoint(5, 5), 0.25, 0.25));
        Assert.Equal(new CellPoint(4, 4), footprint.TopLeftAt(new CellPoint(5, 5), 0.75, 0.75));
        Assert.Equal(new CellPoint(4, 3),
            footprint.TopLeftAt(new ToolPointerInput(new CellPoint(5, 5), 0.75, 0.25, ToolModifiers.None)));
    }

    [Fact]
    public void ASquaresOutline_IsItsFourSides()
    {
        var footprint = Mathematical(3, BrushShape.Square);

        Assert.Equal(
            [
                new FootprintEdge(FootprintEdgeSide.Top, 0, 0, 3),
                new FootprintEdge(FootprintEdgeSide.Bottom, 0, 2, 3),
                new FootprintEdge(FootprintEdgeSide.Left, 0, 0, 3),
                new FootprintEdge(FootprintEdgeSide.Right, 2, 0, 3)
            ],
            Sorted(footprint.Edges));
    }

    [Fact]
    public void APlusShapedCircle_HasTwelveSidesOfOneCell()
    {
        var footprint = Mathematical(3, BrushShape.Circle);

        Assert.Equal(12, footprint.Edges.Count);
        Assert.All(footprint.Edges, edge => Assert.Equal(1, edge.Length));
    }

    [Theory]
    [InlineData(BrushShape.Square, BrushProportions.Mathematical)]
    [InlineData(BrushShape.Square, BrushProportions.Perceptual)]
    [InlineData(BrushShape.Circle, BrushProportions.Mathematical)]
    [InlineData(BrushShape.Circle, BrushProportions.Perceptual)]
    public void TheOutline_RunsAlongEveryUncoveredSide_AndNowhereElse(BrushShape shape, BrushProportions proportions)
    {
        for (var size = BrushTip.MinimumSize; size <= 24; size++)
        {
            var footprint = BrushFootprint.Create(new BrushTip(size, shape, proportions), 0.5);

            var expected = new HashSet<(FootprintEdgeSide, int, int)>();
            for (var row = 0; row < footprint.Height; row++)
            {
                for (var column = 0; column < footprint.Width; column++)
                {
                    if (!footprint.Contains(column, row))
                        continue;

                    if (!footprint.Contains(column, row - 1))
                        expected.Add((FootprintEdgeSide.Top, column, row));
                    if (!footprint.Contains(column, row + 1))
                        expected.Add((FootprintEdgeSide.Bottom, column, row));
                    if (!footprint.Contains(column - 1, row))
                        expected.Add((FootprintEdgeSide.Left, column, row));
                    if (!footprint.Contains(column + 1, row))
                        expected.Add((FootprintEdgeSide.Right, column, row));
                }
            }

            Assert.Equal(expected.OrderBy(side => side), Unrolled(footprint.Edges).OrderBy(side => side));
        }
    }

    [Fact]
    public void EveryRow_IsOneRunOfCells_InsideTheWidth()
    {
        for (var size = BrushTip.MinimumSize; size <= BrushTip.MaximumSize; size++)
        {
            var footprint = Mathematical(size, BrushShape.Circle);

            Assert.All(footprint.Rows, row => Assert.InRange(row.Length, 1, size));
            Assert.All(footprint.Rows, row => Assert.InRange(row.End, 1, size));
            Assert.Equal(size, footprint.Rows.Max(row => row.Length));
        }
    }

    private static BrushFootprint Mathematical(int size, BrushShape shape) =>
        BrushFootprint.Create(new BrushTip(size, shape, BrushProportions.Mathematical), 1);

    private static string[] Draw(BrushFootprint footprint) =>
    [
        .. Enumerable.Range(0, footprint.Height).Select(row => new string(Enumerable.Range(0, footprint.Width)
            .Select(column => footprint.Contains(column, row) ? '#' : '.').ToArray()))
    ];

    private static FootprintEdge[] Sorted(IEnumerable<FootprintEdge> edges) =>
        [.. edges.OrderBy(edge => edge.Side).ThenBy(edge => edge.Row).ThenBy(edge => edge.Column)];

    // Each edge as one entry per cell side it runs along.
    private static IEnumerable<(FootprintEdgeSide, int, int)> Unrolled(IEnumerable<FootprintEdge> edges)
    {
        foreach (var edge in edges)
        {
            var isAlongARow = edge.Side is FootprintEdgeSide.Top or FootprintEdgeSide.Bottom;
            for (var step = 0; step < edge.Length; step++)
            {
                yield return isAlongARow
                    ? (edge.Side, edge.Column + step, edge.Row)
                    : (edge.Side, edge.Column, edge.Row + step);
            }
        }
    }
}
