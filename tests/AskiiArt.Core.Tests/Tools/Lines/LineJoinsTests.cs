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

// Include joins (ADR 0081): lines and outlines joining the line characters already in their cells. Cells here are
// half as wide as they're tall, as in most fonts.
public sealed class LineJoinsTests
{
    private const double HalfAsWide = 0.5;

    private readonly ToolSettings _settings = new();
    private readonly PaintColors _colors = new();
    private EditingSession _session = SessionFor(CharacterSet.Unicode);

    public LineJoinsTests() => Options.Set(LineStyleOptions.IncludeJoins, true);

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private ToolOptionValues Options => _settings.For(LineStyleOptions.Group);

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

    [Fact]
    public void ALineCrossingALine_MakesACross()
    {
        PaintUprightLineAt(5, '│');

        DrawLine(LineStyle.Box, (0, 2), (9, 2));

        Assert.Equal("─────┼────", Row(2));
    }

    [Fact]
    public void ALineEndingOnALine_JoinsOnlyOnTheSideItComesFrom()
    {
        PaintUprightLineAt(5, '│');

        DrawLine(LineStyle.Box, (0, 1), (5, 1));
        DrawLine(LineStyle.Box, (9, 3), (5, 3));

        Assert.Equal("─────┤····", Row(1));
        Assert.Equal("·····├────", Row(3));
    }

    [Fact]
    public void AHeavyLineAcrossALightOne_KeepsItsOwnWeight_AndTheLightOneKeepsItsOwn()
    {
        PaintUprightLineAt(5, '│');

        DrawLine(LineStyle.HeavyBox, (0, 2), (9, 2));

        Assert.Equal("━━━━━┿━━━━", Row(2));
    }

    // No character has double arms across and heavy ones up and down.
    [Fact]
    public void ArmsNoCharacterHasTogether_AllTakeTheDrawnLinesWeight()
    {
        PaintUprightLineAt(5, '┃');

        DrawLine(LineStyle.DoubleBox, (0, 2), (9, 2));

        Assert.Equal("═════╬════", Row(2));
    }

    [Theory]
    [InlineData('│')]
    [InlineData('|')]
    [InlineData('┼')]
    public void AStandardLine_JoinsAsAPlus(char existing)
    {
        PaintUprightLineAt(5, existing);

        DrawLine(LineStyle.Standard, (0, 2), (9, 2));

        Assert.Equal("-----+----", Row(2));
    }

    [Fact]
    public void ABoxLine_JoinsAStandardOne()
    {
        PaintUprightLineAt(5, '|');

        DrawLine(LineStyle.Box, (0, 2), (9, 2));

        Assert.Equal("─────┼────", Row(2));
    }

    [Fact]
    public void ALineAlongALineOfItsWeight_LeavesItAsItWas()
    {
        TestCells.Paint(Layer, "", "", "──────────");

        DrawLine(LineStyle.Box, (2, 2), (7, 2));

        Assert.Equal("──────────", Row(2));
    }

    // Its ends are half the old line's weight, which goes on past them.
    [Fact]
    public void ALineAlongAHeavierLine_JoinsItAtItsEnds()
    {
        TestCells.Paint(Layer, "", "", "━━━━━━━━━━");

        DrawLine(LineStyle.Box, (2, 2), (7, 2));

        Assert.Equal("━━╾────╼━━", Row(2));
    }

    [Fact]
    public void RectanglesSharingAnEdge_JoinWithTees()
    {
        DrawRectangle((0, 0), (5, 3));
        DrawRectangle((5, 0), (9, 3));

        Assert.Equal(["┌────┬───┐", "│····│···│", "│····│···│", "└────┴───┘"], TestCells.Read(Layer)[..4]);
    }

    [Fact]
    public void RoundedCorners_JoinToo()
    {
        PaintUprightLineAt(5, '│');
        _settings.For(ShapeOptions.RectangleGroup).Set(ShapeOptions.RoundedCorners, true);

        DrawRectangle((5, 1), (9, 3));

        Assert.Equal(["·····│····", "·····├───╮", "·····│···│", "·····├───╯"], TestCells.Read(Layer)[..4]);
    }

