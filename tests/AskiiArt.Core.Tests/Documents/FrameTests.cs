// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class FrameTests
{
    [Fact]
    public void InsertLayer_PutsTheLayerAtTheIndex_WithIndexZeroAtTheBottom()
    {
        var document = TestCells.CreateDocument(2, 2);
        var frame = document.ActiveFrame;
        var original = frame.Layers[0];
        var below = NewLayer(document);

        frame.InsertLayer(0, below);

        Assert.Equal([below, original], frame.Layers);
        Assert.Equal(0, frame.IndexOf(below.Id));
        Assert.Equal(-1, frame.IndexOf(Guid.NewGuid()));
    }

    [Fact]
    public void InsertLayer_RejectsALayerOfAnotherSize()
    {
        var document = TestCells.CreateDocument(2, 2);
        var layer = new Layer(Guid.NewGuid(), new LayerProperties("Wrong"), new CanvasSize(3, 2));

        Assert.Throws<ArgumentException>(() => document.ActiveFrame.InsertLayer(0, layer));
    }

    [Fact]
    public void InsertLayer_RejectsADuplicateId()
    {
        var document = TestCells.CreateDocument(2, 2);
        var existingId = document.ActiveFrame.Layers[0].Id;
        var duplicate = new Layer(existingId, new LayerProperties("Copy"), document.CanvasSize);

        Assert.Throws<ArgumentException>(() => document.ActiveFrame.InsertLayer(0, duplicate));
    }

    [Fact]
    public void InsertLayer_RejectsALayerThatAlreadyBelongsToAFrame()
    {
        var document = TestCells.CreateDocument(2, 2);
        var existing = document.ActiveFrame.Layers[0];

        Assert.Throws<InvalidOperationException>(() => TestCells.CreateDocument(2, 2).ActiveFrame.InsertLayer(0, existing));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void InsertLayer_RejectsAnIndexOutOfRange(int index)
    {
        var document = TestCells.CreateDocument(2, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.ActiveFrame.InsertLayer(index, NewLayer(document)));
    }

    [Fact]
    public void RemoveLayer_ReturnsTheDetachedLayerWithItsCells()
    {
        var document = TestCells.CreateDocument(2, 1);
        var removed = TestCells.AddLayer(document);
        TestCells.Paint(removed, "hi");

        var result = document.ActiveFrame.RemoveLayer(removed.Id);

        Assert.Same(removed, result);
        Assert.Single(document.ActiveFrame.Layers);
        Assert.False(document.TryGetLayer(removed.Id, out _));
        Assert.Equal(["hi"], TestCells.Read(result));

        // A detached layer can join a frame again.
        document.ActiveFrame.InsertLayer(1, result);
        Assert.Same(result, document.GetLayer(removed.Id));
    }

    [Fact]
    public void RemoveLayer_RefusesToRemoveTheLastLayer()
    {
        var document = TestCells.CreateDocument(2, 2);

        Assert.Throws<InvalidOperationException>(() => document.ActiveFrame.RemoveLayer(document.ActiveFrame.Layers[0].Id));
    }

    [Fact]
    public void RemoveLayer_WithAnUnknownId_Throws()
    {
        var document = TestCells.CreateDocument(2, 2);
        TestCells.AddLayer(document);

        Assert.Throws<KeyNotFoundException>(() => document.ActiveFrame.RemoveLayer(Guid.NewGuid()));
    }

    [Fact]
    public void MoveLayer_ReordersTheLayers()
    {
        var document = TestCells.CreateDocument(2, 2);
        var frame = document.ActiveFrame;
        var first = frame.Layers[0];
        var second = TestCells.AddLayer(document);
        var third = TestCells.AddLayer(document);

        frame.MoveLayer(first.Id, 2);
        Assert.Equal([second, third, first], frame.Layers);

        frame.MoveLayer(first.Id, 0);
        Assert.Equal([first, second, third], frame.Layers);
    }

    [Fact]
    public void MoveLayer_RejectsAnIndexOutOfRange()
    {
        var document = TestCells.CreateDocument(2, 2);
        var layer = TestCells.AddLayer(document);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.ActiveFrame.MoveLayer(layer.Id, 2));
    }

    private static Layer NewLayer(Document document) =>
        new(Guid.NewGuid(), new LayerProperties("New"), document.CanvasSize);
}
