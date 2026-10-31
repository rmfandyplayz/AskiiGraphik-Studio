// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class ResizeCanvasCommandTests
{
    [Fact]
    public void Shrinking_CropsEveryLayer_AndUndoRestoresTheCroppedCells()
    {
        var document = TestCells.CreateDocument(4, 3);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        TestCells.Paint(bottom, "abcd", "efgh", "ijkl");
        TestCells.Paint(top, "W··X", "····", "Y··Z");
        var before = TestCommands.Snapshot(document);
        var history = new CommandHistory(document);

        history.Execute(new ResizeCanvasCommand(new CanvasSize(2, 1), ResizeAnchor.Center));

        Assert.Equal(new CanvasSize(2, 1), document.CanvasSize);
        Assert.Equal(["fg"], TestCells.Read(bottom));
        Assert.Equal(["··"], TestCells.Read(top));

        history.Undo();
        Assert.Equal(before, TestCommands.Snapshot(document));

        history.Redo();
        Assert.Equal(["fg"], TestCells.Read(bottom));
    }

    [Fact]
    public void Growing_AddsEmptyCellsOnTheSidesAwayFromTheAnchor_AndUndoShrinksBack()
    {
        var document = TestCells.CreateDocument(2, 1);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "ab");
        var history = new CommandHistory(document);

        history.Execute(new ResizeCanvasCommand(new CanvasSize(3, 2), ResizeAnchor.BottomRight));

        Assert.Equal(["···", "·ab"], TestCells.Read(layer));
        history.Undo();
        Assert.Equal(["ab"], TestCells.Read(layer));
    }

    [Fact]
    public void EstimatedMemory_CountsOnlyCroppedCellsThatWereOccupied()
    {
        var document = TestCells.CreateDocument(10, 10);
        TestCells.Paint(document.ActiveFrame.Layers[0], "·········x");
        var empty = new ResizeCanvasCommand(new CanvasSize(10, 10), ResizeAnchor.TopLeft);
        empty.Apply(document);
        var crop = new ResizeCanvasCommand(new CanvasSize(5, 5), ResizeAnchor.TopLeft);

        crop.Apply(document);

        Assert.True(crop.EstimatedMemoryInBytes > empty.EstimatedMemoryInBytes);
        Assert.True(crop.EstimatedMemoryInBytes < 200);
    }

    [Fact]
    public void Constructor_RejectsAnUnknownAnchor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResizeCanvasCommand(new CanvasSize(2, 2), (ResizeAnchor)42));
    }
}
