// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools;

public sealed class CharacterBrushToolTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    private readonly EditingSession _session;
    private readonly PaintColors _colors = new();
    private readonly ToolOptionValues _tipValues = new(BrushTool.TipOptions);
    private readonly ToolOptionValues _options = new(CharacterBrushOptions.Group);
    private readonly RecentGlyphs _recent = new();
    private readonly CharacterBrushTool _tool;

    public CharacterBrushToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3, ColorMode.Ansi));
        TestCells.Paint(Layer, "abc···", "ghi···", "mno···");
        _tool = new CharacterBrushTool(_session, _colors, TestTools.Tip(_tipValues), _options, _recent);
        _colors.Foreground = _red;
        _colors.Background = _blue;
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private Rune Character => _options.Get(CharacterBrushOptions.Character);

    [Fact]
    public void TheCharacter_StartsAsAHash()
    {
        Assert.Equal(new Rune('#'), Character);
    }

    [Fact]
    public void AStroke_PutsTheCharacter_InEveryCellItCrosses_InTheCurrentColors()
    {
        Press(1, 0);
        Move(4, 0);
        Release(4, 0);

        Assert.Equal(["a####·", "ghi···", "mno···"], TestCells.Read(Layer));
        Assert.Equal(TestCells.Glyph('#', _red, _blue), CellAt(1, 0));
        Assert.Equal(TestCells.Glyph('#', _red, _blue), CellAt(4, 0));
    }

    [Fact]
    public void ABiggerTip_PaintsEveryCellUnderIt_HardEdged()
    {
        TestTools.SetTip(_tipValues, 3);
        _options.Set(CharacterBrushOptions.Character, new Rune('*'));

        Press(1, 1);
        Release(1, 1);

        Assert.Equal(["***···", "***···", "***···"], TestCells.Read(Layer));
    }

    [Fact]
    public void AnUnsetBackground_KeepsTheCellsOwn()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _green));
        _colors.Background = CellColor.Unset;

        Press(0, 0);
        Release(0, 0);

        Assert.Equal(TestCells.Glyph('#', _red, _green), CellAt(0, 0));
    }

    [Fact]
    public void AStroke_KeepsItsCharacterAndColors_WhenTheyChangeDuringIt()
    {
        Press(0, 0);
        _options.Set(CharacterBrushOptions.Character, new Rune('+'));
        _colors.Foreground = _green;
        Move(2, 0);
        Release(2, 0);

        Assert.Equal(TestCells.Glyph('#', _red, _blue), CellAt(2, 0));
    }

    [Fact]
    public void OneStroke_IsOneUndoStep_NamedPaint()
    {
        Press(0, 0);
        Move(3, 1);
        Release(3, 1);

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Paint, History.GetStepNameKey(0));
        History.Undo();
        Assert.Equal(["abc···", "ghi···", "mno···"], TestCells.Read(Layer));
    }

    [Fact]
    public void Escape_DuringAStroke_TakesItBack()
    {
        Press(0, 0);
        Move(3, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(["abc···", "ghi···", "mno···"], TestCells.Read(Layer));
        Assert.False(History.CanUndo);
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void EachStroke_MakesItsCharacterTheNewestRecentOne()
    {
        Press(0, 0);
        Release(0, 0);
        _options.Set(CharacterBrushOptions.Character, new Rune('@'));
        Press(1, 0);
        Release(1, 0);

        Assert.Equal("@#", _recent.ToString());
    }

    [Fact]
    public void ACharacterTheDocumentCantHold_PaintsNothing_AndIsntRecent()
    {
        var session = new EditingSession(Document.Create(new CanvasSize(4, 2), "Layer", CharacterSet.Ascii));
        var tool = new CharacterBrushTool(session, _colors, TestTools.Tip(_tipValues), _options, _recent);
        _options.Set(CharacterBrushOptions.Character, new Rune(0x2588));

        tool.PointerPressed(Input(0, 0));
        tool.PointerReleased(Input(0, 0));

        Assert.False(session.History.CanUndo);
        Assert.Empty(_recent.Glyphs);
        Assert.Equal(CharacterBrushOptions.CharacterDoesNotFitWarningKey,
            CharacterBrushOptions.WarningFor(Settings(new Rune(0x2588)), session.Document));
        Assert.Null(CharacterBrushOptions.WarningFor(Settings(new Rune('#')), session.Document));
    }

    [Fact]
    public void ALockedLayer_IsntPainted()
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));
        var stepsBefore = History.UndoCount;

        Press(0, 0);
        Release(0, 0);

        Assert.Equal(stepsBefore, History.UndoCount);
        Assert.Empty(_recent.Glyphs);
    }

    [Fact]
    public void AltClick_PicksTheCharacterAndBothColorsShown()
    {
        Layer.SetCell(new CellPoint(1, 0), TestCells.Glyph('x', _green, _red));

        Press(1, 0, ToolModifiers.Alt);
        Release(1, 0);

        Assert.Equal(new Rune('x'), Character);
        Assert.Equal(_green, _colors.Foreground);
        Assert.Equal(_red, _colors.Background);
        Assert.False(History.CanUndo);
        Assert.Empty(_recent.Glyphs);
    }

    [Fact]
    public void AltClick_OnACellWithNoCharacter_PicksNothing()
    {
        Layer.SetCell(new CellPoint(3, 0), Cell.FromBackground(_green));

        Press(3, 0, ToolModifiers.Alt);
        Press(4, 0, ToolModifiers.Alt);

        Assert.Equal(new Rune('#'), Character);
        Assert.Equal(_red, _colors.Foreground);
        Assert.Equal(_blue, _colors.Background);
    }

    [Fact]
    public void AltClick_InAMonochromeDocument_PicksOnlyTheCharacter()
    {
        var session = new EditingSession(TestCells.CreateDocument(2, 1));
        TestCells.Paint(session.Document.ActiveFrame.Layers[0], "q·");
        var tool = new CharacterBrushTool(session, _colors, TestTools.Tip(_tipValues), _options, _recent);

        tool.PointerPressed(Input(0, 0, ToolModifiers.Alt));

        Assert.Equal(new Rune('q'), Character);
        Assert.Equal(_red, _colors.Foreground);
    }

    [Fact]
    public void Escape_WhilePicking_GivesBackTheCharacterAndColors()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _green));

        Press(0, 0, ToolModifiers.Alt);
        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(new Rune('#'), Character);
        Assert.Equal(_red, _colors.Foreground);
        Assert.Equal(_blue, _colors.Background);
    }

    [Fact]
    public void TheSquareBrackets_ChangeTheTipsSize()
    {
        var size = _tipValues.Get(BrushTipOptions.Size);

        Assert.True(_tool.TextEntered("]"));

        Assert.Equal(size + 1, _tipValues.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void TheBrush_IsInTheBrushGroup_WithTheTipAndItsCharacter()
    {
        Assert.Contains(CharacterBrushTool.Id, BuiltInToolGroups.Brush.ToolIds);
        Assert.Equal([BrushTool.TipOptions, CharacterBrushOptions.Group], CharacterBrushTool.Definition.OptionGroups);
    }

    private static ToolSettings Settings(Rune character)
    {
        var settings = new ToolSettings();
        settings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, character);
        return settings;
    }

    [Fact]
    public void WhileCellsAreSelected_ADrag_PutsTheCharacterOnlyInThem()
    {
        TestTools.Select(_session, "······", "···##·", "······");

        Press(0, 1);
        Move(5, 1);
        Release(5, 1);

        Assert.Equal(["abc···", $"ghi{Character}{Character}·", "mno···"], TestCells.Read(Layer));
    }

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(Input(x, y, modifiers));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));
}
