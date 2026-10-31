// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class DocumentChangeEventTests
{
    [Fact]
    public void SetCell_OutsideAGroup_RaisesOneEventWithTheLayerAndCell()
    {
        var document = TestCells.CreateDocument(4, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        layer.SetCell(new CellPoint(2, 3), TestCells.Glyph('x'));

        var change = Assert.Single(events);
        var cells = Assert.Single(change.CellChanges);
        Assert.Equal(layer.Id, cells.LayerId);
        Assert.Equal([new CellRect(2, 3, 1, 1)], cells.Regions);
        Assert.False(change.LayerListChanged);
        Assert.Empty(change.LayersWithChangedProperties);
        Assert.Null(change.CanvasResize);
    }

    [Fact]
    public void SetCell_WithTheValueTheCellAlreadyHolds_RaisesNothing()
    {
        var document = TestCells.CreateDocument(4, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        layer.SetCell(new CellPoint(0, 0), Cell.Empty);

        Assert.Empty(events);
    }

    [Fact]
    public void AGroup_RaisesOneEventAtTheEnd_WithAdjacentCellsMergedIntoOneRect()
    {
        var document = TestCells.CreateDocument(10, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        using (document.BeginChanges())
        {
            TestCells.Paint(layer, "", "hello");
            Assert.Empty(events);
        }

        var change = Assert.Single(events);
        Assert.Equal([new CellRect(0, 1, 5, 1)], Assert.Single(change.CellChanges).Regions);
    }

    [Fact]
    public void NestedGroups_RaiseOnlyWhenTheOutermostEnds()
    {
        var document = TestCells.CreateDocument(4, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        using (document.BeginChanges())
        {
            using (document.BeginChanges())
                layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a'));

            Assert.Empty(events);
            layer.SetCell(new CellPoint(3, 3), TestCells.Glyph('b'));
        }

        var change = Assert.Single(events);
        Assert.Equal(
            [new CellRect(0, 0, 1, 1), new CellRect(3, 3, 1, 1)],
            Assert.Single(change.CellChanges).Regions.OrderBy(rect => rect.Y));
    }

    [Fact]
    public void AGroupWithNoChanges_RaisesNothing()
    {
        var document = TestCells.CreateDocument(4, 4);
        var events = Record(document);

        using (document.BeginChanges())
            document.ActiveFrame.Layers[0].SetCell(new CellPoint(1, 1), Cell.Empty);

        Assert.Empty(events);
    }

    [Fact]
    public void AGroup_ListsEachChangedLayerOnceInTheOrderFirstChanged()
    {
        var document = TestCells.CreateDocument(4, 4);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        var events = Record(document);

        using (document.BeginChanges())
        {
            top.SetCell(new CellPoint(0, 0), TestCells.Glyph('t'));
            bottom.SetCell(new CellPoint(0, 0), TestCells.Glyph('b'));
            top.SetCell(new CellPoint(1, 0), TestCells.Glyph('t'));
        }

        var change = Assert.Single(events);
        Assert.Equal([top.Id, bottom.Id], change.CellChanges.Select(cells => cells.LayerId));
    }

    [Fact]
    public void LayerListChanges_AreReported()
    {
        var document = TestCells.CreateDocument(2, 2);
        var events = Record(document);

        var added = TestCells.AddLayer(document);
        document.ActiveFrame.MoveLayer(added.Id, 0);
        document.ActiveFrame.RemoveLayer(added.Id);

        Assert.Equal(3, events.Count);
        Assert.All(events, change => Assert.True(change.LayerListChanged));
    }

    [Fact]
    public void MovingALayerToItsOwnIndex_RaisesNothing()
    {
        var document = TestCells.CreateDocument(2, 2);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        document.ActiveFrame.MoveLayer(layer.Id, 0);

        Assert.Empty(events);
    }

    [Fact]
    public void PropertyChanges_AreReported_UnlessThePropertiesAreEqual()
    {
        var document = TestCells.CreateDocument(2, 2);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        layer.SetProperties(layer.Properties with { IsVisible = false });
        layer.SetProperties(layer.Properties with { IsVisible = false });

        var change = Assert.Single(events);
        Assert.Equal([layer.Id], change.LayersWithChangedProperties);
    }

    [Fact]
    public void ADetachedLayer_RaisesNothing()
    {
        var document = TestCells.CreateDocument(2, 2);
        var removed = document.ActiveFrame.RemoveLayer(TestCells.AddLayer(document).Id);
        var events = Record(document);

        removed.SetCell(new CellPoint(0, 0), TestCells.Glyph('x'));

        Assert.Empty(events);
    }

    [Fact]
    public void ACanvasResize_ReplacesCellChangesInTheSameGroup()
    {
        var document = TestCells.CreateDocument(4, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = Record(document);

        using (document.BeginChanges())
        {
            layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a'));
            document.ResizeCanvas(new CanvasSize(6, 5), new CellPoint(1, 0));
            layer.SetCell(new CellPoint(5, 4), TestCells.Glyph('b'));
            document.ResizeCanvas(new CanvasSize(7, 5), new CellPoint(1, 0));
        }

        var change = Assert.Single(events);
        Assert.Empty(change.CellChanges);
        Assert.Equal(new CanvasResizeChange(new CanvasSize(4, 4), new CanvasSize(7, 5), new CellPoint(2, 0)), change.CanvasResize);
    }

    [Fact]
    public void AHandlerThatChangesTheDocument_GetsItsOwnEvent()
    {
        var document = TestCells.CreateDocument(4, 4);
        var layer = document.ActiveFrame.Layers[0];
        var events = new List<DocumentChangedEventArgs>();
        document.Changed += (_, change) =>
        {
            events.Add(change);
            if (events.Count == 1)
                layer.SetCell(new CellPoint(1, 1), TestCells.Glyph('b'));
        };

        layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a'));

        Assert.Equal(2, events.Count);
        Assert.Equal([new CellRect(1, 1, 1, 1)], Assert.Single(events[1].CellChanges).Regions);
    }

    private static List<DocumentChangedEventArgs> Record(Document document)
    {
        var events = new List<DocumentChangedEventArgs>();
        document.Changed += (_, change) => events.Add(change);
        return events;
    }
}
