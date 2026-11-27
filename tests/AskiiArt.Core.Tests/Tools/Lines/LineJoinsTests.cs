// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Shapes;

namespace AskiiArt.Core.Tests.Tools.Lines;

// Include joins (ADR 0081, ADR 0082): lines and rectangles joining the line characters already in their cells. Cells
// here are half as wide as they're tall, as in most fonts.
public sealed class LineJoinsTests
{
    private const double HalfAsWide = 0.5;

    private readonly ToolSettings _settings = new();
    private readonly PaintColors _colors = new();
    private EditingSession _session = SessionFor(CharacterSet.Unicode);

    public LineJoinsTests()
    {
        _settings.For(LineStyleOptions.JoinsGroup).Set(LineStyleOptions.IncludeJoins, true);
        _settings.For(ShapeOptions.RectangleStyleGroup).Set(ShapeOptions.RectangleStyle, "box");
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    // Each arm is none, light or heavy, so the sets count from 1 to 80 in threes.
    [Fact]
    public void EverySetOfLightAndHeavyArms_HasABoxDrawingCharacter()
    {
        var sets = Enumerable.Range(1, 80).Select(code => new LineArms((LineWeight)(code / 27 % 3),
            (LineWeight)(code / 9 % 3), (LineWeight)(code / 3 % 3), (LineWeight)(code % 3)));

        Assert.All(sets, arms => Assert.NotNull(LineJoins.BoxCharacterFor(arms)));
    }

    [Theory]
    [InlineData('┌', "0110")]
    [InlineData('┿', "1212")]
    [InlineData('╞', "1310")]
    [InlineData('╴', "0001")]
    [InlineData('╭', "0110")]
    [InlineData('-', "0101")]
    [InlineData('|', "1010")]
    [InlineData('+', "1111")]
    public void EachLineCharacter_HasItsArms_UpRightDownAndLeft(char glyph, string arms)
    {
        Assert.True(LineJoins.TryGetArms(new Rune(glyph), out var found));
        Assert.Equal(Arms(arms), found);
    }

    [Theory]
    [InlineData('┄')]
    [InlineData('╌')]
    [InlineData('╱')]
    [InlineData('╳')]
    [InlineData('/')]
    [InlineData('_')]
    [InlineData('a')]
    public void DashedAndDiagonalCharacters_AndOthers_AreNotLineCharacters(char glyph) =>
        Assert.False(LineJoins.TryGetArms(new Rune(glyph), out _));

    [Fact]
    public void JoiningNeverMakesARoundedCorner() =>
        Assert.Equal(new Rune('┌'), LineJoins.BoxCharacterFor(Arms("0110")));

    // The arms along the line are the drawn character's, less those an open line's ends point away from the line.
    [Theory]
    [InlineData('─', "0101", '│', '┼')]
    [InlineData('─', "0001", '│', '┤')]
    [InlineData('─', "0100", '│', '├')]
    [InlineData('│', "1000", '─', '┴')]
    [InlineData('━', "0202", '│', '┿')]
    [InlineData('─', "0101", '|', '┼')]
    [InlineData('─', "0100", '━', '╾')]
    [InlineData('─', "0101", '─', '─')]
    [InlineData('╭', "0110", '╭', '╭')]
    public void ABoxLine_JoinsTheLineThere_KeepingItsOwnWeight(char drawn, string along, char existing, char joined) =>
        Assert.Equal(new Rune(joined), LineJoins.Join(new Rune(drawn), Arms(along), new Rune(existing)));

    // No character has double arms across and heavy ones up and down.
    [Fact]
    public void ArmsNoCharacterHasTogether_AllTakeTheDrawnLinesWeight() =>
        Assert.Equal(new Rune('╬'), LineJoins.Join(new Rune('═'), Arms("0303"), new Rune('┃')));

    [Theory]
    [InlineData('│')]
    [InlineData('|')]
    [InlineData('┼')]
    public void APlainLine_JoinsAsAPlus(char existing) =>
        Assert.Equal(new Rune('+'), LineJoins.Join(new Rune('-'), Arms("0101"), new Rune(existing)));

    [Fact]
    public void ArmsTowardCoveredSides_AreDropped()
    {
        Assert.Equal(new Rune('┴'), LineJoins.Join(new Rune('─'), Arms("0101"), new Rune('│'), LineSides.Down));
        Assert.Equal(new Rune('┤'), LineJoins.Join(new Rune('│'), Arms("1010"), new Rune('─'), LineSides.Right));
    }

    [Fact]
    public void AnOpenLinesEnds_HaveNoArmPointingAwayFromIt()
    {
        LineGlyph[] line =
        [
            new(new CellPoint(0, 0), new Rune('─')), new(new CellPoint(1, 0), new Rune('─')),
            new(new CellPoint(2, 1), new Rune('─'))
        ];

        Assert.Equal(Arms("0100"), LineJoins.Along(line, 0, isClosed: false));
        Assert.Equal(Arms("0101"), LineJoins.Along(line, 1, isClosed: false));
        Assert.Equal(Arms("0001"), LineJoins.Along(line, 2, isClosed: false));
        Assert.Equal(Arms("0101"), LineJoins.Along(line, 0, isClosed: true));
    }

    [Fact]
    public void RectanglesSharingAnEdge_JoinWithTees()
    {
        DrawRectangle((0, 0), (5, 3));
        DrawRectangle((5, 0), (9, 3));

        Assert.Equal(["┌────┬───┐··", "│····│···│··", "│····│···│··", "└────┴───┘··"], TestCells.Read(Layer)[..4]);
    }

    [Fact]
    public void RoundedCorners_JoinToo()
    {
        PaintUprightLineAt(5, '│');
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);

        DrawRectangle((5, 1), (9, 3));

        Assert.Equal(["·····│······", "·····├───╮··", "·····│···│··", "·····├───╯··"], TestCells.Read(Layer)[..4]);
    }

