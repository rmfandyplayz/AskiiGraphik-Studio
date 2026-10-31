// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class LayerCommandTests
{
    [Fact]
    public void AddLayer_InsertsAnEmptyLayer_AndRedoReusesItsId()
    {
        var document = TestCells.CreateDocument(2, 2);
        var history = new CommandHistory(document);
        var add = new AddLayerCommand(0, new LayerProperties("Below"));

        history.Execute(add);
        var added = document.ActiveFrame.Layers[0];
        Assert.Equal(add.LayerId, added.Id);
        Assert.Equal("Below", added.Properties.Name);
        Assert.Equal(new CanvasSize(2, 2), added.Size);

        history.Undo();
        Assert.False(document.TryGetLayer(add.LayerId, out _));

        history.Redo();
        Assert.Equal(add.LayerId, document.ActiveFrame.Layers[0].Id);
    }

    [Fact]
    public void AddLayer_WithAnIndexPastTheTop_ThrowsAndChangesNothing()
    {
        var document = TestCells.CreateDocument(2, 2);
        var history = new CommandHistory(document);

        Assert.Throws<ArgumentOutOfRangeException>(() => history.Execute(new AddLayerCommand(2, new LayerProperties("Too high"))));

        Assert.Single(document.ActiveFrame.Layers);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void AddLayer_AfterAResize_MatchesTheCurrentCanvas()
    {
        var document = TestCells.CreateDocument(2, 2);
        var history = new CommandHistory(document);
        history.Execute(new ResizeCanvasCommand(new CanvasSize(5, 3), ResizeAnchor.TopLeft));

        var add = new AddLayerCommand(1, new LayerProperties("New"));
        history.Execute(add);

        Assert.Equal(new CanvasSize(5, 3), document.GetLayer(add.LayerId).Size);
    }

    [Fact]
    public void RemoveLayer_UndoPutsTheLayerBackWithItsCellsAndPosition()
    {
        var document = TestCells.CreateDocument(2, 1);
        var bottom = document.ActiveFrame.Layers[0];
        var middle = TestCells.AddLayer(document, "Middle");
        var top = TestCells.AddLayer(document, "Top");
        TestCells.Paint(middle, "mm");
        var before = TestCommands.Snapshot(document);
        var history = new CommandHistory(document);

        history.Execute(new RemoveLayerCommand(middle.Id));
        Assert.Equal([bottom, top], document.ActiveFrame.Layers);

        history.Undo();
        Assert.Equal(before, TestCommands.Snapshot(document));

        history.Redo();
        Assert.False(document.TryGetLayer(middle.Id, out _));
    }

    [Fact]
    public void RemoveLayer_RefusesTheLastLayer()
    {
        var document = TestCells.CreateDocument(2, 1);
        var history = new CommandHistory(document);

        Assert.Throws<InvalidOperationException>(() => history.Execute(new RemoveLayerCommand(document.ActiveFrame.Layers[0].Id)));

        Assert.Single(document.ActiveFrame.Layers);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void RemoveLayer_EstimatedMemoryIncludesTheRemovedCells()
    {
        var document = TestCells.CreateDocument(100, 100);
        var removed = TestCells.AddLayer(document);
        var command = new RemoveLayerCommand(removed.Id);

        command.Apply(document);

        Assert.True(command.EstimatedMemoryInBytes >= 100 * 100);
    }

    [Fact]
    public void MoveLayer_UndoPutsItBack()
    {
        var document = TestCells.CreateDocument(2, 1);
        var first = document.ActiveFrame.Layers[0];
        var second = TestCells.AddLayer(document);
        var third = TestCells.AddLayer(document);
        var history = new CommandHistory(document);

        history.Execute(new MoveLayerCommand(third.Id, 0));
        Assert.Equal([third, first, second], document.ActiveFrame.Layers);

        history.Undo();
        Assert.Equal([first, second, third], document.ActiveFrame.Layers);
    }

    [Fact]
    public void SetLayerProperties_UndoRestoresTheOldProperties()
    {
        var document = TestCells.CreateDocument(2, 1);
        var layer = document.ActiveFrame.Layers[0];
        var original = layer.Properties;
        var history = new CommandHistory(document);

        history.Execute(new SetLayerPropertiesCommand(layer.Id, original with { Name = "Ink", IsVisible = false, IsLocked = true }));
        Assert.Equal(new LayerProperties("Ink") { IsVisible = false, IsLocked = true }, layer.Properties);

        history.Undo();
        Assert.Same(original, layer.Properties);
    }

    [Fact]
    public void Revert_BeforeApply_Throws()
    {
        var document = TestCells.CreateDocument(2, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;

        Assert.Throws<InvalidOperationException>(() => new RemoveLayerCommand(layerId).Revert(document));
        Assert.Throws<InvalidOperationException>(() => new SetLayerPropertiesCommand(layerId, new LayerProperties("x")).Revert(document));
    }
}
