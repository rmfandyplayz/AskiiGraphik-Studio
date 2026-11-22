// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Moving;

namespace AskiiArt.Core.Tests.Tools;

/// <summary>The Move tool taking the selection past the canvas's edges, into the zone around it (ADR 0071).</summary>
public sealed class MoveToolZoneTests
{
    private readonly EditingSession _session;
    private readonly MoveTool _tool;

    // A 6 × 3 canvas with cells half as wide as they're tall, so the zone reaches 10 columns and 5 rows past it.
    public MoveToolZoneTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3));
        TestCells.Paint(Layer, "abcdef", "ghijkl", "mnopqr");
        _tool = new MoveTool(TestTools.Context(_session, cellWidthToHeight: 0.5));
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CellRect SelectedBounds => _session.Selection.Mask.Bounds;

    [Fact]
    public void ADrag_StopsAtTheZonesOuterEdge_AndThenSlidesAlongIt()
    {
        TestTools.Select(_session, "##····", "······", "······");

        Press(0, 0);
        Move(-20, 1);
        Assert.Equal(new CellRect(-10, 1, 2, 1), SelectedBounds);

        Move(-20, 20);
        Release(-20, 20);

        Assert.Equal(new CellRect(-10, 7, 2, 1), SelectedBounds);
    }

    [Fact]
    public void TheArrows_StopAtTheZonesOuterEdge()
    {
        TestTools.Select(_session, "#·····", "······", "······");

        _tool.KeyPressed(ToolKey.FarLeft, ToolModifiers.None);
        _tool.KeyPressed(ToolKey.Left, ToolModifiers.None);
        for (var press = 0; press < 8; press++)
            _tool.KeyPressed(ToolKey.Up, ToolModifiers.None);

        Assert.Equal(new CellRect(-10, -5, 1, 1), SelectedBounds);
    }

    [Fact]
    public void WithNothingSelected_TheWholeLayerGoesAtMostTenColumnsPastTheCanvas()
    {
        Drag((0, 0), (30, 0));

        Assert.Equal(new CellRect(10, 0, 6, 3), SelectedBounds);
        Assert.Equal(["······", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void WhenTheMoveEnds_TheSelectionStaysPastTheCanvas_WhileTheCellsThereAreLost()
    {
        TestTools.Select(_session, "##····", "······", "······");
        Drag((0, 0), (-1, 0));

        _tool.Deactivate();

        Assert.Equal(new CellRect(-1, 0, 2, 1), SelectedBounds);
        Assert.Equal(["b·cdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        var tool = new MoveTool(TestTools.Context(_session, cellWidthToHeight: 0.5));
        tool.PointerPressed(Input(0, 0));
        tool.PointerMoved(Input(2, 0));
        tool.PointerReleased(Input(2, 0));
        Assert.Equal(new CellRect(1, 0, 2, 1), SelectedBounds);
        Assert.Equal(["··bdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void ASelectionAllPastTheCanvas_StillMoves()
    {
        TestTools.Select(_session, "##····", "······", "······");
        Drag((0, 0), (-5, 0));
        Assert.False(_session.Selection.Mask.HasCellsOnCanvas);

        Drag((-5, 0), (-4, 1));

        Assert.Equal(new CellRect(-4, 1, 2, 1), SelectedBounds);
        Assert.True(_tool.ShowsMoveCursorAt(new CellPoint(-3, 1)));
    }

    [Fact]
    public void APaste_KeepsItsCellsInTheZoneSelected()
    {
        _tool.Paste(PastedText.ToClip("xy")!, new CellPoint(5, 0));

        Assert.Equal(new CellRect(5, 0, 2, 1), SelectedBounds);
        Assert.Equal(new Rune('x'), Layer.GetCell(new CellPoint(5, 0)).Glyph);
    }

    [Fact]
    public void APasteWiderThanTheZone_MovesOnlyAsFarAsItStillCoversIt_AndIsSelectedAsFarAsTheZoneReaches()
    {
        // 30 cells, from 12 columns left of the canvas to 12 right of it, where the zone is 26 wide.
        _tool.Paste(PastedText.ToClip("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123")!, new CellPoint(-12, 0));
        Assert.Equal(new Rune('M'), Layer.GetCell(new CellPoint(0, 0)).Glyph);

        _tool.KeyPressed(ToolKey.FarLeft, ToolModifiers.None);
        Assert.Equal(new Rune('O'), Layer.GetCell(new CellPoint(0, 0)).Glyph);

        _tool.KeyPressed(ToolKey.FarRight, ToolModifiers.None);
        _tool.KeyPressed(ToolKey.FarRight, ToolModifiers.None);
        Assert.Equal(new Rune('K'), Layer.GetCell(new CellPoint(0, 0)).Glyph);
        Assert.Equal(new CellRect(-10, 0, 26, 1), SelectedBounds);
    }

    private void Drag((int X, int Y) from, (int X, int Y) to)
    {
        Press(from.X, from.Y);
        Move(to.X, to.Y);
        Release(to.X, to.Y);
    }

    private void Press(int x, int y) => _tool.PointerPressed(Input(x, y));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
