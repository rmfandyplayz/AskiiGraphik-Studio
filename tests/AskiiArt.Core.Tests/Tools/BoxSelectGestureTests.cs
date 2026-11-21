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

/// <summary>
/// The combine modes and the keys that shape the box, which Rectangle Select and Ellipse Select share. Cells here are
/// half as wide as they're tall, so a box that looks square has twice as many columns as rows.
/// </summary>
public sealed class BoxSelectGestureTests
{
    private const ToolModifiers Shift = ToolModifiers.Shift;
    private const ToolModifiers Alt = ToolModifiers.Alt;

    private readonly EditingSession _session = new(TestCells.CreateDocument(12, 7));
    private readonly ToolContext _context;
    private readonly RectangleSelectTool _tool;

    public BoxSelectGestureTests()
    {
        _context = TestTools.Context(_session, cellWidthToHeight: 0.5);
        _tool = new RectangleSelectTool(_context);
    }

    private CanvasSize Size => _session.Document.CanvasSize;

    private CommandHistory History => _session.History;

    [Fact]
    public void ShiftHeldAtThePress_AddsTheRectangleToTheSelection()
    {
        Select(new CellRect(0, 0, 2, 2));

        Drag((1, 1, Shift), (3, 2, Shift));

        Assert.Equal(Union(new CellRect(0, 0, 2, 2), new CellRect(1, 1, 3, 2)), _session.Selection.Mask);
        Assert.Equal(2, History.UndoCount);
        Assert.Equal(UndoStepNames.RectangleSelect, History.GetStepNameKey(1));
    }

    [Fact]
    public void AltHeldAtThePress_TakesTheRectangleAway()
    {
        Select(new CellRect(0, 0, 4, 2));

        Drag((1, 0, Alt), (2, 3, Alt));

        Assert.Equal(["#··#········", "#··#········", "············", "············", "············", "············",
            "············"], TestMasks.Describe(_session.Selection.Mask));
    }

    [Fact]
    public void ShiftAndAltHeldAtThePress_KeepOnlyWhereTheyOverlap()
    {
        Select(new CellRect(0, 0, 4, 2));

        Drag((2, 1, Shift | Alt), (5, 3, Shift | Alt));

        Assert.Equal(SelectionMask.Rectangle(Size, new CellRect(2, 1, 2, 1)), _session.Selection.Mask);
    }

    [Fact]
    public void TheKeysAtThePress_PickTheModeForTheWholeDrag()
    {
        Select(new CellRect(0, 0, 2, 2));

        Drag((1, 1, Shift), (3, 2, ToolModifiers.None));

        Assert.Equal(Union(new CellRect(0, 0, 2, 2), new CellRect(1, 1, 3, 2)), _session.Selection.Mask);
    }

    [Fact]
    public void WithNoKeysHeld_TheModeChosenInToolOptionsIsUsed()
    {
        _context.Settings.For(SelectionToolOptions.Group).Set(SelectionToolOptions.Mode, "add");
        Select(new CellRect(0, 0, 2, 2));

        Drag((1, 1, ToolModifiers.None), (3, 2, ToolModifiers.None));

        Assert.Equal(Union(new CellRect(0, 0, 2, 2), new CellRect(1, 1, 3, 2)), _session.Selection.Mask);
    }

    [Fact]
    public void WhileAdding_TheOverlayShowsTheSelectionItWouldMake()
    {
        Select(new CellRect(0, 0, 2, 2));

        _tool.PointerPressed(Input(1, 1, Shift));
        _tool.PointerMoved(Input(3, 2, Shift));

        Assert.Equal(Union(new CellRect(0, 0, 2, 2), new CellRect(1, 1, 3, 2)), _tool.Overlay.SelectionPreview?.Result);
    }

