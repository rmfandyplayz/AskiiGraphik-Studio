// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CellRectTests
{
    [Fact]
    public void Edges_AreExclusiveOnTheRightAndBottom()
    {
        var rect = new CellRect(2, 3, 4, 5);

        Assert.Equal(2, rect.Left);
        Assert.Equal(3, rect.Top);
        Assert.Equal(6, rect.Right);
        Assert.Equal(8, rect.Bottom);
        Assert.Equal(20, rect.Area);
        Assert.Equal(new CellPoint(2, 3), rect.Position);
    }

    [Fact]
    public void Constructor_RejectsANegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CellRect(0, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CellRect(0, 0, 1, -1));
    }

    [Fact]
    public void FromEdges_AndFromPoint_BuildTheExpectedRects()
    {
        Assert.Equal(new CellRect(1, 2, 3, 4), CellRect.FromEdges(1, 2, 4, 6));
        Assert.Equal(new CellRect(5, 7, 1, 1), CellRect.FromPoint(new CellPoint(5, 7)));
    }

    [Fact]
    public void Contains_Point_ExcludesTheRightAndBottomEdges()
    {
        var rect = new CellRect(0, 0, 2, 2);

        Assert.True(rect.Contains(new CellPoint(0, 0)));
        Assert.True(rect.Contains(new CellPoint(1, 1)));
        Assert.False(rect.Contains(new CellPoint(2, 1)));
        Assert.False(rect.Contains(new CellPoint(1, 2)));
        Assert.False(rect.Contains(new CellPoint(-1, 0)));
    }

    [Fact]
    public void Contains_Rect_TreatsAnEmptyRectAsInsideEverything()
    {
        var rect = new CellRect(0, 0, 4, 4);

        Assert.True(rect.Contains(new CellRect(1, 1, 2, 2)));
        Assert.True(rect.Contains(rect));
        Assert.False(rect.Contains(new CellRect(3, 3, 2, 2)));
        Assert.True(rect.Contains(new CellRect(100, 100, 0, 0)));
        Assert.False(CellRect.Empty.Contains(rect));
    }

    [Fact]
    public void Intersect_ReturnsTheSharedCells_OrEmptyWhenOnlyEdgesTouch()
    {
        var rect = new CellRect(0, 0, 4, 4);

        Assert.Equal(new CellRect(2, 2, 2, 2), rect.Intersect(new CellRect(2, 2, 5, 5)));
        Assert.Equal(CellRect.Empty, rect.Intersect(new CellRect(4, 0, 2, 2)));
        Assert.False(rect.Intersects(new CellRect(4, 0, 2, 2)));
        Assert.True(rect.Intersects(new CellRect(3, 3, 2, 2)));
    }

    [Fact]
    public void Union_IsTheBoundingRect_AndIgnoresEmptyRects()
    {
        var first = new CellRect(0, 0, 1, 1);
        var second = new CellRect(3, 2, 1, 1);

        Assert.Equal(new CellRect(0, 0, 4, 3), first.Union(second));
        Assert.Equal(second, CellRect.Empty.Union(second));
        Assert.Equal(first, first.Union(new CellRect(50, 50, 0, 0)));
        Assert.Equal(CellRect.Empty, CellRect.Empty.Union(new CellRect(9, 9, 0, 3)));
    }

    [Fact]
    public void Offset_MovesTheRect()
    {
        Assert.Equal(new CellRect(-1, 5, 2, 2), new CellRect(1, 2, 2, 2).Offset(new CellPoint(-2, 3)));
    }
}
