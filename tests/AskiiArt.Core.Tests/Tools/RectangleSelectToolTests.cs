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

public sealed class RectangleSelectToolTests
{
    private readonly EditingSession _session = new(TestCells.CreateDocument(5, 4));
    private readonly RectangleSelectTool _tool;
    private int _overlayChanges;

    public RectangleSelectToolTests()
    {
        _tool = new RectangleSelectTool(_session);
        _tool.OverlayChanged += (_, _) => _overlayChanges++;
    }

    private CommandHistory History => _session.History;

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Fact]
    public void ADrag_SelectsTheRectangleBetweenItsEnds_BothIncluded()
    {
        Press(1, 1);
        Move(2, 1);
        Move(3, 2);
        Release(3, 2);

        Assert.Equal(["·····", "·###·", "·###·", "·····"], Selected);
    }

    [Fact]
    public void ADragUpAndLeft_SelectsTheSameRectangle()
    {
        Press(3, 2);
        Move(1, 1);
        Release(1, 1);

        Assert.Equal(["·····", "·###·", "·###·", "·····"], Selected);
    }

    [Fact]
    public void ADrag_IsOneUndoStep_NamedRectangleSelect_ThatLeavesTheDocumentSaved()
    {
        Press(0, 0);
        Move(1, 0);
        Move(2, 1);
        Release(2, 1);

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.RectangleSelect, History.GetStepNameKey(0));
        Assert.False(History.HasUnsavedChanges);

        History.Undo();
        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void ADrag_ReplacesTheSelection()
    {
        SelectionEdits.SelectAll(_session);

        Press(0, 0);
        Release(1, 0);

        Assert.Equal(["##···", "·····", "·····", "·····"], Selected);
    }

    [Fact]
    public void ADragPastTheCanvasEdge_StopsAtTheEdge()
    {
        Press(3, 2);
        Move(9, 9);
        Release(9, 9);

        Assert.Equal(["·····", "·····", "···##", "···##"], Selected);
    }

    [Fact]
    public void ADragWhollyOffTheCanvas_SelectsNothing()
    {
        SelectionEdits.SelectAll(_session);

        Press(-3, -3);
        Release(-1, -2);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(UndoStepNames.Deselect, History.GetStepNameKey(1));
    }

    [Fact]
    public void AClick_SelectsNothing()
    {
        SelectionEdits.SelectAll(_session);

        Press(2, 2);
        Release(2, 2);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(2, History.UndoCount);
        Assert.Equal(UndoStepNames.Deselect, History.GetStepNameKey(1));
    }

    [Fact]
    public void AClick_WithNothingSelected_MakesNoStep()
    {
        Press(2, 2);
        Release(2, 2);

        Assert.False(History.CanUndo);
    }

    [Fact]
    public void ADragBackToItsFirstCell_SelectsThatCell()
    {
        Press(2, 2);
        Move(3, 2);
        Move(2, 2);
        Release(2, 2);

        Assert.Equal(["·····", "·····", "··#··", "·····"], Selected);
    }

    [Fact]
    public void WhileDragging_TheOverlayShowsTheRectangle_AndTheSelectionWaits()
    {
        Press(1, 1);
        Assert.Null(_tool.Overlay.PendingSelection);

        Move(2, 2);

        var pending = _tool.Overlay.PendingSelection;
        Assert.NotNull(pending);
        Assert.Equal(["·····", "·##··", "·##··", "·····"], TestMasks.Describe(pending));
        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(1, _overlayChanges);

        Release(2, 2);

        Assert.Null(_tool.Overlay.PendingSelection);
        Assert.Equal(2, _overlayChanges);
    }

    [Fact]
    public void EscDuringADrag_CancelsIt_AndLeavesTheSelectionAlone()
    {
        var before = SelectionMask.Rectangle(_session.Document.CanvasSize, new CellRect(0, 0, 1, 1));
        SelectionEdits.Select(_session, before, UndoStepNames.RectangleSelect);
        Press(1, 1);
        Move(3, 3);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Release(3, 3);

        Assert.Same(before, _session.Selection.Mask);
        Assert.Null(_tool.Overlay.PendingSelection);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void EscWithoutADrag_IsLeftToTheApp()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Delete, ToolModifiers.None));
        Assert.False(_tool.TextEntered("a"));
    }

    [Fact]
    public void Deactivating_CancelsADrag()
    {
        Press(1, 1);
        Move(3, 3);

        _tool.Deactivate();
        Release(3, 3);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Null(_tool.Overlay.PendingSelection);
        Assert.False(History.CanUndo);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ALockedOrHiddenLayer_CanStillBeSelectedOn(bool isLocked, bool isHidden)
    {
        var layer = _session.ActiveLayer;
        History.Execute(new SetLayerPropertiesCommand(layer.Id, layer.Properties with
        {
            IsLocked = isLocked,
            IsVisible = !isHidden
        }));

        Press(0, 0);
        Release(1, 1);

        Assert.Equal(["##···", "##···", "·····", "·····"], Selected);
    }

    [Fact]
    public void ModifiersAreIgnored_ForNow()
    {
        _tool.PointerPressed(Input(0, 0, ToolModifiers.Shift));
        _tool.PointerReleased(Input(1, 0, ToolModifiers.Alt));

        Assert.Equal(["##···", "·····", "·····", "·····"], Selected);
    }

    [Fact]
    public void Panning_UsesSpaceOnly()
    {
        Assert.True(_tool.AllowsSpacePanning);
        Assert.False(_tool.PansWithPrimaryButton);
        Assert.False(_tool.IsTakingText);
    }

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);

    private void Press(int x, int y) => _tool.PointerPressed(Input(x, y));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));
}
