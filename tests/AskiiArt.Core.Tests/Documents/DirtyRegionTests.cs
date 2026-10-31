// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class DirtyRegionTests
{
    [Fact]
    public void CellsAlongARow_MergeIntoOneRect()
    {
        var region = new DirtyRegion();

        for (var x = 0; x < 5; x++)
            region.Add(Cell(x, 2));

        Assert.Equal([new CellRect(0, 2, 5, 1)], region.ToList());
    }

    [Fact]
    public void RowsOfTheSameSpan_MergeIntoOneRect()
    {
        var region = new DirtyRegion();

        region.Add(new CellRect(1, 0, 3, 1));
        region.Add(new CellRect(1, 1, 3, 1));
        region.Add(new CellRect(1, 2, 3, 1));

        Assert.Equal([new CellRect(1, 0, 3, 3)], region.ToList());
    }

    [Fact]
    public void ARowFilledOutOfOrder_StillMergesIntoOneRect()
    {
        var region = new DirtyRegion();

        region.Add(Cell(0, 0));
        region.Add(Cell(2, 0));
        region.Add(Cell(1, 0));

        Assert.Equal([new CellRect(0, 0, 3, 1)], region.ToList());
    }

    [Fact]
    public void DiagonalCells_StaySeparate()
    {
        var region = new DirtyRegion();

        region.Add(Cell(0, 0));
        region.Add(Cell(1, 1));

        Assert.Equal(2, region.ToList().Count);
    }

    [Fact]
    public void ARectInsideAnExistingOne_AddsNothing()
    {
        var region = new DirtyRegion();

        region.Add(new CellRect(0, 0, 4, 4));
        region.Add(new CellRect(1, 1, 2, 2));
        region.Add(CellRect.Empty);

        Assert.Equal([new CellRect(0, 0, 4, 4)], region.ToList());
    }

    [Fact]
    public void ARectContainingExistingOnes_ReplacesThem()
    {
        var region = new DirtyRegion();

        region.Add(Cell(1, 1));
        region.Add(Cell(5, 5));
        region.Add(new CellRect(0, 0, 10, 10));

        Assert.Equal([new CellRect(0, 0, 10, 10)], region.ToList());
    }

    [Fact]
    public void TooManyScatteredRects_CollapseIntoTheirBounds()
    {
        var region = new DirtyRegion();

        for (var index = 0; index <= DirtyRegion.MaximumRectCount; index++)
            region.Add(Cell(index * 2, index * 2));

        var bounds = Assert.Single(region.ToList());
        Assert.Equal(new CellRect(0, 0, (DirtyRegion.MaximumRectCount * 2) + 1, (DirtyRegion.MaximumRectCount * 2) + 1), bounds);
    }

    [Fact]
    public void EveryAddedCell_IsCovered()
    {
        var region = new DirtyRegion();
        var random = new Random(1234);
        var added = new List<CellPoint>();

        for (var index = 0; index < 200; index++)
        {
            var point = new CellPoint(random.Next(30), random.Next(30));
            added.Add(point);
            region.Add(CellRect.FromPoint(point));
        }

        var rects = region.ToList();
        Assert.InRange(rects.Count, 1, DirtyRegion.MaximumRectCount);
        Assert.All(added, point => Assert.Contains(rects, rect => rect.Contains(point)));
    }

    private static CellRect Cell(int x, int y) => CellRect.FromPoint(new CellPoint(x, y));
}
