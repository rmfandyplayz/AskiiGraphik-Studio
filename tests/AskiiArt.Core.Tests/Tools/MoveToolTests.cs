// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tests.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Moving;

namespace AskiiArt.Core.Tests.Tools;

public sealed class MoveToolTests
{
    private static readonly string[] _art = ["abcdef", "ghijkl", "mnopqr"];

    private readonly EditingSession _session;
    private readonly MoveTool _tool;

    public MoveToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3));
        TestCells.Paint(Layer, _art);
        _tool = new MoveTool(TestTools.Context(_session));
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Fact]
    public void ADrag_MovesTheSelectedCells_AndTheSelection_AsOneMoveStep()
    {
        SelectTheFirstTwoCells();

        Drag((0, 0), (2, 1));

        Assert.Equal(["··cdef", "ghabkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["······", "··##··", "······"], Selected);
        Assert.Equal(2, History.UndoCount);
        Assert.Equal(UndoStepNames.Move, History.GetStepNameKey(1));
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public void AMoveAndItsUndo_ReportTheCellsTheyChange_AsTheRectThatHoldsThem()
    {
        SelectTheFirstTwoCells();
        List<LayerCellsChange> changes = [];
        _session.Document.Changed += (_, change) => changes.AddRange(change.CellChanges);

        Drag((0, 0), (2, 1));
        History.Undo();

        Assert.Equal(2, changes.Count);
        Assert.All(changes, change => Assert.Equal([new CellRect(0, 0, 4, 2)], change.Regions));
    }

    [Fact]
    public void ADragStartingAnywhere_MovesTheSelectedCells()
    {
        SelectTheFirstTwoCells();

        Drag((5, 1), (5, 2));

        Assert.Equal(["··cdef", "abijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void MovedCells_PassOverArt_WithoutHarmingIt()
    {
        SelectTheFirstTwoCells();

        Press(0, 0);
        Move(2, 0);
        Assert.Equal(["··abef", "ghijkl", "mnopqr"], TestCells.Read(Layer));

        Move(4, 0);
        Release(4, 0);

        Assert.Equal(["··cdab", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void SeveralDragsAndNudges_AreOneStep_ThatOneUndoTakesBack()
    {
        SelectTheFirstTwoCells();

        Drag((0, 0), (1, 0));
        Drag((3, 2), (4, 2));
        _tool.KeyPressed(ToolKey.Down, ToolModifiers.None);

        Assert.Equal(["··cdef", "ghabkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(2, History.UndoCount);

        History.Undo();

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(["##····", "······", "······"], Selected);
    }

    [Fact]
    public void EmptyCells_DontCoverWhatTheyLandOn()
    {
        Layer.SetCell(new CellPoint(1, 0), Cell.Empty);
        SelectTheFirstTwoCells();

        Drag((0, 0), (2, 0));

        Assert.Equal(["··adef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["··##··", "······", "······"], Selected);
    }

    [Fact]
    public void Esc_PutsTheCellsBack_AndLeavesNoStep_ThenIsntTheTools()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 1));

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(["##····", "······", "······"], Selected);
        Assert.Equal(1, History.UndoCount);
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void EscDuringADrag_PutsTheCellsBack_AndTheRestOfTheDragDoesNothing()
    {
        SelectTheFirstTwoCells();
        Press(0, 0);
        Move(2, 0);

        _tool.KeyPressed(ToolKey.Escape, ToolModifiers.None);
        Move(3, 0);
        Release(3, 0);

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void Enter_EndsTheMove_SoTheCellsReplaceWhatTheyreOn()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        Assert.True(_tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));
        Drag((0, 0), (2, 0));

        Assert.Equal(["····ab", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(3, History.UndoCount);
    }

    [Fact]
    public void AltDrag_MovesACopy_LeavingTheCellsWhereTheyWere()
    {
        SelectTheFirstTwoCells();

        Drag((0, 0), (2, 0), ToolModifiers.Alt);

        Assert.Equal(["ababef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["··##··", "······", "······"], Selected);
    }

    [Fact]
    public void AltDragDuringAMove_EndsIt_AndMovesACopyOnFromThere()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        Drag((0, 0), (0, 1), ToolModifiers.Alt);

        Assert.Equal(["··abef", "ghabkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(3, History.UndoCount);
    }

    [Fact]
    public void TheArrows_NudgeTheSelectedCellsOneCell_AsOneMove()
    {
        Select("··#···", "······", "······");

        Assert.True(_tool.KeyPressed(ToolKey.Right, ToolModifiers.None));
        Assert.True(_tool.KeyPressed(ToolKey.Down, ToolModifiers.None));

        Assert.Equal(["ab·def", "ghickl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["······", "···#··", "······"], Selected);
        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void ShiftAndAnArrow_NudgesTenCells()
    {
        var session = new EditingSession(TestCells.CreateDocument(12, 1));
        var layer = session.Document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "a···········");
        TestTools.Select(session, "#···········");
        var tool = new MoveTool(TestTools.Context(session));

        Assert.True(tool.KeyPressed(ToolKey.FarRight, ToolModifiers.None));

        Assert.Equal(["··········a·"], TestCells.Read(layer));
    }

    [Fact]
    public void ANudgeDuringADrag_IsTakenButDoesNothing()
    {
        SelectTheFirstTwoCells();
        Press(0, 0);
        Move(1, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Down, ToolModifiers.None));

        Assert.Equal(["·abdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void CellsMovedOffTheCanvas_ComeBack_WhileTheMoveLasts()
    {
        SelectTheFirstTwoCells();

        Press(3, 0);
        Move(0, 0);
        Assert.Equal(["··cdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.True(_session.Selection.IsEmpty);

        Move(4, 0);
        Release(4, 0);

        Assert.Equal(["·abdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["·##···", "······", "······"], Selected);
    }

    [Fact]
    public void CellsOffTheCanvas_AreLost_WhenTheMoveEnds()
    {
        SelectTheFirstTwoCells();
        Drag((1, 0), (0, 0));

        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);
        Drag((0, 0), (1, 0));

        Assert.Equal(["·bcdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void MovingBackToWhereItBegan_LeavesNoStep()
    {
        SelectTheFirstTwoCells();
        Press(0, 0);
        Move(2, 0);

        Move(0, 0);
        Release(0, 0);

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void AClick_MakesNoStep()
    {
        SelectTheFirstTwoCells();

        Drag((1, 1), (1, 1));

        Assert.Equal(1, History.UndoCount);
        Assert.False(_tool.IsMoving);
    }

    [Fact]
    public void WithNothingSelected_ADragMovesTheWholeLayer_AndSelectsNothing()
    {
        Drag((0, 0), (1, 1));

        Assert.Equal(["······", "·abcde", "·ghijk"], TestCells.Read(Layer));
        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void AnUndo_EndsTheMove()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));
        History.Undo();

        Drag((0, 0), (1, 0));

        Assert.Equal(["·abdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void AnotherChange_EndsTheMove_SoTheCellsReplaceWhatTheyreOn()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        Select("···#··", "······", "······");
        Drag((0, 0), (1, 0));

        Assert.Equal(["··a·bf", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void ASave_DoesntEndTheMove()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        History.MarkSaved();
        Drag((0, 0), (2, 0));

        Assert.Equal(["··cdab", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(2, History.UndoCount);
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public void ChoosingAnotherLayer_EndsTheMove()
    {
        var other = TestCells.AddLayer(_session.Document);
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        _session.SetActiveLayer(other.Id);
        _session.SetActiveLayer(Layer.Id);
        Drag((0, 0), (2, 0));

        Assert.Equal(["····ab", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void ChoosingAnotherTool_EndsTheMove_WhereItsCellsAre()
    {
        SelectTheFirstTwoCells();
        Drag((0, 0), (2, 0));

        _tool.Deactivate();

        Assert.Equal(["··abef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(2, History.UndoCount);
        Assert.False(_tool.IsMoving);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ALockedOrHiddenLayer_IsLeftAlone(bool isLocked, bool isHidden)
    {
        SelectTheFirstTwoCells();
        History.Execute(new SetLayerPropertiesCommand(Layer.Id,
            Layer.Properties with { IsLocked = isLocked, IsVisible = !isHidden }));
        var stepsBefore = History.UndoCount;

        Drag((0, 0), (2, 0));

        Assert.False(_tool.KeyPressed(ToolKey.Right, ToolModifiers.None));
        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(stepsBefore, History.UndoCount);
    }

    [Theory]
    [InlineData(ToolKey.Escape)]
    [InlineData(ToolKey.Enter)]
    public void EscAndEnter_WithNoMoveUnderWay_ArentTheTools(ToolKey key)
    {
        SelectTheFirstTwoCells();
        Drag((1, 1), (1, 1));

        Assert.False(_tool.KeyPressed(key, ToolModifiers.None));
    }

    private void SelectTheFirstTwoCells() => Select("##····", "······", "······");

    private void Select(params string[] rows) => TestTools.Select(_session, rows);

    private void Drag((int X, int Y) from, (int X, int Y) to, ToolModifiers modifiers = ToolModifiers.None)
    {
        _tool.PointerPressed(Input(from.X, from.Y, modifiers));
        _tool.PointerMoved(Input(to.X, to.Y, modifiers));
        _tool.PointerReleased(Input(to.X, to.Y, modifiers));
    }

    private void Press(int x, int y) => _tool.PointerPressed(Input(x, y));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);
}
