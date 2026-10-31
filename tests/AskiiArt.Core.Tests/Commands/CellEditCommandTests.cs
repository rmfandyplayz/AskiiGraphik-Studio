// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class CellEditCommandTests
{
    [Fact]
    public void Builder_RecordsEachCellsBeforeAndAfter()
    {
        var document = TestCells.CreateDocument(3, 2);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "abc");
        var builder = new CellEditBuilder(document, layer.Id);

        builder.Set(new CellPoint(1, 0), TestCells.Glyph('X'));
        builder.Set(new CellPoint(0, 1), TestCells.Glyph('Y'));
        var command = builder.Build();

        Assert.NotNull(command);
        Assert.Equal(layer.Id, command.LayerId);
        Assert.Equal(
            [new CellEdit(1, TestCells.Glyph('b'), TestCells.Glyph('X')), new CellEdit(3, Cell.Empty, TestCells.Glyph('Y'))],
            command.Edits);
        Assert.Equal(["abc", "···"], TestCells.Read(layer));
    }

    [Fact]
    public void Builder_KeepsTheFirstBeforeAndTheLastAfter()
    {
        var document = TestCells.CreateDocument(2, 1);
        var builder = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);

        builder.Set(new CellPoint(0, 0), TestCells.Glyph('1'));
        builder.Set(new CellPoint(0, 0), TestCells.Glyph('2'));

        Assert.Equal(1, builder.Count);
        Assert.Equal([new CellEdit(0, Cell.Empty, TestCells.Glyph('2'))], builder.Build()!.Edits);
    }

    [Fact]
    public void Builder_DropsCellsSetToWhatTheyAlreadyHold_AndReturnsNullWhenNothingChanges()
    {
        var document = TestCells.CreateDocument(2, 1);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "a");
        var builder = new CellEditBuilder(document, layer.Id);

        builder.Set(new CellPoint(0, 0), TestCells.Glyph('a'));
        builder.Set(new CellPoint(1, 0), Cell.Empty);

        Assert.Null(builder.Build());
    }

    [Fact]
    public void Builder_RejectsPointsOutsideTheCanvasAndUnknownLayers()
    {
        var document = TestCells.CreateDocument(2, 2);
        var builder = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Set(new CellPoint(2, 0), TestCells.Glyph('x')));
        Assert.Throws<KeyNotFoundException>(() => new CellEditBuilder(document, Guid.NewGuid()));
    }

    [Fact]
    public void ApplyAndRevert_WriteTheAfterAndBeforeValues()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "a·c");
        var command = TestCommands.Paint(document, layer.Id, "·b·");

        command.Apply(document);
        Assert.Equal(["·b·"], TestCells.Read(layer));

        command.Revert(document);
        Assert.Equal(["a·c"], TestCells.Read(layer));
    }

    [Fact]
    public void Apply_ToALayerThatNoLongerExists_ThrowsWithoutChangingAnything()
    {
        var document = TestCells.CreateDocument(2, 1);
        var extra = TestCells.AddLayer(document);
        var command = TestCommands.Paint(document, extra.Id, "x");
        document.ActiveFrame.RemoveLayer(extra.Id);

        Assert.Throws<KeyNotFoundException>(() => command.Apply(document));
    }

    [Fact]
    public void TryMergeWith_KeepsEachCellsFirstBeforeAndLastAfter()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layer = document.ActiveFrame.Layers[0];
        var first = TestCommands.Paint(document, layer.Id, "ab");
        first.Apply(document);
        var second = TestCommands.Paint(document, layer.Id, "~Bc");
        second.Apply(document);

        Assert.True(first.TryMergeWith(second, out var merged));

        var mergedEdits = Assert.IsType<CellEditCommand>(merged).Edits;
        Assert.Equal(
            [
                new CellEdit(0, Cell.Empty, TestCells.Glyph('a')),
                new CellEdit(1, Cell.Empty, TestCells.Glyph('B')),
                new CellEdit(2, Cell.Empty, TestCells.Glyph('c')),
            ],
            mergedEdits);
        merged.Revert(document);
        Assert.Equal(["···"], TestCells.Read(layer));
    }

    [Fact]
    public void TryMergeWith_DropsCellsThatEndUpUnchanged()
    {
        var document = TestCells.CreateDocument(2, 1);
        var layer = document.ActiveFrame.Layers[0];
        var type = TestCommands.Paint(document, layer.Id, "a");
        type.Apply(document);
        var erase = TestCommands.Paint(document, layer.Id, "·");
        erase.Apply(document);

        Assert.True(type.TryMergeWith(erase, out var merged));

        Assert.Empty(Assert.IsType<CellEditCommand>(merged).Edits);
    }

    [Fact]
    public void TryMergeWith_RefusesOtherLayersAndOtherCommands()
    {
        var document = TestCells.CreateDocument(2, 1);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        var onBottom = TestCommands.Paint(document, bottom.Id, "a");

        Assert.False(onBottom.TryMergeWith(TestCommands.Paint(document, top.Id, "b"), out _));
        Assert.False(onBottom.TryMergeWith(new MoveLayerCommand(top.Id, 0), out _));
    }

    [Fact]
    public void EstimatedMemory_GrowsWithTheNumberOfCells()
    {
        var document = TestCells.CreateDocument(10, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;

        var small = TestCommands.Paint(document, layerId, "a").EstimatedMemoryInBytes;
        var large = TestCommands.Paint(document, layerId, "abcdefghij").EstimatedMemoryInBytes;

        Assert.True(large > small);
    }
}
