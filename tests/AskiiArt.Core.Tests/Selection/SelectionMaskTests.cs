// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

public sealed class SelectionMaskTests
{
    private static readonly CanvasSize _size = new(5, 3);

    [Fact]
    public void Empty_SelectsNothing()
    {
        var mask = SelectionMask.Empty(_size);

        Assert.True(mask.IsEmpty);
        Assert.False(mask.IsRectangle);
        Assert.Equal(CellRect.Empty, mask.Bounds);
        Assert.False(mask.Contains(new CellPoint(0, 0)));
        Assert.Empty(mask.Edges);
    }

    [Fact]
    public void All_SelectsEveryCell()
    {
        var mask = SelectionMask.All(_size);

        Assert.True(mask.IsRectangle);
        Assert.Equal(_size.Bounds, mask.Bounds);
        Assert.Equal(["#####", "#####", "#####"], TestMasks.Describe(mask));
    }

    [Fact]
    public void Rectangle_LeavesOutCellsOffTheCanvas()
    {
        var mask = SelectionMask.Rectangle(_size, new CellRect(3, -1, 4, 3));

        Assert.Equal(CellRect.FromEdges(3, 0, 5, 2), mask.Bounds);
        Assert.Equal(["···##", "···##", "·····"], TestMasks.Describe(mask));
        Assert.False(mask.Contains(new CellPoint(5, 0)));
        Assert.False(mask.Contains(new CellPoint(3, -1)));
    }

    [Fact]
    public void Rectangle_WhollyOffTheCanvas_IsEmpty()
    {
        Assert.Equal(SelectionMask.Empty(_size), SelectionMask.Rectangle(_size, new CellRect(6, 0, 2, 2)));
    }

    [Fact]
    public void Builder_TrimsTheBoundsToTheSelectedCells()
    {
        var mask = TestMasks.Parse("·····", "·#·#·", "··#··");

        Assert.Equal(CellRect.FromEdges(1, 1, 4, 3), mask.Bounds);
        Assert.False(mask.IsRectangle);
        Assert.Equal(["·····", "·#·#·", "··#··"], TestMasks.Describe(mask));
    }

    [Fact]
    public void Builder_ForARectangle_EqualsTheSameRectangle()
    {
        var built = TestMasks.Parse("·····", "·###·", "·###·");
        var rectangle = SelectionMask.Rectangle(_size, CellRect.FromEdges(1, 1, 4, 3));

        Assert.True(built.IsRectangle);
        Assert.Equal(rectangle, built);
        Assert.Equal(rectangle.GetHashCode(), built.GetHashCode());
        Assert.Equal(rectangle.EstimatedMemoryInBytes, built.EstimatedMemoryInBytes);
    }

    [Fact]
    public void Builder_KeepsToItsArea()
    {
        var builder = new SelectionMaskBuilder(_size, CellRect.FromEdges(1, 0, 3, 2));

        builder.AddRun(row: 0, column: -2, length: 9);
        builder.Add(new CellPoint(4, 2));

        Assert.Equal(["·##··", "·····", "·····"], TestMasks.Describe(builder.Build()));
    }

    [Fact]
    public void Equality_ComparesCanvasSizeAndCells()
    {
        var mask = TestMasks.Parse("#·#··", "·····", "·····");

        Assert.Equal(mask, TestMasks.Parse("#·#··", "·····", "·····"));
        Assert.NotEqual(mask, TestMasks.Parse("#··#·", "·····", "·····"));
        Assert.NotEqual(mask, TestMasks.Parse("###··", "·····", "·····"));
        Assert.NotEqual(SelectionMask.Empty(_size), SelectionMask.Empty(new CanvasSize(5, 4)));
        Assert.Equal(SelectionMask.Empty(_size), TestMasks.Parse("·····", "·····", "·····"));
    }

    [Fact]
    public void ASmallSelection_IsSmall_OnAHugeCanvas()
    {
        var huge = new CanvasSize(4000, 4000);
        var builder = new SelectionMaskBuilder(huge);
        builder.Add(new CellPoint(2000, 2000));
        builder.Add(new CellPoint(2002, 2001));

        Assert.True(builder.Build().EstimatedMemoryInBytes < 256);
        Assert.True(SelectionMask.All(huge).EstimatedMemoryInBytes < 256);
    }

    [Fact]
    public void RunsInRow_OfARectangle_IsOneRunPerRow()
    {
        var mask = SelectionMask.Rectangle(_size, new CellRect(1, 1, 3, 2));

        Assert.Equal([], Runs(mask, 0));
        Assert.Equal([(1, 3)], Runs(mask, 1));
        Assert.Equal([(1, 3)], Runs(mask, 2));
        Assert.Equal([], Runs(mask, 5));
    }

