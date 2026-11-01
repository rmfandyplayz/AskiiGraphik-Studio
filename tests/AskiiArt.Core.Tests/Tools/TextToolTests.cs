// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.Core.Tests.Tools;

public sealed class TextToolTests
{
    private readonly FakeClock _clock = new();
    private readonly EditingSession _session;
    private readonly TextTool _tool;

    public TextToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3), _clock);
        _tool = new TextTool(_session);
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void BeforeAClick_ThereIsNoCaret_AndTypingIsNotUsed()
    {
        Assert.Null(_tool.Caret);
        Assert.False(_tool.TextEntered("a"));
        Assert.False(_tool.KeyPressed(ToolKey.Right, ToolModifiers.None));
        Assert.Equal(["······", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void Typing_WritesAtTheClickedCellAndMovesTheCaretRight()
    {
        Click(1, 1);

        Type("hi!");

        Assert.Equal(["······", "·hi!··", "······"], TestCells.Read(Layer));
        Assert.Equal(new CellPoint(4, 1), _tool.Caret);
    }

    [Fact]
    public void Typing_OverwritesAndStopsAtTheRightEdge()
    {
        TestCells.Paint(Layer, "abcdef");
        Click(4, 0);

        Type("XYZ");

        // The caret stays in the last column, so the last character typed wins there.
        Assert.Equal(["abcdXZ", "······", "······"], TestCells.Read(Layer));
        Assert.Equal(new CellPoint(5, 0), _tool.Caret);
    }

    [Fact]
    public void Space_TypesAnOccupiedSpace_AndSpaceDoesNotPan()
    {
        Click(0, 0);

        Type(" ");

        Assert.True(Layer.GetCell(new CellPoint(0, 0)).IsOccupied);
        Assert.False(_tool.AllowsSpacePanning);
    }

    [Fact]
    public void ACharacterOutsideTheBasicPlane_TakesOneCell()
    {
        Click(0, 0);

        Type("😀!");

        Assert.Equal(["😀!····", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void ControlCharactersAndMarks_AreSkipped()
    {
        Click(0, 0);

        // What some platforms send as text for Enter and Backspace, and a lone combining accent.
        Type("a\r\b́b");

        Assert.Equal(["ab····", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void InAnAsciiDocument_NonAsciiCharactersAreSkipped()
    {
        var session = new EditingSession(Document.Create(new CanvasSize(6, 1), "Layer", CharacterSet.Ascii), _clock);
        var tool = new TextTool(session);
        tool.PointerPressed(PointerAt(0, 0));

        tool.TextEntered("aé漢b");

        Assert.Equal(["ab····"], TestCells.Read(session.Document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void ABurstOfTyping_IsOneUndoStep()
    {
        Click(0, 0);

        Type("abc");
        Press(ToolKey.Backspace);
        Type("d");

        Assert.Equal(1, History.UndoCount);
        History.Undo();
        Assert.Equal(["······", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void APause_StartsANewUndoStep()
    {
        Click(0, 0);
        Type("ab");

        _clock.Advance(TextTool.TypingPause + TimeSpan.FromMilliseconds(1));
        Type("c");

        Assert.Equal(2, History.UndoCount);
        History.Undo();
        Assert.Equal(["ab····", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void TypingSteadily_StaysOneUndoStepEvenPastThePause()
    {
        Click(0, 0);
        foreach (var character in "abcd")
        {
            _clock.Advance(TimeSpan.FromSeconds(1));
            Type(character.ToString());
        }

        Assert.Equal(1, History.UndoCount);
    }

    [Theory]
    [InlineData(ToolKey.Left)]
    [InlineData(ToolKey.Enter)]
    [InlineData(ToolKey.Home)]
    public void MovingTheCaret_StartsANewUndoStep(ToolKey key)
    {
        Click(2, 0);
        Type("a");

        Press(key);
        Type("b");

        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void ClickingAgain_StartsANewUndoStep()
    {
        Click(0, 0);
        Type("a");

        Click(3, 0);
        Type("b");

        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void Backspace_ErasesTheCellBeforeTheCaret_WithoutShiftingTheRest()
    {
        TestCells.Paint(Layer, "abcdef");
        Click(3, 0);

        Press(ToolKey.Backspace);

        Assert.Equal(["ab·def", "······", "······"], TestCells.Read(Layer));
        Assert.Equal(new CellPoint(2, 0), _tool.Caret);
    }

    [Fact]
    public void Backspace_AtTheLeftEdge_DoesNothing()
    {
        TestCells.Paint(Layer, "abcdef");
        Click(0, 0);

        Press(ToolKey.Backspace);

        Assert.Equal(["abcdef", "······", "······"], TestCells.Read(Layer));
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void Delete_ErasesTheCellAtTheCaret_AndTheCaretStays()
    {
        TestCells.Paint(Layer, "abcdef");
        Click(2, 0);

        Press(ToolKey.Delete);

        Assert.Equal(["ab·def", "······", "······"], TestCells.Read(Layer));
        Assert.Equal(new CellPoint(2, 0), _tool.Caret);
    }

    [Fact]
    public void Enter_GoesToTheNextRowAtTheClickedColumn()
    {
        Click(2, 0);
        Type("ab");

        Press(ToolKey.Enter);
        Type("cd");

        Assert.Equal(["··ab··", "··cd··", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void Enter_OnTheLastRow_StaysThere()
    {
        Click(2, 2);
        Type("a");

        Press(ToolKey.Enter);

        Assert.Equal(new CellPoint(2, 2), _tool.Caret);
    }

    [Fact]
    public void ArrowKeys_MoveOneCell_AndStopAtTheEdges()
    {
        Click(0, 0);

        Press(ToolKey.Left);
        Press(ToolKey.Up);
        Assert.Equal(new CellPoint(0, 0), _tool.Caret);

        Press(ToolKey.Right);
        Press(ToolKey.Down);
        Assert.Equal(new CellPoint(1, 1), _tool.Caret);
    }

    [Fact]
    public void Home_GoesToTheFirstColumn_AndEndGoesPastTheLastGlyphInTheRow()
    {
        TestCells.Paint(Layer, "······", "ab·c··");
        Click(1, 1);

        Press(ToolKey.End);
        Assert.Equal(new CellPoint(4, 1), _tool.Caret);

        Press(ToolKey.Home);
        Assert.Equal(new CellPoint(0, 1), _tool.Caret);
    }

    [Fact]
    public void End_InAFullRow_StopsAtTheLastColumn()
    {
        TestCells.Paint(Layer, "abcdef");
        Click(0, 0);

        Press(ToolKey.End);

        Assert.Equal(new CellPoint(5, 0), _tool.Caret);
    }

    [Fact]
    public void Escape_RemovesTheCaret_AndRaisesOverlayChanged()
    {
        Click(1, 1);
        var overlayChanges = 0;
        _tool.OverlayChanged += (_, _) => overlayChanges++;

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Null(_tool.Caret);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
        Assert.Equal(1, overlayChanges);
        Assert.False(_tool.TextEntered("a"));
    }

    [Fact]
    public void ClickingOutsideTheCanvas_LeavesTheCaretAlone()
    {
        Click(1, 1);

        _tool.PointerPressed(PointerAt(-1, 0));
        _tool.PointerPressed(PointerAt(6, 0));

        Assert.Equal(new CellPoint(1, 1), _tool.Caret);
    }

    [Fact]
    public void OnALockedLayer_TypingChangesNothing()
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));
        Click(0, 0);

        Assert.True(_tool.TextEntered("ab"));
        Press(ToolKey.Delete);

        Assert.Equal(["······", "······", "······"], TestCells.Read(Layer));
        Assert.Equal(new CellPoint(0, 0), _tool.Caret);
    }

    [Fact]
    public void AfterTheCanvasShrinks_TheCaretMovesBackInside()
    {
        Click(5, 2);
        History.Execute(new ResizeCanvasCommand(new CanvasSize(3, 2), ResizeAnchor.TopLeft));

        Type("a");

        Assert.Equal(["···", "··a"], TestCells.Read(Layer));
    }

    [Fact]
    public void Registry_CreatesATextToolPerSession()
    {
        var registry = new ToolRegistry([TextTool.Definition]);

        var tool = registry.Get(TextTool.Id).Create(_session);

        Assert.IsType<TextTool>(tool);
        Assert.NotSame(tool, registry.Get(TextTool.Id).Create(_session));
        Assert.Throws<ArgumentException>(() => new ToolRegistry([TextTool.Definition, TextTool.Definition]));
    }

    private static ToolPointerInput PointerAt(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);

    private void Click(int x, int y) => _tool.PointerPressed(PointerAt(x, y));

    private void Type(string text) => Assert.True(_tool.TextEntered(text));

    private void Press(ToolKey key) => Assert.True(_tool.KeyPressed(key, ToolModifiers.None));
}