    [Theory]
    [InlineData(Shift)]
    [InlineData(Alt)]
    [InlineData(Shift | Alt)]
    public void AClick_WhenNotReplacing_ChangesNothing(ToolModifiers modifiers)
    {
        Select(new CellRect(0, 0, 2, 2));
        var before = _session.Selection.Mask;

        Drag((5, 5, modifiers), (5, 5, modifiers));

        Assert.Same(before, _session.Selection.Mask);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void AClick_InAModeChosenInToolOptions_ChangesNothing()
    {
        _context.Settings.For(SelectionToolOptions.Group).Set(SelectionToolOptions.Mode, "subtract");
        Select(new CellRect(0, 0, 2, 2));

        Drag((5, 5, ToolModifiers.None), (5, 5, ToolModifiers.None));

        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ShiftPressedDuringTheDrag_MakesTheBoxLookSquare()
    {
        _tool.PointerPressed(Input(0, 0, ToolModifiers.None));
        _tool.PointerMoved(Input(3, 3, Shift));

        Assert.Equal(Rectangle(0, 0, 8, 4), _tool.Overlay.SelectionPreview?.Result);

        _tool.PointerMoved(Input(9, 1, Shift));

        Assert.Equal(Rectangle(0, 0, 10, 5), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void ShiftDuringADragUpAndLeft_GrowsTheBoxUpAndLeft()
    {
        _tool.PointerPressed(Input(11, 6, ToolModifiers.None));
        _tool.PointerMoved(Input(10, 5, Shift));

        Assert.Equal(Rectangle(8, 5, 4, 2), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void AltPressedDuringTheDrag_DrawsFromTheFirstCellOutwards()
    {
        _tool.PointerPressed(Input(5, 3, ToolModifiers.None));
        _tool.PointerMoved(Input(7, 4, Alt));

        Assert.Equal(Rectangle(3, 2, 5, 3), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void ShiftAndAltDuringTheDrag_MakeASquareAroundTheFirstCell_WithOddSides()
    {
        _tool.PointerPressed(Input(5, 3, ToolModifiers.None));
        _tool.PointerMoved(Input(7, 3, Shift | Alt));

        Assert.Equal(Rectangle(3, 2, 5, 3), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void AKeyHeldSinceThePress_DoesntShapeTheBox_UntilItsPressedAgain()
    {
        _tool.PointerPressed(Input(0, 0, Shift));
        _tool.PointerMoved(Input(3, 1, Shift));

        Assert.Equal(Rectangle(0, 0, 4, 2), _tool.Overlay.SelectionPreview?.Result);

        _tool.ModifiersChanged(ToolModifiers.None);
        _tool.ModifiersChanged(Shift);

        Assert.Equal(Rectangle(0, 0, 4, 2), _tool.Overlay.SelectionPreview?.Result);
        _tool.PointerMoved(Input(3, 3, Shift));
        Assert.Equal(Rectangle(0, 0, 8, 4), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void PressingOrLettingGoOfAKey_ReshapesTheBox_WithoutMoving()
    {
        _tool.PointerPressed(Input(5, 3, ToolModifiers.None));
        _tool.PointerMoved(Input(6, 4, ToolModifiers.None));

        _tool.ModifiersChanged(Alt);
        Assert.Equal(Rectangle(4, 2, 3, 3), _tool.Overlay.SelectionPreview?.Result);

        _tool.ModifiersChanged(ToolModifiers.None);
        Assert.Equal(Rectangle(5, 3, 2, 2), _tool.Overlay.SelectionPreview?.Result);
    }

    [Fact]
    public void KeysPressedDuringTheDrag_ShapeWhatItSelects()
    {
        Drag((5, 3, ToolModifiers.None), (6, 4, Alt));

        Assert.Equal(Rectangle(4, 2, 3, 3), _session.Selection.Mask);
    }

    [Fact]
    public void ModifiersChanging_WithoutADrag_DoNothing()
    {
        _tool.ModifiersChanged(Shift);

        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void EllipseSelect_SelectsTheEllipseThatFitsTheBox()
    {
        var tool = new EllipseSelectTool(_context);

        tool.PointerPressed(Input(0, 0, ToolModifiers.None));
        tool.PointerMoved(Input(3, 3, Shift));
        tool.PointerReleased(Input(3, 3, Shift));

        Assert.Equal(SelectionShapes.Ellipse(Size, new CellRect(0, 0, 8, 4)), _session.Selection.Mask);
        Assert.Equal(UndoStepNames.EllipseSelect, History.GetStepNameKey(0));
    }

    [Fact]
    public void EllipseSelect_ClickedWhileReplacing_Deselects()
    {
        Select(new CellRect(0, 0, 2, 2));
        var tool = new EllipseSelectTool(_context);

        tool.PointerPressed(Input(5, 5, ToolModifiers.None));
        tool.PointerReleased(Input(5, 5, ToolModifiers.None));

        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void EllipseSelect_EscCancelsADrag()
    {
        var tool = new EllipseSelectTool(_context);
        tool.PointerPressed(Input(0, 0, ToolModifiers.None));
        tool.PointerMoved(Input(4, 4, ToolModifiers.None));

        Assert.True(tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        tool.PointerReleased(Input(4, 4, ToolModifiers.None));

        Assert.False(History.CanUndo);
        Assert.Equal(ToolOverlay.None, tool.Overlay);
    }

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);

    private void Drag((int X, int Y, ToolModifiers Modifiers) from, (int X, int Y, ToolModifiers Modifiers) to)
    {
        _tool.PointerPressed(Input(from.X, from.Y, from.Modifiers));
        _tool.PointerMoved(Input(to.X, to.Y, to.Modifiers));
        _tool.PointerReleased(Input(to.X, to.Y, to.Modifiers));
    }

    private void Select(CellRect rect) =>
        SelectionEdits.Select(_session, SelectionMask.Rectangle(Size, rect), UndoStepNames.RectangleSelect);

    private SelectionMask Rectangle(int left, int top, int width, int height) =>
        SelectionMask.Rectangle(Size, new CellRect(left, top, width, height));

    private SelectionMask Union(CellRect first, CellRect second) =>
        Rectangle(first).CombinedWith(Rectangle(second), SelectionCombineMode.Add);

    private SelectionMask Rectangle(CellRect rect) => SelectionMask.Rectangle(Size, rect);
}
