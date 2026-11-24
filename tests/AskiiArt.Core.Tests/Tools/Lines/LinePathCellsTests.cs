// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Lines;

namespace AskiiArt.Core.Tests.Tools.Lines;

// Which cells a line or an outline passes through: one to a column where it runs more across, one to a row where it
// runs more down, unbroken and each once (step 5.1).
public sealed class LinePathCellsTests
{
    private const double HalfAsWide = 0.5;

    [Fact]
    public void AFlatLine_TakesEveryCellAlongItsRow_InOrder()
    {
        var cells = CellsOf(LinePath.Straight(Middle(1, 2), Middle(5, 2)));

        Assert.Equal([new(1, 2), new(2, 2), new(3, 2), new(4, 2), new(5, 2)], cells);
    }

    [Fact]
    public void AnUprightLine_TakesEveryCellDownItsColumn_GoingUpwardsWhenDrawnUpwards()
    {
        var cells = CellsOf(LinePath.Straight(Middle(3, 4), Middle(3, 1)));

        Assert.Equal([new(3, 4), new(3, 3), new(3, 2), new(3, 1)], cells);
    }

    [Fact]
    public void AShallowLine_TakesOneCellToAColumn()
    {
        var cells = CellsOf(LinePath.Straight(Middle(0, 0), Middle(6, 3)));

        Assert.Equal(7, cells.Count);
        Assert.Equal(Enumerable.Range(0, 7), cells.Select(cell => cell.X));
        AssertUnbroken(cells, closed: false);
    }

    [Fact]
    public void ASteepLine_TakesOneCellToARow()
    {
        var cells = CellsOf(LinePath.Straight(Middle(0, 0), Middle(2, 6)));

        Assert.Equal(Enumerable.Range(0, 7), cells.Select(cell => cell.Y));
        AssertUnbroken(cells, closed: false);
    }

    [Fact]
    public void ALineStartingAndEndingPartWayAcrossCells_StillTakesTheCellsItsEndsAreIn()
    {
        var cells = CellsOf(LinePath.Straight(new CanvasPoint(0.9, 0.5), new CanvasPoint(3.1, 0.5)));

        Assert.Equal([new(0, 0), new(1, 0), new(2, 0), new(3, 0)], cells);
    }

    [Fact]
    public void APathOfOnePoint_TakesItsCell()
    {
        var cells = CellsOf(LinePath.Through([new CanvasPoint(2.3, 4.8)], closed: false));

        Assert.Equal([new(2, 4)], cells);
    }

    [Fact]
    public void ARectangle_TakesItsBorderCellsOnce_WithItsCornersMarked()
    {
        var rect = new CellRect(2, 1, 5, 4);
        var cells = LinePathCells.Of(LinePath.Rectangle(rect), HalfAsWide);

        var border = Enumerable.Range(rect.Top, rect.Height)
            .SelectMany(y => Enumerable.Range(rect.Left, rect.Width).Select(x => new CellPoint(x, y)))
            .Where(cell => cell.X == rect.Left || cell.X == rect.Right - 1 || cell.Y == rect.Top ||
                           cell.Y == rect.Bottom - 1)
            .ToHashSet();
        Assert.Equal(border.Count, cells.Count);
        Assert.True(border.SetEquals(cells.Select(cell => cell.Cell)));
        Assert.Equal(
            new HashSet<CellPoint> { new(2, 1), new(6, 1), new(6, 4), new(2, 4) },
            cells.Where(cell => cell.IsCorner).Select(cell => cell.Cell).ToHashSet());
    }

    [Theory]
    [InlineData(20, 10)]
    [InlineData(7, 4)]
    [InlineData(3, 9)]
    public void AnEllipse_IsUnbrokenAllRound_EachCellOnce_InsideItsRect_WithNoCorners(int width, int height)
    {
        var rect = new CellRect(1, 1, width, height);
        var cells = LinePathCells.Of(LinePath.Ellipse(rect), HalfAsWide);

        var points = cells.Select(cell => cell.Cell).ToList();
        AssertUnbroken(points, closed: true);
        Assert.Equal(points.Count, points.Distinct().Count());
        Assert.All(points, point => Assert.True(rect.Contains(point)));
        Assert.DoesNotContain(cells, cell => cell.IsCorner);
    }

    // A steep path's cells say how far it lies from their middles, so a precise match can centre it.
    [Fact]
    public void ASteepLine_SaysHowFarRightOfEachCellsMiddleItCrossesTheMiddleRow()
    {
        var cells = LinePathCells.Of(LinePath.Straight(new CanvasPoint(0.5, 0.5), new CanvasPoint(1.5, 4.5)),
            HalfAsWide);

        var third = cells.Single(cell => cell.Cell.Y == 2);
        Assert.Equal(new CellPoint(1, 2), third.Cell);
        Assert.Equal(-0.5, third.Offset.X, 9);
        Assert.Equal(0, third.Offset.Y);
    }

    [Fact]
    public void AGentleSlope_KeepsWhereItLiesUpAndDown()
    {
        var cells = LinePathCells.Of(LinePath.Straight(new CanvasPoint(0.5, 0.1), new CanvasPoint(10.5, 2.1)),
            HalfAsWide);

        Assert.All(cells, cell => Assert.Equal(default, cell.Offset));
    }

    private static List<CellPoint> CellsOf(LinePath path) =>
        [.. LinePathCells.Of(path, HalfAsWide).Select(cell => cell.Cell)];

    private static CanvasPoint Middle(int x, int y) => new(x + 0.5, y + 0.5);

    private static void AssertUnbroken(List<CellPoint> cells, bool closed)
    {
        var count = closed ? cells.Count : cells.Count - 1;
        for (var index = 0; index < count; index++)
        {
            var from = cells[index];
            var to = cells[(index + 1) % cells.Count];
            Assert.True(Math.Abs(to.X - from.X) <= 1 && Math.Abs(to.Y - from.Y) <= 1,
                $"{from} and {to} don't touch.");
        }
    }
}
