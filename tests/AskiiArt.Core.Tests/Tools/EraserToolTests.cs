// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Eraser;

namespace AskiiArt.Core.Tests.Tools;

public sealed class EraserToolTests
{
    private readonly EditingSession _session;
    private readonly EraserTool _tool;

    public EraserToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3));
        TestCells.Paint(Layer, "abcdef", "ghijkl", "mnopqr");
        _tool = new EraserTool(_session);
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void AClick_EmptiesOneCell_NotASpace()
    {
        Press(2, 1);
        Release(2, 1);

        Assert.Equal(["abcdef", "gh·jkl", "mnopqr"], TestCells.Read(Layer));
        Assert.True(Layer.GetCell(new CellPoint(2, 1)).IsEmpty);
    }

    [Fact]
    public void AFastDrag_LeavesNoGaps_AndErasesAsItGoes()
    {
        Press(0, 0);

        Move(5, 0);
        Assert.Equal(["······", "ghijkl", "mnopqr"], TestCells.Read(Layer));

        Move(5, 2);
        Release(5, 2);
        Assert.Equal(["······", "ghijk·", "mnopq·"], TestCells.Read(Layer));
    }

    [Fact]
    public void ADiagonalDrag_TouchesEveryCellAlongIt()
    {
        Press(0, 0);
        Move(2, 2);
        Release(2, 2);

        Assert.Equal(["·bcdef", "g·ijkl", "mn·pqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void OneStroke_IsOneUndoStep_NamedErase()
    {
        Press(0, 0);
        Move(3, 0);
        Move(3, 2);
        Release(3, 2);

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Erase, History.GetStepNameKey(0));

        History.Undo();
        Assert.Equal(["abcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));

        History.Redo();
        Assert.Equal(["····ef", "ghi·kl", "mno·qr"], TestCells.Read(Layer));
    }

    [Fact]
    public void TwoStrokes_AreTwoUndoSteps()
    {
        Press(0, 0);
        Release(0, 0);
        Press(1, 0);
        Release(1, 0);

        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void MovingWithoutPressing_ErasesNothing()
    {
        Move(1, 1);
        Move(2, 2);

        Assert.False(History.CanUndo);
    }

    [Fact]
    public void MovingAfterTheStrokeEnded_ErasesNothing()
    {
        Press(0, 0);
        Release(0, 0);

        Move(3, 0);

        Assert.Equal(["·bcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void GoingOffTheCanvas_ErasesUpToTheEdge_AndComingBackContinues()
    {
        Press(4, 0);
        Move(9, 0);
        Move(9, 2);
        Move(4, 2);
        Release(4, 2);

        Assert.Equal(["abcd··", "ghijkl", "mnop··"], TestCells.Read(Layer));
    }

    [Fact]
    public void AStrokeCanStartOutsideTheCanvas()
    {
        Press(-3, 1);
        Move(1, 1);
        Release(1, 1);

        Assert.Equal(["abcdef", "··ijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ALockedOrHiddenLayer_IsLeftAlone(bool isLocked, bool isHidden)
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with
        {
            IsLocked = isLocked,
            IsVisible = !isHidden
        }));
        var stepsBefore = History.UndoCount;

        Press(0, 0);
        Move(3, 0);
        Release(3, 0);

        Assert.Equal(["abcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(stepsBefore, History.UndoCount);
    }

    [Fact]
    public void Esc_TakesTheStrokeBack_AndLeavesNoStep()
    {
        Press(0, 0);
        Move(3, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(["abcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(0, History.StepCount);

        Move(5, 0);
        Release(5, 0);
        Assert.Equal(0, History.StepCount);
    }

    [Fact]
    public void Esc_AfterUndoingDuringTheStroke_LeavesEarlierStepsAlone()
    {
        History.Execute(new AddLayerCommand(1, new LayerProperties("Earlier")));
        Press(0, 0);
        History.Undo();

        _tool.KeyPressed(ToolKey.Escape, ToolModifiers.None);

        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void Esc_WithNoStroke_IsntUsed()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void AStrokeThatErasedNothing_LeavesNoStep_AndEscChangesNothingElse()
    {
        TestCells.Paint(Layer, "······", "ghijkl", "mnopqr");
        History.Execute(new AddLayerCommand(1, new LayerProperties("Earlier")));
        _session.SetActiveLayer(_session.Document.ActiveFrame.Layers[0].Id);
        Press(0, 0);

        _tool.KeyPressed(ToolKey.Escape, ToolModifiers.None);

        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void BeingDeactivated_KeepsWhatWasErased_AndEndsTheStroke()
    {
        Press(0, 0);

        _tool.Deactivate();
        Move(3, 0);

        Assert.Equal(["·bcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void TheEraser_TakesNoText_AndLetsSpacePan()
    {
        Assert.False(_tool.TextEntered("a"));
        Assert.False(_tool.IsTakingText);
        Assert.True(_tool.AllowsSpacePanning);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 0, 5, 2)]
    [InlineData(5, 2, 0, 0)]
    [InlineData(-4, 7, 9, -3)]
    [InlineData(2, 0, 2, 6)]
    public void ALine_RunsEndToEnd_WithEachCellTouchingTheLast(int fromX, int fromY, int toX, int toY)
    {
        var line = CellLine.Between(new CellPoint(fromX, fromY), new CellPoint(toX, toY)).ToList();

        Assert.Equal(new CellPoint(fromX, fromY), line[0]);
        Assert.Equal(new CellPoint(toX, toY), line[^1]);
        foreach (var (previous, next) in line.Zip(line.Skip(1)))
        {
            Assert.True(Math.Abs(next.X - previous.X) <= 1);
            Assert.True(Math.Abs(next.Y - previous.Y) <= 1);
        }
    }

    private void Press(int x, int y) => _tool.PointerPressed(Input(x, y));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