    [Fact]
    public void WithoutIncludeJoins_ALineDrawsOver()
    {
        Options.Set(LineStyleOptions.IncludeJoins, false);
        PaintUprightLineAt(5, '│');

        DrawLine(LineStyle.Box, (0, 2), (9, 2));

        Assert.Equal("──────────", Row(2));
    }

    [Fact]
    public void SingleCharacter_NeverJoins()
    {
        PaintUprightLineAt(5, '│');
        _settings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, new Rune('─'));

        DrawLine(LineStyle.SingleCharacter, (0, 2), (9, 2));

        Assert.Equal("──────────", Row(2));
    }

    [Fact]
    public void OtherCharacters_AreDrawnOver()
    {
        TestCells.Paint(Layer, "", "", "·a╱/_·····");

        DrawLine(LineStyle.Box, (0, 2), (9, 2));

        Assert.Equal("──────────", Row(2));
    }

    [Fact]
    public void AShapesFill_DoesntJoin()
    {
        PaintUprightLineAt(5, '│');
        _settings.For(ShapeOptions.FillGroup).Set(ShapeOptions.Fill, "character");
        _settings.For(CharacterBrushOptions.Group).Set(CharacterBrushOptions.Character, new Rune('─'));

        DrawRectangle((2, 0), (8, 4));

        Assert.Equal("··│─────│·", Row(2));
    }

    [Fact]
    public void BoxStylesInAnAsciiDocument_AreDrawnInStandard_JoiningWithAPlus()
    {
        _session = SessionFor(CharacterSet.Ascii);
        PaintUprightLineAt(5, '|');

        DrawLine(LineStyle.Box, (0, 2), (9, 2));

        Assert.Equal("-----+----", Row(2));
    }

    // Each new end takes the last one's step back first, so a join the line has left goes back to how it was.
    [Fact]
    public void ALineJoinsWhatWasThereBeforeTheDrag()
    {
        PaintUprightLineAt(5, '│');
        var tool = LineTool();
        Options.Set(LineStyleOptions.Style, "box");

        tool.PointerPressed(Input(0, 2));
        tool.PointerMoved(Input(9, 2));
        tool.PointerMoved(Input(3, 2));
        tool.PointerReleased(Input(3, 2));

        Assert.Equal("────·│····", Row(2));
    }

    private static EditingSession SessionFor(CharacterSet characterSet) =>
        new(Document.Create(new CanvasSize(10, 5), "Layer", characterSet, colorMode: ColorMode.Ansi));

    private static LineArms Arms(string upRightDownLeft) => new(
        (LineWeight)(upRightDownLeft[0] - '0'), (LineWeight)(upRightDownLeft[1] - '0'),
        (LineWeight)(upRightDownLeft[2] - '0'), (LineWeight)(upRightDownLeft[3] - '0'));

    private void PaintUprightLineAt(int x, char glyph)
    {
        var row = new string('·', x) + glyph;
        TestCells.Paint(Layer, row, row, row, row, row);
    }

    private string Row(int y) => TestCells.Read(Layer)[y];

    private void DrawLine(LineStyle style, (int X, int Y) from, (int X, int Y) to)
    {
        Options.Set(LineStyleOptions.Style, StyleId(style));
        var tool = LineTool();
        tool.PointerPressed(Input(from.X, from.Y));
        tool.PointerMoved(Input(to.X, to.Y));
        tool.PointerReleased(Input(to.X, to.Y));
    }

    private void DrawRectangle((int X, int Y) from, (int X, int Y) to)
    {
        Options.Set(LineStyleOptions.Style, "box");
        var tool = new BoxShapeTool(BoxShape.Rectangle, _session, _colors, _settings, () => HalfAsWide,
            () => TestTools.NoInk);
        tool.PointerPressed(Input(from.X, from.Y));
        tool.PointerMoved(Input(to.X, to.Y));
        tool.PointerReleased(Input(to.X, to.Y));
    }

    private LineTool LineTool() => new(_session, _colors, Options, _settings.For(CharacterBrushOptions.Group),
        _settings.RecentGlyphs, () => HalfAsWide, () => TestTools.NoInk);

    private static string StyleId(LineStyle style) => style switch
    {
        LineStyle.Box => "box",
        LineStyle.HeavyBox => "heavyBox",
        LineStyle.DoubleBox => "doubleBox",
        LineStyle.SingleCharacter => "singleCharacter",
        _ => "standard"
    };

    private static ToolPointerInput Input(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
