// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools.Lines;

// The Line tool (ADR 0079): a drag draws a line in the document as it goes, as one undo step. Cells here are half as
// wide as they're tall, as in most fonts.
public sealed class LineToolTests
{
    private const double HalfAsWide = 0.5;
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);

    private readonly EditingSession _session;
    private readonly PaintColors _colors = new();
    private readonly ToolSettings _settings = new();
    private readonly RecentGlyphs _recent = new();
    private readonly LineTool _tool;

    public LineToolTests()
    {
        _session = new EditingSession(
            Document.Create(new CanvasSize(10, 4), "Layer", CharacterSet.Ascii, colorMode: ColorMode.Ansi));
        TestCells.Paint(Layer, "abcdefghij", "··········", "··········", "··········");
        _tool = new LineTool(_session, _colors, Style, _settings.For(LineStyleOptions.JoinsGroup), Character, _recent,
            () => HalfAsWide, () => TestTools.NoInk);
        _colors.Foreground = _red;
        _colors.Background = _blue;
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private ToolOptionValues Style => _settings.For(LineStyleOptions.Group);

    private ToolOptionValues Character => _settings.For(CharacterBrushOptions.Group);

    [Fact]
    public void ADrag_DrawsAStraightLine_FromCellToCell_InTheCurrentColors()
    {
        Press(1, 1);
        Move(6, 1);
        Release(6, 1);

        Assert.Equal(["abcdefghij", "·------···", "··········", "··········"], TestCells.Read(Layer));
        Assert.Equal(TestCells.Glyph('-', _red, _blue), CellAt(1, 1));
        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Line, History.GetStepNameKey(0));
    }

    [Fact]
    public void TheLine_ShowsAsItsDragged_AndEachNewEndReplacesTheLast_AsOneStep()
    {
        Press(1, 0);
        Move(8, 0);
        Assert.Equal("a--------j", TestCells.Read(Layer)[0]);

        Move(3, 0);
        Assert.Equal("a---efghij", TestCells.Read(Layer)[0]);
        Release(3, 0);

        Assert.Equal(1, History.UndoCount);
        History.Undo();
        Assert.Equal("abcdefghij", TestCells.Read(Layer)[0]);
    }

    [Fact]
    public void AReleaseInTheCellItStarted_DrawsNothing()
    {
        Press(2, 2);
        Move(5, 2);
        Move(2, 2);
        Release(2, 2);

        Assert.Equal("··········", TestCells.Read(Layer)[2]);
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void Esc_TakesTheLineBack_AndIsOnlyUsedDuringADrag()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Press(1, 1);
        Move(7, 3);
        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Move(8, 3);
        Release(8, 3);

        Assert.Equal(["abcdefghij", "··········", "··········", "··········"], TestCells.Read(Layer));
        Assert.Equal(0, History.UndoCount);
    }

    // Five columns look shorter than three rows when cells are half as wide.
    [Fact]
    public void Shift_KeepsTheLineToTheAxisThePointerHasGoneFurtherAlong_OnScreen()
    {
        Press(1, 0, ToolModifiers.Shift);
        Move(6, 1, ToolModifiers.Shift);
        Assert.Equal(["a------hij", "··········", "··········", "··········"], TestCells.Read(Layer));

        Move(3, 3, ToolModifiers.Shift);
        Assert.Equal(["a|cdefghij", "·|········", "·|········", "·|········"], TestCells.Read(Layer));
    }

    [Fact]
    public void ModifiersPressedDuringTheDrag_RedrawTheLineAtOnce()
    {
        Press(1, 1);
        Move(7, 2);
        _tool.ModifiersChanged(ToolModifiers.Shift);

        Assert.Equal(["abcdefghij", "·-------··", "··········", "··········"], TestCells.Read(Layer));
    }

    [Fact]
    public void CtrlShift_KeepsTheLineToOneColumnPerRow()
    {
        Press(0, 0, ToolModifiers.Command | ToolModifiers.Shift);
        Move(5, 3, ToolModifiers.Command | ToolModifiers.Shift);

        Assert.Equal(["\\bcdefghij", "·\\········", "··\\·······", "···\\······"], TestCells.Read(Layer));
    }

    [Fact]
    public void SingleCharacter_DrawsTheCharacterBrushsCharacter_AndMakesItRecent()
    {
        Style.Set(LineStyleOptions.Style, "singleCharacter");
        Character.Set(CharacterBrushOptions.Character, new Rune('*'));

        Press(0, 3);
        Move(4, 3);
        Release(4, 3);

        Assert.Equal("*****·····", TestCells.Read(Layer)[3]);
        Assert.Equal(new Rune('*'), _recent.Glyphs[0]);
    }

    [Fact]
    public void SingleCharacter_DrawsNothing_WhileTheDocumentCantHoldTheCharacter()
    {
        Style.Set(LineStyleOptions.Style, "singleCharacter");
        Character.Set(CharacterBrushOptions.Character, new Rune('█'));

        Press(0, 3);
        Move(4, 3);
        Release(4, 3);

        Assert.Equal(0, History.UndoCount);
        Assert.Equal(CharacterBrushOptions.CharacterDoesNotFitWarningKey, LineToolDefinitionWarning());
    }

    [Fact]
    public void TheLine_KeepsToTheSelection()
    {
        TestTools.Select(_session, "··········", "···####···", "··········", "··········");
        var stepsBefore = History.UndoCount;

        Press(0, 1);
        Move(9, 1);
        Release(9, 1);

        Assert.Equal("···----···", TestCells.Read(Layer)[1]);
        Assert.Equal(stepsBefore + 1, History.UndoCount);
    }

    [Fact]
    public void ALineRunningOffTheCanvas_DrawsTheCellsOnIt()
    {
        Press(7, 2);
        Move(14, 2);
        Release(14, 2);

        Assert.Equal("·······---", TestCells.Read(Layer)[2]);
    }

    [Fact]
    public void ALockedLayer_IsntDrawnOn()
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));

        Press(1, 1);
        Move(6, 1);
        Release(6, 1);

        Assert.Equal("··········", TestCells.Read(Layer)[1]);
        Assert.Equal(1, History.UndoCount);
    }

    // An undo during the drag takes the line away, and the drag ends rather than taking back a step not its own.
    [Fact]
    public void AnUndoDuringTheDrag_EndsIt()
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { Name = "Art" }));
        Press(1, 1);
        Move(6, 1);
        History.Undo();
        Move(8, 1);
        Release(8, 1);

        Assert.Equal("··········", TestCells.Read(Layer)[1]);
        Assert.Equal(1, History.UndoCount);
        Assert.Equal(1, History.RedoCount);
    }

    [Fact]
    public void ChoosingACharacterInTheCharactersWindow_ChoosesSingleCharacter()
    {
        LineTool.Definition.CharacterChosen!(_settings);

        Assert.Equal(LineStyle.SingleCharacter, LineStyleOptions.ReadStyle(Style));
    }

    [Fact]
    public void TheCharacterWarning_ShowsOnlyInSingleCharacter()
    {
        Character.Set(CharacterBrushOptions.Character, new Rune('█'));

        Assert.Null(LineToolDefinitionWarning());
        Style.Set(LineStyleOptions.Style, "singleCharacter");
        Assert.Equal(CharacterBrushOptions.CharacterDoesNotFitWarningKey, LineToolDefinitionWarning());
    }

    // Only the rectangle has the box-drawing styles (ADR 0082).
    [Fact]
    public void TheLinesStyles_AreThePlainOnes()
    {
        Assert.Equal(["standard", "standardByStep", "precise", "singleCharacter"],
            LineStyleOptions.Style.Choices.Select(choice => choice.Value));
        Assert.Contains(LineStyleOptions.Group, LineTool.Definition.OptionGroups);
    }

    [Fact]
    public void IncludeJoins_IsOffAtFirst_AndTheLineHasIt()
    {
        Assert.False(_settings.For(LineStyleOptions.JoinsGroup).Get(LineStyleOptions.IncludeJoins));
        Assert.Contains(LineStyleOptions.JoinsGroup, LineTool.Definition.OptionGroups);
    }

    private string? LineToolDefinitionWarning() => LineTool.Definition.WarningFor!(_settings, _session.Document);

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(Input(x, y, modifiers));

    private void Move(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerMoved(Input(x, y, modifiers));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);
}