    // The newer rectangle's fill covers the older one's lines inside it, so its outline joins only what's outside.
    [Fact]
    public void ACharacterFill_CoversTheLinesInsideIt()
    {
        DrawRectangle((0, 0), (6, 4));
        _settings.For(ShapeOptions.FillGroup).Set(ShapeOptions.Fill, "character");
        _settings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, new Rune('#'));

        DrawRectangle((3, 2), (10, 6));

        Assert.Equal(
            [
                "┌─────┐·····", "│·····│·····", "│··┌──┴───┐·", "│··│######│·", "└──┤######│·", "···│######│·",
                "···└──────┘·"
            ],
            TestCells.Read(Layer));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("background")]
    public void OtherFills_LeaveTheLinesInside_SoTheOutlineCrossesThem(string fill)
    {
        DrawRectangle((0, 0), (6, 4));
        _settings.For(ShapeOptions.FillGroup).Set(ShapeOptions.Fill, fill);

        DrawRectangle((3, 2), (10, 6));

        Assert.Equal("│··┌──┼───┐·", Row(2));
        Assert.Equal("└──┼──┘···│·", Row(4));
    }

    [Theory]
    [InlineData('│')]
    [InlineData('|')]
    [InlineData('┼')]
    public void ALine_JoinsAsAPlus(char existing)
    {
        PaintUprightLineAt(5, existing);

        DrawLine((0, 2), (9, 2));

        Assert.Equal("-----+----··", Row(2));
    }

    [Fact]
    public void WithoutIncludeJoins_ALineDrawsOver()
    {
        _settings.For(LineStyleOptions.JoinsGroup).Set(LineStyleOptions.IncludeJoins, false);
        PaintUprightLineAt(5, '│');

        DrawLine((0, 2), (9, 2));

        Assert.Equal("----------··", Row(2));
    }

    [Fact]
    public void SingleCharacter_NeverJoins()
    {
        PaintUprightLineAt(5, '│');
        _settings.For(LineStyleOptions.Group).Set(LineStyleOptions.Style, "singleCharacter");
        _settings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, new Rune('─'));

        DrawLine((0, 2), (9, 2));

        Assert.Equal("──────────··", Row(2));
    }

    [Fact]
    public void OtherCharacters_AreDrawnOver()
    {
        TestCells.Paint(Layer, "", "", "·a╱/_·····");

        DrawLine((0, 2), (9, 2));

        Assert.Equal("----------··", Row(2));
    }

    [Fact]
    public void TheEllipse_NeverJoins()
    {
        PaintUprightLineAt(5, '│');
        var tool = new BoxShapeTool(BoxShape.Ellipse, _session, _colors, _settings, () => HalfAsWide,
            () => TestTools.NoInk);

        Drag(tool, (0, 0), (10, 6));

        Assert.Equal('-', Row(0)[5]);
    }

    [Fact]
    public void BoxStylesInAnAsciiDocument_AreDrawnInStandard_JoiningWithAPlus()
    {
        _session = SessionFor(CharacterSet.Ascii);
        PaintUprightLineAt(5, '|');

        DrawRectangle((0, 1), (9, 3));

        Assert.Equal("+----+---+··", Row(1));
    }

    // Each new end takes the last one's step back first, so a join the line has left goes back to how it was.
    [Fact]
    public void ALineJoinsWhatWasThereBeforeTheDrag()
    {
        PaintUprightLineAt(5, '│');
        var tool = LineTool();

        tool.PointerPressed(Input(0, 2));
        tool.PointerMoved(Input(9, 2));
        Assert.Equal('+', Row(2)[5]);
        tool.PointerMoved(Input(3, 2));
        tool.PointerReleased(Input(3, 2));

        Assert.Equal("----·│······", Row(2));
    }

    private static EditingSession SessionFor(CharacterSet characterSet) =>
        new(Document.Create(new CanvasSize(12, 7), "Layer", characterSet, colorMode: ColorMode.Ansi));

    private static LineArms Arms(string upRightDownLeft) => new(
        (LineWeight)(upRightDownLeft[0] - '0'), (LineWeight)(upRightDownLeft[1] - '0'),
        (LineWeight)(upRightDownLeft[2] - '0'), (LineWeight)(upRightDownLeft[3] - '0'));

    private void PaintUprightLineAt(int x, char glyph)
    {
        var row = new string('·', x) + glyph;
        TestCells.Paint(Layer, row, row, row, row, row, row, row);
    }

    private string Row(int y) => TestCells.Read(Layer)[y];

    private void DrawLine((int X, int Y) from, (int X, int Y) to) => Drag(LineTool(), from, to);

    private void DrawRectangle((int X, int Y) from, (int X, int Y) to) => Drag(
        new BoxShapeTool(BoxShape.Rectangle, _session, _colors, _settings, () => HalfAsWide, () => TestTools.NoInk),
        from, to);

    private static void Drag(ITool tool, (int X, int Y) from, (int X, int Y) to)
    {
        tool.PointerPressed(Input(from.X, from.Y));
        tool.PointerMoved(Input(to.X, to.Y));
        tool.PointerReleased(Input(to.X, to.Y));
    }

    private LineTool LineTool() => new(_session, _colors, _settings.For(LineStyleOptions.Group),
        _settings.For(LineStyleOptions.JoinsGroup), _settings.For(CharacterBrushOptions.Group), _settings.RecentGlyphs,
        () => HalfAsWide, () => TestTools.NoInk);

    private static ToolPointerInput Input(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
