// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Shapes;

namespace AskiiArt.Core.Tests.Tools.Shapes;

// The Rectangle and Ellipse tools (ADR 0080). Cells here are half as wide as they're tall, as in most fonts.
public sealed class BoxShapeToolTests
{
    private const double HalfAsWide = 0.5;
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);

    private readonly ToolSettings _settings = new();
    private readonly PaintColors _colors = new();
    private EditingSession _session;
    private BoxShapeTool _tool;

    public BoxShapeToolTests()
    {
        _session = SessionFor(ColorMode.Ansi);
        _tool = ToolFor(BoxShape.Rectangle);
        _colors.Foreground = _red;
        _colors.Background = _blue;
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private ToolOptionValues Style => _settings.For(LineStyleOptions.Group);

    private ToolOptionValues RectangleStyle => _settings.For(ShapeOptions.RectangleStyleGroup);

    private ToolOptionValues Fill => _settings.For(ShapeOptions.FillGroup);

    private ToolOptionValues Character => _settings.For(CharacterBrushOptions.Group);

    [Fact]
    public void ADrag_DrawsARectangleFromCellToCell_BothIncluded_InTheCurrentColors_AsOneStep()
    {
        Drag((1, 1), (6, 4));

        Assert.Equal(
            ["············", "·+----+·····", "·|····|·····", "·|····|·····", "·+----+·····", "············",
                "············"],
            TestCells.Read(Layer));
        Assert.Equal(TestCells.Glyph('-', _red, _blue), Layer.GetCell(new CellPoint(2, 1)));
        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Rectangle, History.GetStepNameKey(0));
    }

    [Fact]
    public void ADragUpAndLeft_DrawsTheSameRectangle()
    {
        Drag((6, 4), (1, 1));

        Assert.Equal("·+----+·····", TestCells.Read(Layer)[1]);
        Assert.Equal("·+----+·····", TestCells.Read(Layer)[4]);
    }

    [Fact]
    public void TheRectangle_ShowsAsItsDragged_EachBoxReplacingTheLast()
    {
        Press(1, 1);
        Move(8, 5);
        Move(3, 2);
        Release(3, 2);

        Assert.Equal(["············", "·+-+········", "·+-+········"], TestCells.Read(Layer)[..3]);
        Assert.Equal("············", TestCells.Read(Layer)[5]);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void RoundedCorners_AreDotsAtTheTopAndApostrophesAtTheBottom()
    {
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);

        Drag((1, 1), (6, 3));

        Assert.Equal(["·.----.·····", "·|····|·····", "·'----'·····"], TestCells.Read(Layer)[1..4]);
    }

    [Fact]
    public void RoundedCorners_InSingleCharacter_LeaveTheCornersAlone()
    {
        TestCells.Paint(Layer, "", "x·····x");
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);
        RectangleStyle.Set(ShapeOptions.RectangleStyle, "singleCharacter");
        Character.Set(CharacterBrushOptions.Character, new Rune('*'));

        Drag((0, 1), (6, 3));

        Assert.Equal(["x*****x·····", "*·····*·····", "·*****······"], TestCells.Read(Layer)[1..4]);
    }

    [Fact]
    public void RoundedCorners_InBox_AreTheRoundedBoxDrawingCorners()
    {
        _session = SessionFor(ColorMode.Ansi, CharacterSet.Unicode);
        _tool = ToolFor(BoxShape.Rectangle);
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);
        RectangleStyle.Set(ShapeOptions.RectangleStyle, "box");

        Drag((1, 1), (6, 3));

        Assert.Equal(["·╭────╮·····", "·│····│·····", "·╰────╯·····"], TestCells.Read(Layer)[1..4]);
    }

    // Heavy and double box-drawing lines have no rounded corners.
    [Theory]
    [InlineData("heavyBox", "┏━━━━┓")]
    [InlineData("doubleBox", "╔════╗")]
    public void RoundedCorners_InHeavyAndDoubleBox_StaySquare(string style, string top)
    {
        _session = SessionFor(ColorMode.Ansi, CharacterSet.Unicode);
        _tool = ToolFor(BoxShape.Rectangle);
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);
        RectangleStyle.Set(ShapeOptions.RectangleStyle, style);

        Drag((1, 1), (6, 3));

        Assert.Equal("·" + top + "·····", TestCells.Read(Layer)[1]);
    }

    [Fact]
    public void ABoxStyleInAnAsciiDocument_DrawsInStandard_WithItsRoundedCorners_AndWarns()
    {
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);
        RectangleStyle.Set(ShapeOptions.RectangleStyle, "box");

        Drag((1, 1), (6, 3));

        Assert.Equal(["·.----.·····", "·|····|·····", "·'----'·····"], TestCells.Read(Layer)[1..4]);
        Assert.Equal(LineStyleOptions.BoxNeedsUnicodeWarningKey, Warning());
    }

    [Fact]
    public void FillCharacter_FillsTheInside_WithTheCharacterBrushsCharacter_AndMakesItRecent()
    {
        Fill.Set(ShapeOptions.Fill, "character");
        Character.Set(CharacterBrushOptions.Character, new Rune('#'));

        Drag((1, 1), (5, 4));

        Assert.Equal(["·+---+······", "·|###|······", "·|###|······", "·+---+······"], TestCells.Read(Layer)[1..5]);
        Assert.Equal(TestCells.Glyph('#', _red, _blue), Layer.GetCell(new CellPoint(2, 2)));
        Assert.Equal(new Rune('#'), _settings.RecentGlyphs.Glyphs[0]);
    }

    [Fact]
    public void FillBackground_ColoursTheInside_AndKeepsItsGlyphs()
    {
        TestCells.Paint(Layer, "", "", "···a");
        Fill.Set(ShapeOptions.Fill, "background");

        Drag((1, 1), (5, 4));

        Assert.Equal(TestCells.Glyph('a').WithBackground(_blue), Layer.GetCell(new CellPoint(3, 2)));
        Assert.Equal(Cell.FromBackground(_blue), Layer.GetCell(new CellPoint(2, 3)));
        Assert.True(Layer.GetCell(new CellPoint(0, 2)).IsEmpty);
    }

    [Fact]
    public void FillBackground_FillsNothing_AndWarns_InAMonochromeDocument()
    {
        _session = SessionFor(ColorMode.Monochrome);
        _tool = ToolFor(BoxShape.Rectangle);
        Fill.Set(ShapeOptions.Fill, "background");

        Drag((1, 1), (5, 4));

        Assert.Equal("·|···|······", TestCells.Read(Layer)[2]);
        Assert.Equal(Cell.Empty, Layer.GetCell(new CellPoint(2, 2)));
        Assert.Equal(EraserTool.NoBackgroundsWarningKey, Warning());
    }

    [Fact]
    public void AnEllipse_FitsTheBox_AndItsFillStaysInsideItsOutline()
    {
        _tool = ToolFor(BoxShape.Ellipse);
        Fill.Set(ShapeOptions.Fill, "character");
        Character.Set(CharacterBrushOptions.Character, new Rune('#'));

        Drag((0, 0), (10, 6));

        var rows = TestCells.Read(Layer);
        Assert.Equal("|#########|·", rows[3]);
        Assert.DoesNotContain('#', rows[0] + rows[6]);
        Assert.True(Layer.GetCell(new CellPoint(0, 0)).IsEmpty);
        Assert.True(Layer.GetCell(new CellPoint(10, 6)).IsEmpty);
        Assert.Equal(UndoStepNames.Ellipse, History.GetStepNameKey(0));
    }

    // Ten columns look as wide as five rows when cells are half as wide.
    [Fact]
    public void Shift_MakesTheBoxLookSquare()
    {
        Press(0, 0, ToolModifiers.Shift);
        Move(9, 1, ToolModifiers.Shift);

        Assert.Equal("+--------+··", TestCells.Read(Layer)[4]);
        Assert.Equal("············", TestCells.Read(Layer)[5]);
    }

    [Fact]
    public void Alt_GrowsTheBoxFromTheFirstCell_AndModifiersRedrawAtOnce()
    {
        Press(5, 3);
        Move(7, 4);
        _tool.ModifiersChanged(ToolModifiers.Alt);

        Assert.Equal(["············", "············", "···+---+····", "···|···|····", "···+---+····"],
            TestCells.Read(Layer)[..5]);
    }

    [Fact]
    public void ABoxOneCellThin_DrawsALine()
    {
        Drag((1, 2), (8, 2));

        Assert.Equal("·--------···", TestCells.Read(Layer)[2]);
    }

    [Fact]
    public void AReleaseInTheFirstCell_DrawsNothing_EvenWithShift()
    {
        Press(3, 3, ToolModifiers.Shift);
        Move(7, 5, ToolModifiers.Shift);
        Move(3, 3, ToolModifiers.Shift);
        Release(3, 3);

        Assert.All(TestCells.Read(Layer), row => Assert.Equal("············", row));
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void Esc_TakesTheShapeBack()
    {
        Press(1, 1);
        Move(6, 4);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Release(6, 4);

        Assert.Equal(0, History.UndoCount);
        Assert.True(Layer.GetCell(new CellPoint(1, 1)).IsEmpty);
    }

    [Fact]
    public void TheShape_KeepsToTheSelection()
    {
        TestTools.Select(_session, "#####·······", "············", "············", "············", "············",
            "············", "············");

        Drag((0, 0), (8, 4));

        Assert.Equal(["+----·······", "············"], TestCells.Read(Layer)[..2]);
    }

    [Fact]
    public void ACharacterTheDocumentCantHold_DrawsNothing_AndWarns_OnlyWhileTheShapeUsesIt()
    {
        Character.Set(CharacterBrushOptions.Character, new Rune('█'));
        Assert.Null(Warning());

        Fill.Set(ShapeOptions.Fill, "character");
        Drag((1, 1), (5, 4));

        Assert.Equal(0, History.UndoCount);
        Assert.Equal(CharacterBrushOptions.CharacterDoesNotFitWarningKey, Warning());
    }

    [Fact]
    public void ChoosingACharacter_MakesTheShapesStyleSingleCharacter_OnlyWhenNothingUsesIt()
    {
        BoxShapeTool.RectangleDefinition.CharacterChosen!(_settings);
        Assert.Equal(LineStyle.SingleCharacter, LineStyleOptions.ReadStyle(RectangleStyle));
        Assert.Equal(LineStyle.Standard, LineStyleOptions.ReadStyle(Style));

        BoxShapeTool.EllipseDefinition.CharacterChosen!(_settings);
        Assert.Equal(LineStyle.SingleCharacter, LineStyleOptions.ReadStyle(Style));

        Style.Set(LineStyleOptions.Style, "precise");
        Fill.Set(ShapeOptions.Fill, "character");
        BoxShapeTool.EllipseDefinition.CharacterChosen!(_settings);
        Assert.Equal(LineStyle.Precise, LineStyleOptions.ReadStyle(Style));
    }

    // The ellipse keeps the Line tool's styles and doesn't join (ADR 0082).
    [Fact]
    public void OnlyTheRectangle_HasRoundedCorners_TheBoxStyles_AndIncludeJoins()
    {
        Assert.Equal(
            [ShapeOptions.RectangleStyleGroup, LineStyleOptions.JoinsGroup, ShapeOptions.FillGroup,
                ShapeOptions.RectangleGroup, CharacterBrushOptions.Group],
            BoxShapeTool.RectangleDefinition.OptionGroups);
        Assert.Equal([LineStyleOptions.Group, ShapeOptions.FillGroup, CharacterBrushOptions.Group],
            BoxShapeTool.EllipseDefinition.OptionGroups);
        Assert.Contains("doubleBox", ShapeOptions.RectangleStyle.Choices.Select(choice => choice.Value));
    }

    [Fact]
    public void TheRectanglesStyle_IsItsOwn()
    {
        RectangleStyle.Set(ShapeOptions.RectangleStyle, "precise");
        Style.Set(LineStyleOptions.Style, "standardByStep");

        Assert.Equal(LineStyle.Precise, LineStyleOptions.ReadStyle(RectangleStyle));
        Assert.Equal(LineStyle.StandardByStep, LineStyleOptions.ReadStyle(Style));
    }

    private static EditingSession SessionFor(ColorMode colorMode, CharacterSet characterSet = CharacterSet.Ascii) =>
        new(Document.Create(new CanvasSize(12, 7), "Layer", characterSet, colorMode: colorMode));

    private BoxShapeTool ToolFor(BoxShape shape) =>
        new(shape, _session, _colors, _settings, () => HalfAsWide, () => TestTools.NoInk);

    private string? Warning() => BoxShapeTool.RectangleDefinition.WarningFor!(_settings, _session.Document);

    private void Drag((int X, int Y) from, (int X, int Y) to)
    {
        Press(from.X, from.Y);
        Move(to.X, to.Y);
        Release(to.X, to.Y);
    }

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(Input(x, y, modifiers));

    private void Move(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerMoved(Input(x, y, modifiers));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);
}
