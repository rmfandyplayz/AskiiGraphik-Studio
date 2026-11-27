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

public sealed class PolygonalLassoToolTests
{
    private static readonly string[] _square = ["####·", "####·", "####·", "·····"];

    // A marker half a cell across and down, as at a zoom where cells are twice the marker's radius.
    private static readonly ToolMarkerReach _reach = new(0.5, 0.5);

    private readonly EditingSession _session = new(TestCells.CreateDocument(5, 4));
    private readonly PolygonalLassoTool _tool;

    public PolygonalLassoToolTests()
    {
        _tool = new PolygonalLassoTool(TestTools.Context(_session));
    }

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Fact]
    public void ClickingTheFirstCornerAgain_SelectsTheShape_AsOneStep()
    {
        ClickTheSquaresCorners();
        Click(0.5, 0.5);

        Assert.Equal(_square, Selected);
        Assert.False(_tool.IsDrawing);
        Assert.Equal(1, _session.History.UndoCount);
        Assert.Equal(UndoStepNames.PolygonalLasso, _session.History.GetStepNameKey(0));
    }

    [Fact]
    public void ADoubleClick_AddsItsCorner_AndFinishes()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        Click(0.25, 2.75);
        _tool.PointerPressed(Input(0.25, 2.75, clickCount: 2));

        Assert.Equal(_square, Selected);
        Assert.False(_tool.IsDrawing);
    }

    [Fact]
    public void Enter_Finishes()
    {
        ClickTheSquaresCorners();

        Assert.True(_tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));

        Assert.Equal(_square, Selected);
    }

    [Fact]
    public void Backspace_TakesTheLastCornerAway()
    {
        ClickTheSquaresCorners();
        Click(4.5, 3.5);

        Assert.True(_tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None));
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(_square, Selected);
    }

    // Undo while drawing is the tool's, and does what Backspace does (ADR 0083).
    [Fact]
    public void Undo_TakesTheLastCornerAway_WhileAShapeIsDrawn()
    {
        Assert.False(_tool.TakesUndo);
        ClickTheSquaresCorners();
        Click(4.5, 3.5);
        Assert.True(_tool.TakesUndo);

        _tool.Undo();
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(_square, Selected);
        Assert.False(_tool.TakesUndo);
    }

    [Fact]
    public void BackspaceOnTheOnlyCorner_Cancels()
    {
        Click(1.5, 1.5);

        _tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None);

        Assert.False(_tool.IsDrawing);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
        Assert.False(_session.History.CanUndo);
    }

    [Fact]
    public void Esc_Cancels_LeavingTheSelectionAlone()
    {
        SelectionEdits.SelectAll(_session);
        ClickTheSquaresCorners();

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.False(_tool.IsDrawing);
        Assert.Equal(1, _session.History.UndoCount);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void KeysWithoutAShape_AreLeftToTheApp()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None));
    }

    [Fact]
    public void OtherKeysWhileDrawing_AreLeftToTheApp()
    {
        Click(1.5, 1.5);

        Assert.False(_tool.KeyPressed(ToolKey.Delete, ToolModifiers.None));
    }

    [Fact]
    public void TheOverlay_ShowsTheCorners_AndALineToThePointer()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        _tool.PointerMoved(Input(2.5, 2.5));

        Assert.Equal([new CanvasPoint(0.25, 0.25), new CanvasPoint(3.75, 0.25), new CanvasPoint(2.5, 2.5)],
            _tool.Overlay.Path);
    }

    [Fact]
    public void AClickInTheFirstCornersCell_ButOffItsMarker_AddsACorner()
    {
        ClickTheSquaresCorners();
        Click(0.95, 0.95);

        Assert.True(_tool.IsDrawing);
        Assert.Equal(5, _tool.Overlay.Markers?.Count);
    }

    [Fact]
    public void AClickOnTheFirstCornersMarker_FromAnotherCell_Finishes()
    {
        Click(0.9, 0.9);
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        Click(1.2, 1.2);

        Assert.False(_tool.IsDrawing);
        Assert.Equal(1, _session.History.UndoCount);
    }

    [Fact]
    public void EachCorner_HasAMarker_TheFirstFinishingTheShape()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        _tool.PointerMoved(Input(2.5, 2.5));

        Assert.Equal(
        [
            new ToolMarker(new CanvasPoint(0.25, 0.25), ToolMarkerKind.Finish),
            new ToolMarker(new CanvasPoint(3.75, 0.25), ToolMarkerKind.Corner),
            new ToolMarker(new CanvasPoint(3.75, 2.75), ToolMarkerKind.Corner)
        ], _tool.Overlay.Markers);
    }

    [Fact]
    public void ThePointerOnTheFirstCornersMarker_LightsItUp_AndJoinsTheLineToIt()
    {
        ClickTheSquaresCorners();
        _tool.PointerMoved(Input(0.4, 0.4));

        var overlay = _tool.Overlay;
        Assert.Equal(ToolMarkerKind.FinishUnderPointer, overlay.Markers?[0].Kind);
        Assert.Equal(new CanvasPoint(0.25, 0.25), overlay.Path?[^1]);
        Assert.False(overlay.ShowsPathClosing);
        Assert.Equal(_square, TestMasks.Describe(overlay.SelectionPreview!.Result));
    }

    [Fact]
    public void WithFewerThanThreeCorners_TheFirstCornerNeverLightsUp()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        _tool.PointerMoved(Input(0.3, 0.3));

        Assert.Equal(ToolMarkerKind.Finish, _tool.Overlay.Markers?[0].Kind);
    }

    [Fact]
    public void TheOverlay_PreviewsWhatADoubleClickWhereThePointerIsWouldSelect_AndHowTheShapeWouldClose()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        _tool.PointerMoved(Input(0.25, 2.75));

        var overlay = _tool.Overlay;
        Assert.Equal(_square, TestMasks.Describe(overlay.SelectionPreview!.Result));
        Assert.True(overlay.ShowsPathClosing);
        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void WithFewerThanThreeCorners_ClickingTheFirstCornerAddsAnother()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        Click(0.5, 0.5);

        Assert.True(_tool.IsDrawing);
        Assert.Equal(4, _tool.Overlay.Path?.Count);
    }

    [Fact]
    public void FinishingWithFewerThanThreeCorners_Deselects_WhenReplacing()
    {
        SelectionEdits.SelectAll(_session);
        Click(0.25, 0.25);
        Click(3.75, 0.25);

        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void FinishingWithFewerThanThreeCorners_ChangesNothing_WhenIntersecting()
    {
        SelectionEdits.SelectAll(_session);
        _tool.PointerPressed(Input(0.25, 0.25, ToolModifiers.Shift | ToolModifiers.Alt));
        Click(3.75, 0.25);

        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(1, _session.History.UndoCount);
    }

    [Fact]
    public void AShapeWhollyOffTheCanvas_DeselectsEverything_WhenIntersecting()
    {
        SelectionEdits.SelectAll(_session);
        _tool.PointerPressed(Input(-3, -3, ToolModifiers.Shift | ToolModifiers.Alt));
        Click(-1, -3);
        Click(-1, -1);

        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void TheKeysAtTheFirstClick_PickTheMode()
    {
        SelectionEdits.Select(_session, SelectionMask.Rectangle(_session.Document.CanvasSize, new CellRect(4, 3, 1, 1)),
            UndoStepNames.RectangleSelect);

        _tool.PointerPressed(Input(0.25, 0.25, ToolModifiers.Shift));
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        _tool.PointerPressed(Input(0.25, 2.75, ToolModifiers.Alt));
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(["####·", "####·", "####·", "····#"], Selected);
    }

    [Fact]
    public void Deactivating_Cancels()
    {
        ClickTheSquaresCorners();

        _tool.Deactivate();

        Assert.False(_tool.IsDrawing);
        Assert.False(_session.History.CanUndo);
    }

    [Fact]
    public void AnUndoDuringAShape_ShowsWhatFinishingWouldNowSelect_WithoutThePointerMoving()
    {
        var corner = SelectionMask.Rectangle(_session.Document.CanvasSize, new CellRect(4, 3, 1, 1));
        SelectionEdits.Select(_session, corner, UndoStepNames.RectangleSelect);
        SelectionEdits.Deselect(_session);
        _tool.PointerPressed(Input(0.25, 0.25, ToolModifiers.Shift));
        _tool.PointerReleased(Input(0.25, 0.25, ToolModifiers.Shift));
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        _tool.PointerMoved(Input(0.25, 2.75));
        var markers = _tool.Overlay.Markers;

        _session.History.Undo();

        var overlay = _tool.Overlay;
        Assert.Equal(corner, overlay.SelectionPreview!.Before);
        Assert.Equal(["####·", "####·", "####·", "····#"], TestMasks.Describe(overlay.SelectionPreview.Result));
        Assert.Same(markers, overlay.Markers);
    }

    private static ToolPointerInput Input(double x, double y, ToolModifiers modifiers = ToolModifiers.None,
        int clickCount = 1)
    {
        var cell = new CellPoint((int)Math.Floor(x), (int)Math.Floor(y));
        return new ToolPointerInput(cell, x - cell.X, y - cell.Y, modifiers, ClickCount: clickCount,
            MarkerReach: _reach);
    }

    private void ClickTheSquaresCorners()
    {
        Click(0.25, 0.25);
        Click(3.75, 0.25);
        Click(3.75, 2.75);
        Click(0.25, 2.75);
    }

    private void Click(double x, double y)
    {
        _tool.PointerPressed(Input(x, y));
        _tool.PointerReleased(Input(x, y));
    }
}
