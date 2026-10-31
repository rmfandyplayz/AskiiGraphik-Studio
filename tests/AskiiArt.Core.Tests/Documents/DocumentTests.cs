// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class DocumentTests
{
    [Fact]
    public void Create_MakesOneFrameWithOneEmptyVisibleLayer()
    {
        var document = Document.Create(new CanvasSize(3, 2), "Background");

        var frame = Assert.Single(document.Frames);
        Assert.Same(frame, document.ActiveFrame);
        Assert.Same(document, frame.Document);

        var layer = Assert.Single(frame.Layers);
        Assert.Equal("Background", layer.Properties.Name);
        Assert.True(layer.Properties.IsVisible);
        Assert.Equal(new CanvasSize(3, 2), layer.Size);
        Assert.Equal(["···", "···"], TestCells.Read(layer));
    }

    [Fact]
    public void Create_GivesEveryDocumentADifferentLayerId()
    {
        var first = TestCells.CreateDocument(2, 2).ActiveFrame.Layers[0];
        var second = TestCells.CreateDocument(2, 2).ActiveFrame.Layers[0];

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void GetLayer_FindsALayerById()
    {
        var document = TestCells.CreateDocument(2, 2);
        var added = TestCells.AddLayer(document);

        Assert.Same(added, document.GetLayer(added.Id));
        Assert.True(document.TryGetLayer(added.Id, out var found));
        Assert.Same(added, found);
    }

    [Fact]
    public void GetLayer_WithAnUnknownId_Throws()
    {
        var document = TestCells.CreateDocument(2, 2);
        var unknownId = Guid.NewGuid();

        Assert.Throws<KeyNotFoundException>(() => document.GetLayer(unknownId));
        Assert.False(document.TryGetLayer(unknownId, out _));
    }

    [Fact]
    public void ResizeCanvas_ResizesEveryLayerAndMovesTheirCells()
    {
        var document = TestCells.CreateDocument(3, 2);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        TestCells.Paint(bottom, "abc", "def");
        TestCells.Paint(top, "·X·", "···");

        document.ResizeCanvas(new CanvasSize(4, 3), new CellPoint(1, 1));

        Assert.Equal(new CanvasSize(4, 3), document.CanvasSize);
        Assert.Equal(["····", "·abc", "·def"], TestCells.Read(bottom));
        Assert.Equal(["····", "··X·", "····"], TestCells.Read(top));
    }

    [Fact]
    public void ResizeCanvas_DropsCellsThatLandOutside()
    {
        var document = TestCells.CreateDocument(3, 3);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "abc", "def", "ghi");

        document.ResizeCanvas(new CanvasSize(2, 2), new CellPoint(-1, -1));

        Assert.Equal(["ef", "hi"], TestCells.Read(layer));
    }
}
