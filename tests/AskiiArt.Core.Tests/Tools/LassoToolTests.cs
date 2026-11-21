// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Tools;

public sealed class LassoToolTests
{
    private readonly EditingSession _session = new(TestCells.CreateDocument(5, 4));
    private readonly LassoTool _tool;

    public LassoToolTests()
    {
        _tool = new LassoTool(TestTools.Context(_session));
    }

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Fact]
    public void ADrag_SelectsTheCellsItGoesRound_JoiningItsEndToItsStart()
    {
        Press(0.25, 0.25);
        Move(3.75, 0.25);
        Move(3.75, 2.75);
        Release(0.25, 2.75);

        Assert.Equal(["####·", "####·", "####·", "·····"], Selected);
        Assert.Equal(1, _session.History.UndoCount);
        Assert.Equal(UndoStepNames.Lasso, _session.History.GetStepNameKey(0));
    }

    [Fact]
    public void WhileDrawing_TheOverlayShowsThePath()
    {
        Press(0.25, 0.25);
        Move(3.75, 0.25);
        Move(3.75, 2.75);

        Assert.Equal([new CanvasPoint(0.25, 0.25), new CanvasPoint(3.75, 0.25), new CanvasPoint(3.75, 2.75)],
            _tool.Overlay.Path);
        Assert.True(_session.Selection.IsEmpty);

        Release(3.75, 2.75);

        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void WhileDrawing_TheOverlayPreviewsWhatLettingGoWouldSelect_AndHowThePathWouldClose()
    {
        Press(0.25, 0.25);
        Move(3.75, 0.25);
        Move(3.75, 2.75);
        Move(0.25, 2.75);

        var overlay = _tool.Overlay;
        Assert.Equal(["####·", "####·", "####·", "·····"], TestMasks.Describe(overlay.SelectionPreview!.Result));
        Assert.True(overlay.ShowsPathClosing);
        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void TinyMoves_DontAddToThePath()
    {
        Press(1.5, 1.5);
        Move(1.55, 1.52);

        Assert.Null(_tool.Overlay.Path);
    }

    [Fact]
    public void ShiftHeldAtThePress_AddsToTheSelection()
    {
        SelectionEdits.SelectAll(_session);
        SelectionEdits.Select(_session, SelectionMask.Rectangle(_session.Document.CanvasSize, new CellRect(4, 3, 1, 1)),
            UndoStepNames.RectangleSelect);

        _tool.PointerPressed(Input(0.25, 0.25, ToolModifiers.Shift));
        Move(1.75, 0.25);
        Release(1.75, 1.75);

        Assert.Equal(["##···", "·#···", "·····", "····#"], Selected);
    }

    [Fact]
    public void AClick_Deselects_WhenReplacing()
    {
        SelectionEdits.SelectAll(_session);

        Press(1.5, 1.5);
        Release(1.5, 1.5);

        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void AClick_ChangesNothing_WhenAdding()
    {
        SelectionEdits.SelectAll(_session);

        _tool.PointerPressed(Input(1.5, 1.5, ToolModifiers.Shift));
        Release(1.5, 1.5);

        Assert.Equal(1, _session.History.UndoCount);
    }

    [Fact]
    public void EscDuringADrag_CancelsIt()
    {
        Press(0.25, 0.25);
        Move(3.75, 0.25);
        Move(3.75, 2.75);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Release(0.25, 2.75);

        Assert.False(_session.History.CanUndo);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void EscWithoutADrag_IsLeftToTheApp()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void Deactivating_CancelsADrag()
    {
        Press(0.25, 0.25);
        Move(3.75, 0.25);

        _tool.Deactivate();
        Release(3.75, 2.75);

        Assert.False(_session.History.CanUndo);
    }

    [Fact]
    public void MovesWithoutADrag_DrawNothing()
    {
        Move(2, 2);

        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    private static ToolPointerInput Input(double x, double y, ToolModifiers modifiers = ToolModifiers.None)
    {
        var cell = new CellPoint((int)Math.Floor(x), (int)Math.Floor(y));
        return new ToolPointerInput(cell, x - cell.X, y - cell.Y, modifiers);
    }

    private void Press(double x, double y) => _tool.PointerPressed(Input(x, y));

    private void Move(double x, double y) => _tool.PointerMoved(Input(x, y));

    private void Release(double x, double y) => _tool.PointerReleased(Input(x, y));
}