    [Fact]
    public void RunsInRow_GoLeftToRight_InCanvasColumns()
    {
        var mask = TestMasks.Parse("·····", "·#·##", "··#··");

        Assert.Equal([(1, 1), (3, 2)], Runs(mask, 1));
        Assert.Equal([(2, 1)], Runs(mask, 2));
    }

    [Fact]
    public void RunsInRow_CoverEverySelectedCell_AndNoOther()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var mask = TestMasks.Random(new CanvasSize(70, 6), seed);
            for (var row = 0; row < 6; row++)
            {
                var covered = new bool[70];
                foreach (var (column, length) in mask.RunsInRow(row))
                {
                    for (var x = column; x < column + length; x++)
                        covered[x] = true;
                }

                for (var x = 0; x < 70; x++)
                    Assert.Equal(mask.Contains(new CellPoint(x, row)), covered[x]);
            }
        }
    }

    [Fact]
    public void Edges_OfARectangle_AreItsFourSides()
    {
        var mask = SelectionMask.Rectangle(_size, CellRect.FromEdges(1, 0, 4, 2));

        Assert.Equal(
            [
                new CellEdge(CellEdgeSide.Top, 1, 0, 3),
                new CellEdge(CellEdgeSide.Bottom, 1, 1, 3),
                new CellEdge(CellEdgeSide.Left, 1, 0, 2),
                new CellEdge(CellEdgeSide.Right, 3, 0, 2)
            ],
            mask.Edges.Order(EdgeOrder.Instance));
    }

    [Fact]
    public void Edges_OfAHole_GoAroundItOnTheInside()
    {
        var mask = TestMasks.Parse("###", "#·#", "###");

        Assert.Contains(new CellEdge(CellEdgeSide.Bottom, 1, 0, 1), mask.Edges);
        Assert.Contains(new CellEdge(CellEdgeSide.Top, 1, 2, 1), mask.Edges);
        Assert.Contains(new CellEdge(CellEdgeSide.Right, 0, 1, 1), mask.Edges);
        Assert.Contains(new CellEdge(CellEdgeSide.Left, 2, 1, 1), mask.Edges);
    }

    [Fact]
    public void Edges_JoinNeighbouringSides_IntoOneRun()
    {
        var mask = TestMasks.Parse("####", "##··");

        Assert.Contains(new CellEdge(CellEdgeSide.Top, 0, 0, 4), mask.Edges);
        Assert.Contains(new CellEdge(CellEdgeSide.Left, 0, 0, 2), mask.Edges);
        Assert.Equal(6, mask.Edges.Count);
    }

    [Theory]
    [InlineData(7, 5, 1)]
    [InlineData(64, 4, 2)]
    [InlineData(65, 6, 3)]
    [InlineData(130, 5, 4)]
    public void Edges_AreEverySideOfASelectedCellWhoseNeighbourIsNot(int width, int height, int seed)
    {
        var mask = TestMasks.Random(new CanvasSize(width, height), seed);

        Assert.Equal(UnitEdgesByBruteForce(mask), UnitEdgesOf(mask.Edges));
    }

    [Theory]
    [InlineData(ResizeAnchor.TopLeft)]
    [InlineData(ResizeAnchor.Top)]
    [InlineData(ResizeAnchor.TopRight)]
    [InlineData(ResizeAnchor.Left)]
    [InlineData(ResizeAnchor.Center)]
    [InlineData(ResizeAnchor.Right)]
    [InlineData(ResizeAnchor.BottomLeft)]
    [InlineData(ResizeAnchor.Bottom)]
    [InlineData(ResizeAnchor.BottomRight)]
    public void Resized_MovesTheCellsWithTheArt(ResizeAnchor anchor)
    {
        var mask = TestMasks.Parse("#··", "·#·", "··#");

        foreach (var newSize in new[] { new CanvasSize(5, 6), new CanvasSize(2, 2), new CanvasSize(4, 1) })
        {
            var plan = CanvasResizePlan.Create(mask.CanvasSize, newSize, anchor);

            var resized = mask.Resized(plan);

            Assert.Equal(newSize, resized.CanvasSize);
            Assert.Equal(Shifted(mask, plan), TestMasks.Describe(resized));
        }
    }

    [Fact]
    public void Moved_MovesARaggedSelection_OnTheSameCanvas()
    {
        var mask = TestMasks.Parse("#···", "##··", "····");

        var moved = mask.Moved(new CellPoint(2, 1));

        Assert.Equal(mask.CanvasSize, moved.CanvasSize);
        Assert.Equal(["····", "··#·", "··##"], TestMasks.Describe(moved));
    }

    [Fact]
    public void Moved_PartlyOffTheCanvas_LosesTheCellsOffIt()
    {
        Assert.Equal(["#···", "····"], TestMasks.Describe(TestMasks.Parse("##··", "····").Moved(new CellPoint(-1, 0))));
        Assert.Equal(["····", "··#·"], TestMasks.Describe(TestMasks.Parse("·#··", "·##·").Moved(new CellPoint(1, 1))));
    }

    [Fact]
    public void Moved_WhollyOffTheCanvas_IsEmpty() =>
        Assert.True(TestMasks.Parse("##", "··").Moved(new CellPoint(0, 2)).IsEmpty);

    [Fact]
    public void MovedOnto_PlacesTheCellsOnACanvasOfAnotherSize_WithoutAnyOffIt()
    {
        var moved = TestMasks.Parse("##", "·#").MovedOnto(new CanvasSize(4, 2), new CellPoint(2, 1));

        Assert.Equal(new CanvasSize(4, 2), moved.CanvasSize);
        Assert.Equal(["····", "··##"], TestMasks.Describe(moved));
    }

    [Fact]
    public void Moved_ByNothing_IsTheSame()
    {
        var mask = TestMasks.Parse("#·#", "·#·");

        Assert.Equal(mask, mask.Moved(default));
    }

    [Fact]
    public void Resized_Bigger_AroundTheCentre_LeavesTheNewEdgesUnselected()
    {
        var mask = TestMasks.Parse("#·", "##");

        var resized = mask.Resized(CanvasResizePlan.Create(mask.CanvasSize, new CanvasSize(4, 4), ResizeAnchor.Center));

        Assert.Equal(["····", "·#··", "·##·", "····"], TestMasks.Describe(resized));
    }

    [Fact]
    public void Resized_SoAllItsCellsAreCropped_IsEmpty()
    {
        var mask = TestMasks.Parse("##·", "···", "···");

        var resized = mask.Resized(
            CanvasResizePlan.Create(mask.CanvasSize, new CanvasSize(1, 1), ResizeAnchor.BottomRight));

        Assert.True(resized.IsEmpty);
        Assert.Equal(new CanvasSize(1, 1), resized.CanvasSize);
    }

    [Fact]
    public void Resized_ARectangle_StaysARectangle()
    {
        var resized = SelectionMask.All(_size).Resized(
            CanvasResizePlan.Create(_size, new CanvasSize(3, 3), ResizeAnchor.Right));

        Assert.True(resized.IsRectangle);
        Assert.Equal(SelectionMask.All(new CanvasSize(3, 3)), resized);
    }

    [Fact]
    public void Resized_WithAPlanForAnotherCanvas_Throws()
    {
        var plan = CanvasResizePlan.Create(new CanvasSize(9, 9), _size, ResizeAnchor.TopLeft);

        Assert.Throws<ArgumentException>(() => SelectionMask.All(_size).Resized(plan));
    }

    [Fact]
    public void Inverted_SelectsWhatWasntSelected_AndNothingThatWas()
    {
        var mask = TestMasks.Parse("##···", "·#···", "·····");

        Assert.Equal(["··###", "#·###", "#####"], TestMasks.Describe(mask.Inverted()));
        Assert.Equal(SelectionMask.All(_size), SelectionMask.Empty(_size).Inverted());
        Assert.True(SelectionMask.All(_size).Inverted().IsEmpty);
    }

    [Fact]
    public void Grown_AddsEveryCellTouchingTheSelection_CornersIncluded()
    {
        var mask = TestMasks.Parse("·····", "··#··", "·····", "·····");

        Assert.Equal(["·###·", "·###·", "·###·", "·····"], TestMasks.Describe(mask.Grown()));
    }

    [Fact]
    public void Grown_StopsAtTheCanvasEdges()
    {
        var mask = TestMasks.Parse("#····", "·····", "····#");

        Assert.Equal(["##···", "##·##", "···##"], TestMasks.Describe(mask.Grown()));
    }

    [Fact]
    public void Grown_OfNothing_IsNothing()
    {
        Assert.True(SelectionMask.Empty(_size).Grown().IsEmpty);
    }

    [Fact]
    public void Shrunk_LeavesOutEveryCellTouchingAnUnselectedOne_CornersIncluded()
    {
        var mask = TestMasks.Parse("·····", "·###·", "·###·", "·###·", "·····");

        Assert.Equal(["·····", "·····", "··#··", "·····", "·····"], TestMasks.Describe(mask.Shrunk()));
    }

    [Fact]
    public void Shrunk_KeepsCellsAlongTheCanvasEdges()
    {
        var mask = TestMasks.Parse("###··", "###··", "####·");

        Assert.Equal(["##···", "##···", "##···"], TestMasks.Describe(mask.Shrunk()));
        Assert.Equal(SelectionMask.All(_size), SelectionMask.All(_size).Shrunk());
    }

    [Fact]
    public void Shrunk_ThenGrown_OfAThinLine_IsNothing()
    {
        var mask = TestMasks.Parse("·····", "·###·", "·····");

        Assert.True(mask.Shrunk().IsEmpty);
        Assert.True(mask.Shrunk().Grown().IsEmpty);
    }

    [Fact]
    public void GrownAndShrunk_MatchABruteForceCount_OnRandomMasks()
    {
        var size = new CanvasSize(23, 11);
        for (var seed = 0; seed < 20; seed++)
        {
            var mask = TestMasks.Random(size, seed);

            Assert.Equal(GrownByBruteForce(mask), TestMasks.Describe(mask.Grown()));
            Assert.Equal(ShrunkByBruteForce(mask), TestMasks.Describe(mask.Shrunk()));
        }
    }

    // A cell is selected after growing when it or any of its eight neighbours was.
    private static string[] GrownByBruteForce(SelectionMask mask) =>
        ByNeighbours(mask, (cell, neighbours) => cell || neighbours.Any(selected => selected));

    // A cell stays selected after shrinking when it and all its neighbours on the canvas were.
    private static string[] ShrunkByBruteForce(SelectionMask mask) =>
        ByNeighbours(mask, (cell, neighbours) => cell && neighbours.All(selected => selected));

    private static string[] ByNeighbours(SelectionMask mask, Func<bool, List<bool>, bool> isSelected)
    {
        var size = mask.CanvasSize;
        var rows = new string[size.Height];
        for (var y = 0; y < size.Height; y++)
        {
            var row = new char[size.Width];
            for (var x = 0; x < size.Width; x++)
            {
                var neighbours = new List<bool>();
                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var neighbour = new CellPoint(x + dx, y + dy);
                        if ((dx != 0 || dy != 0) && size.Contains(neighbour))
                            neighbours.Add(mask.Contains(neighbour));
                    }
                }

                var selected = isSelected(mask.Contains(new CellPoint(x, y)), neighbours);
                row[x] = selected ? TestMasks.Selected : TestCells.EmptyMarker;
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    // What resizing should give: each new cell is selected when the old cell that moves onto it was.
    private static string[] Shifted(SelectionMask mask, CanvasResizePlan plan)
    {
        var builder = new SelectionMaskBuilder(plan.NewSize);
        for (var y = 0; y < plan.NewSize.Height; y++)
        {
            for (var x = 0; x < plan.NewSize.Width; x++)
            {
                if (mask.Contains(new CellPoint(x - plan.ContentOffset.X, y - plan.ContentOffset.Y)))
                    builder.Add(new CellPoint(x, y));
            }
        }

        return TestMasks.Describe(builder.Build());
    }

    private static HashSet<CellEdge> UnitEdgesByBruteForce(SelectionMask mask)
    {
        var edges = new HashSet<CellEdge>();
        for (var y = 0; y < mask.CanvasSize.Height; y++)
        {
            for (var x = 0; x < mask.CanvasSize.Width; x++)
            {
                if (!mask.Contains(new CellPoint(x, y)))
                    continue;

                if (!mask.Contains(new CellPoint(x, y - 1)))
                    edges.Add(new CellEdge(CellEdgeSide.Top, x, y, 1));

                if (!mask.Contains(new CellPoint(x, y + 1)))
                    edges.Add(new CellEdge(CellEdgeSide.Bottom, x, y, 1));

                if (!mask.Contains(new CellPoint(x - 1, y)))
                    edges.Add(new CellEdge(CellEdgeSide.Left, x, y, 1));

                if (!mask.Contains(new CellPoint(x + 1, y)))
                    edges.Add(new CellEdge(CellEdgeSide.Right, x, y, 1));
            }
        }

        return edges;
    }

    // Splits each run into one edge per cell side, checking that no two runs overlap.
    private static HashSet<CellEdge> UnitEdgesOf(IEnumerable<CellEdge> runs)
    {
        var edges = new HashSet<CellEdge>();
        foreach (var run in runs)
        {
            var isHorizontal = run.Side is CellEdgeSide.Top or CellEdgeSide.Bottom;
            for (var step = 0; step < run.Length; step++)
            {
                var unit = isHorizontal
                    ? run with { Column = run.Column + step, Length = 1 }
                    : run with { Row = run.Row + step, Length = 1 };
                Assert.True(edges.Add(unit), $"{unit} is in two runs.");
            }
        }

        return edges;
    }

    private sealed class EdgeOrder : IComparer<CellEdge>
    {
        public static EdgeOrder Instance { get; } = new();

        public int Compare(CellEdge x, CellEdge y) =>
            (x.Side, x.Row, x.Column, x.Length).CompareTo((y.Side, y.Row, y.Column, y.Length));
    }

    private static List<(int Column, int Length)> Runs(SelectionMask mask, int row)
    {
        var runs = new List<(int Column, int Length)>();
        foreach (var run in mask.RunsInRow(row))
            runs.Add(run);

        return runs;
    }
}
