// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tests.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Tools;

/// <summary>The arrows in the selection tools move the selected cells as the Move tool's do (ADR 0073).</summary>
public sealed class SelectionToolArrowsTests
{
    private static readonly string[] _art = ["abcdef", "ghijkl", "mnopqr"];

    private readonly FakeClock _clock = new();
    private readonly EditingSession _session;

    public SelectionToolArrowsTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3), _clock);
        TestCells.Paint(Layer, _art);
    }

    public static TheoryData<string> SelectionTools => ["rectangle", "ellipse", "lasso", "polygon", "wand"];

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Theory]
    [MemberData(nameof(SelectionTools))]
    public void TheArrows_MoveTheSelectedCells_AsOneMoveStep(string toolName)
    {
        var tool = Create(toolName);
        SelectTheFirstTwoCells();

        Nudge(tool, ToolKey.Right, ToolKey.Down);

        Assert.Equal(["··cdef", "gabjkl", "mnopqr"], TestCells.Read(Layer));
        Assert.Equal(["······", "·##···", "······"], Selected);
        Assert.Equal(2, History.UndoCount);
        Assert.Equal(UndoStepNames.Move, History.GetStepNameKey(1));
    }

    [Theory]
    [MemberData(nameof(SelectionTools))]
    public void WithNothingSelected_TheArrowsDoNothing(string toolName)
    {
        var tool = Create(toolName);

        Assert.False(tool.KeyPressed(ToolKey.Right, ToolModifiers.None));
        Assert.False(tool.KeyPressed(ToolKey.FarDown, ToolModifiers.Shift));

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void ShiftAndAnArrow_MovesTheCellsTen_PastTheCanvasToo()
    {
        var tool = Create("rectangle");
        SelectTheFirstTwoCells();

        Assert.True(tool.KeyPressed(ToolKey.FarRight, ToolModifiers.Shift));

        Assert.Equal(new CellRect(10, 0, 2, 1), _session.Selection.Mask.Bounds);
        Assert.Equal(["··cdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void Enter_EndsTheMove_AndDeselects_LeavingTheCellsWhereTheyAre()
    {
        var tool = Create("rectangle");
        SelectTheFirstTwoCells();
        Nudge(tool, ToolKey.Right, ToolKey.Right);

        Assert.True(tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(["··abef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Theory]
    [InlineData("rectangle")]
    [InlineData("ellipse")]
    [InlineData("lasso")]
    [InlineData("wand")]
    public void EnterWithNoMoveUnderWay_DoesNothing(string toolName)
    {
        var tool = Create(toolName);
        SelectTheFirstTwoCells();

        Assert.False(tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));

        Assert.False(_session.Selection.IsEmpty);
    }

    [Fact]
    public void AGestureStarting_EndsTheMove_SoItsCellsReplaceWhatTheyreOn()
    {
        var tool = Create("rectangle");
        SelectTheFirstTwoCells();
        Nudge(tool, ToolKey.Right, ToolKey.Right);

        // A drag that's let go of with Esc leaves the selection as it was.
        tool.PointerPressed(Input(5, 2));
        Assert.True(tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Nudge(tool, ToolKey.Left, ToolKey.Left);

        Assert.Equal(["ab··ef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void AnotherTool_EndsTheMove()
    {
        var tool = Create("lasso");
        SelectTheFirstTwoCells();
        Nudge(tool, ToolKey.Right, ToolKey.Right);

        tool.Deactivate();
        Nudge(Create("lasso"), ToolKey.Left, ToolKey.Left);

        Assert.Equal(["ab··ef", "ghijkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Theory]
    [InlineData("rectangle")]
    [InlineData("lasso")]
    [InlineData("polygon")]
    [InlineData("wand")]
    public void DuringAGesture_TheArrowsAreTaken_ButDoNothing(string toolName)
    {
        var tool = Create(toolName);
        SelectTheFirstTwoCells();

        tool.PointerPressed(Input(4, 2));
        Assert.True(tool.KeyPressed(ToolKey.Right, ToolModifiers.None));

        Assert.Equal(_art, TestCells.Read(Layer));
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void AnUndo_TakesTheCellsBack_AndTheArrowsCarryOnFromThere()
    {
        var tool = Create("wand");
        SelectTheFirstTwoCells();
        Nudge(tool, ToolKey.Right);
        _clock.Advance(TimeSpan.FromSeconds(1));
        Nudge(tool, ToolKey.Right);

        History.Undo();
        Assert.Equal(["·abdef", "ghijkl", "mnopqr"], TestCells.Read(Layer));

        Nudge(tool, ToolKey.Down);
        Assert.Equal(["··cdef", "gabjkl", "mnopqr"], TestCells.Read(Layer));
    }

    [Fact]
    public void OnALockedLayer_TheArrowsDoNothing()
    {
        var tool = Create("rectangle");
        SelectTheFirstTwoCells();
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));

        Assert.False(tool.KeyPressed(ToolKey.Right, ToolModifiers.None));

        Assert.Equal(_art, TestCells.Read(Layer));
    }

    private ITool Create(string toolName)
    {
        var context = TestTools.Context(_session);
        return toolName switch
        {
            "rectangle" => new RectangleSelectTool(context),
            "ellipse" => new EllipseSelectTool(context),
            "lasso" => new LassoTool(context),
            "polygon" => new PolygonalLassoTool(context),
            "wand" => new MagicWandTool(context),
            _ => throw new ArgumentOutOfRangeException(nameof(toolName))
        };
    }

    private void SelectTheFirstTwoCells() => TestTools.Select(_session, "##····", "······", "······");

    // Presses and lets go of each arrow in turn, with no time between.
    private static void Nudge(ITool tool, params ToolKey[] arrows)
    {
        foreach (var arrow in arrows)
        {
            tool.KeyPressed(arrow, ToolModifiers.None);
            tool.ArrowReleased(arrow);
        }
    }

    private static ToolPointerInput Input(int x, int y) =>
        new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
