// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class LayerTests
{
    [Fact]
    public void SetCell_StoresTheCellAtThatPoint()
    {
        var layer = TestCells.CreateDocument(3, 2).ActiveFrame.Layers[0];

        layer.SetCell(new CellPoint(2, 1), TestCells.Glyph('Z'));

        Assert.Equal(TestCells.Glyph('Z'), layer.GetCell(new CellPoint(2, 1)));
        Assert.Equal(["···", "··Z"], TestCells.Read(layer));
    }

    [Fact]
    public void IndexOf_IsRowMajor()
    {
        var layer = TestCells.CreateDocument(3, 2).ActiveFrame.Layers[0];

        Assert.Equal(5, layer.IndexOf(new CellPoint(2, 1)));
        Assert.Equal(new CellPoint(2, 1), layer.PointAt(5));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, 2)]
    public void GetCellAndSetCell_RejectPointsOutsideTheCanvas(int x, int y)
    {
        var layer = TestCells.CreateDocument(3, 2).ActiveFrame.Layers[0];
        var point = new CellPoint(x, y);

        Assert.Throws<ArgumentOutOfRangeException>(() => layer.GetCell(point));
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.SetCell(point, TestCells.Glyph('x')));
    }

    [Fact]
    public void CopyCells_CopiesTheRegionRowByRow()
    {
        var layer = TestCells.CreateDocument(4, 3).ActiveFrame.Layers[0];
        TestCells.Paint(layer, "abcd", "efgh", "ijkl");
        var destination = new Cell[4];

        layer.CopyCells(new CellRect(1, 1, 2, 2), destination);

        Assert.Equal(["fg", "jk"], TestCells.Describe(destination, 2));
    }

    [Fact]
    public void CopyCells_RejectsARegionOutsideTheCanvasOrAShortDestination()
    {
        var layer = TestCells.CreateDocument(4, 3).ActiveFrame.Layers[0];

        Assert.Throws<ArgumentOutOfRangeException>(() => layer.CopyCells(new CellRect(3, 0, 2, 1), new Cell[2]));
        Assert.Throws<ArgumentException>(() => layer.CopyCells(new CellRect(0, 0, 2, 2), new Cell[3]));
    }

    [Fact]
    public void SetProperties_ReplacesTheProperties()
    {
        var layer = TestCells.CreateDocument(2, 2).ActiveFrame.Layers[0];
        var locked = layer.Properties with { IsLocked = true };

        layer.SetProperties(locked);

        Assert.Same(locked, layer.Properties);
    }
}
