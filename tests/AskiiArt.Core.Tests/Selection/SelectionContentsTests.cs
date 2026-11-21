// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Tools;

namespace AskiiArt.Core.Tests.Selection;

/// <summary>Emptying the selected cells with <see cref="SelectionContents"/> (ADR 0065).</summary>
public sealed class SelectionContentsTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);

    private readonly EditingSession _session = new(TestCells.CreateDocument(4, 2, ColorMode.Ansi));

    public SelectionContentsTests()
    {
        TestCells.Paint(Layer, "abcd", "efgh");
        Layer.SetCell(new CellPoint(1, 0), TestCells.Glyph('B', _red, _blue));
        Layer.SetCell(new CellPoint(2, 0), Cell.FromBackground(_blue));
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void Clear_EmptiesEverySelectedCell_ColoursAndBackgroundsToo_AndKeepsTheSelection()
    {
        TestTools.Select(_session, "·##·", "·#··");
        var selection = _session.Selection.Mask;

        Assert.True(SelectionContents.CanClear(_session));
        Assert.True(SelectionContents.Clear(_session));

        Assert.Equal(["a··d", "e·gh"], TestCells.Read(Layer));
        Assert.Equal(Cell.Empty, Layer.GetCell(new CellPoint(1, 0)));
        Assert.Equal(Cell.Empty, Layer.GetCell(new CellPoint(2, 0)));
        Assert.Same(selection, _session.Selection.Mask);
    }

    [Fact]
    public void Clear_IsOneUndoStep_NamedDelete()
    {
        TestTools.Select(_session, "·##·", "·#··");
        var steps = History.UndoCount;

        SelectionContents.Clear(_session);

        Assert.Equal(steps + 1, History.UndoCount);
        Assert.Equal(UndoStepNames.DeleteSelectedCells, History.GetStepNameKey(steps));

        History.Undo();
        Assert.Equal(["aB·d", "efgh"], TestCells.Read(Layer));
        Assert.Equal(TestCells.Glyph('B', _red, _blue), Layer.GetCell(new CellPoint(1, 0)));
        Assert.Equal(Cell.FromBackground(_blue), Layer.GetCell(new CellPoint(2, 0)));
    }

    [Fact]
    public void Clear_WithNothingSelected_ChangesNothing()
    {
        Assert.False(SelectionContents.CanClear(_session));
        Assert.False(SelectionContents.Clear(_session));

        Assert.Equal(["aB·d", "efgh"], TestCells.Read(Layer));
        Assert.Equal(0, History.UndoCount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Clear_OnALockedOrHiddenLayer_ChangesNothing(bool isLocked, bool isHidden)
    {
        TestTools.Select(_session, "####", "####");
        History.Execute(new SetLayerPropertiesCommand(Layer.Id,
            Layer.Properties with { IsLocked = isLocked, IsVisible = !isHidden }));
        var steps = History.UndoCount;

        Assert.False(SelectionContents.CanClear(_session));
        Assert.False(SelectionContents.Clear(_session));

        Assert.Equal(["aB·d", "efgh"], TestCells.Read(Layer));
        Assert.Equal(steps, History.UndoCount);
    }

    [Fact]
    public void Clear_WhenTheSelectedCellsAreEmptyAlready_MakesNoStep()
    {
        TestCells.Paint(Layer, "a··d");
        TestTools.Select(_session, "·##·", "····");
        var steps = History.UndoCount;

        Assert.True(SelectionContents.CanClear(_session));
        Assert.False(SelectionContents.Clear(_session));

        Assert.Equal(steps, History.UndoCount);
    }

    [Fact]
    public void Clear_EmptiesOnlyTheActiveLayer()
    {
        var upper = TestCells.AddLayer(_session.Document);
        TestCells.Paint(upper, "wxyz", "wxyz");
        _session.SetActiveLayer(upper.Id);
        TestTools.Select(_session, "##··", "····");

        SelectionContents.Clear(_session);

        Assert.Equal(["··yz", "wxyz"], TestCells.Read(upper));
        Assert.Equal(["aB·d", "efgh"], TestCells.Read(Layer));
    }
}
