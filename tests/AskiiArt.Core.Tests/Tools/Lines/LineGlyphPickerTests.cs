// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tests.Ramps;
using AskiiArt.Core.Tools.Lines;

namespace AskiiArt.Core.Tests.Tools.Lines;

// The characters a line or an outline gets in each style (step 5.1). Cells here are half as wide as they're tall, so
// \ and / stand for lines going one column per row, and a line going two columns per row looks like 45°.
public sealed class LineGlyphPickerTests
{
    private const double HalfAsWide = 0.5;
    private static readonly Rune _star = new('*');

    // Glyphs whose ink lies in plain places: - across the middle, _ along the bottom, ' at the top, | down the
    // middle, and / and \ corner to corner. Other candidates show no ink, so they're never picked.
    private static readonly FakeGlyphDensities _glyphs = new(
        new Dictionary<char, double>(),
        new Dictionary<char, GlyphShape>
        {
            ['-'] = ShapeWhere((column, row) => row is 3 or 4 && column is 1 or 2),
            ['_'] = ShapeWhere((_, row) => row is 6 or 7),
            ['\''] = ShapeWhere((column, row) => row <= 2 && column is 1 or 2),
            ['|'] = ShapeWhere((column, _) => column is 1 or 2),
            ['/'] = ShapeWhere((column, row) => column == (7 - row) / 2),
            ['\\'] = ShapeWhere((column, row) => column == row / 2)
        });

    [Theory]
    [InlineData(8, 0, '-')]
    [InlineData(0, 4, '|')]
    [InlineData(4, 4, '\\')]
    [InlineData(4, -4, '/')]
    [InlineData(-4, 4, '/')]
    [InlineData(8, 4, '\\')]
    [InlineData(8, 2, '-')]
    [InlineData(2, 8, '|')]
    public void Standard_GivesEveryCellTheCharacterWhoseAngleOnScreenIsNearest(int across, int down, char expected)
    {
        var glyphs = Pick(LinePath.Straight(Middle(10, 10), Middle(10 + across, 10 + down)), LineStyle.Standard);

        Assert.All(glyphs, glyph => Assert.Equal(new Rune(expected), glyph.Glyph));
    }

    [Theory]
    [InlineData(LineStyle.Standard)]
    [InlineData(LineStyle.StandardByStep)]
    [InlineData(LineStyle.Precise)]
    public void ARectangle_HasPlusCorners(LineStyle style)
    {
        var glyphs = Pick(LinePath.Rectangle(new CellRect(0, 0, 6, 4)), style).ToDictionary(g => g.Cell, g => g.Glyph);

        Assert.Equal(new Rune('+'), glyphs[new CellPoint(0, 0)]);
        Assert.Equal(new Rune('+'), glyphs[new CellPoint(5, 0)]);
        Assert.Equal(new Rune('+'), glyphs[new CellPoint(5, 3)]);
        Assert.Equal(new Rune('+'), glyphs[new CellPoint(0, 3)]);
        Assert.Equal(new Rune('-'), glyphs[new CellPoint(2, 0)]);
        Assert.Equal(new Rune('|'), glyphs[new CellPoint(5, 2)]);
    }

    // A line going eight columns down a row looks flat, so Standard draws it all in -, while by step shows the step:
    // the cells either side of it each lie between cells a row apart.
    [Fact]
    public void StandardByStep_ShowsEachStepAShallowLineTakes()
    {
        var path = LinePath.Straight(Middle(0, 0), Middle(8, 1));

        var standard = Pick(path, LineStyle.Standard);
        var byStep = Pick(path, LineStyle.StandardByStep);

        Assert.All(standard, glyph => Assert.Equal(new Rune('-'), glyph.Glyph));
        Assert.Equal("---\\\\----", string.Concat(byStep.Select(glyph => glyph.Glyph.ToString())));
        Assert.Equal(new CellPoint(3, 0), byStep[3].Cell);
        Assert.Equal(new CellPoint(4, 1), byStep[4].Cell);
    }

    [Fact]
    public void SingleCharacter_PutsTheChosenCharacterInEveryCell()
    {
        var glyphs = Pick(LinePath.Ellipse(new CellRect(0, 0, 12, 6)), LineStyle.SingleCharacter);

        Assert.NotEmpty(glyphs);
        Assert.All(glyphs, glyph => Assert.Equal(_star, glyph.Glyph));
    }

    // Glyphs ink their cells at many heights, so a flat line keeps the height it runs at.
    [Theory]
    [InlineData(0.5, '-')]
    [InlineData(0.85, '_')]
    [InlineData(0.15, '\'')]
    public void Precise_PicksTheCharacterInkedWhereAFlatLineRuns(double height, char expected)
    {
        var glyphs = Pick(LinePath.Straight(new CanvasPoint(0.5, 2 + height), new CanvasPoint(9.5, 2 + height)),
            LineStyle.Precise);

        Assert.All(glyphs, glyph => Assert.Equal(new Rune(expected), glyph.Glyph));
    }

    // Every glyph inks the middle of its cell across, so an upright line near a cell's side is matched as if centred.
    [Fact]
    public void Precise_MatchesAnUprightLineNearACellsSideAsIfCentred()
    {
        var glyphs = Pick(LinePath.Straight(new CanvasPoint(3.1, 0.5), new CanvasPoint(3.1, 6.5)), LineStyle.Precise);

        Assert.All(glyphs, glyph => Assert.Equal(new Rune('|'), glyph.Glyph));
    }

    [Fact]
    public void Precise_PicksTheDiagonalAlongALineGoingOneColumnPerRow()
    {
        var glyphs = Pick(LinePath.Straight(Middle(0, 0), Middle(5, 5)), LineStyle.Precise);

        Assert.All(glyphs, glyph => Assert.Equal(new Rune('\\'), glyph.Glyph));
    }

    [Fact]
    public void Precise_WithNoCandidateShowingInk_DrawsStandard()
    {
        var blank = new FakeGlyphDensities(new Dictionary<char, double>());
        var picker = new LineGlyphPicker(HalfAsWide, blank, CharacterSet.Ascii);

        var glyphs = picker.Pick(LinePath.Straight(Middle(0, 0), Middle(5, 5)), LineStyle.Precise, _star);

        Assert.All(glyphs, glyph => Assert.Equal(new Rune('\\'), glyph.Glyph));
    }

    [Fact]
    public void ACellWidthOfZeroOrLess_IsRefused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LineGlyphPicker(0, _glyphs, CharacterSet.Ascii));
    }

    private static IReadOnlyList<LineGlyph> Pick(LinePath path, LineStyle style) =>
        new LineGlyphPicker(HalfAsWide, _glyphs, CharacterSet.Ascii).Pick(path, style, _star);

    private static CanvasPoint Middle(int x, int y) => new(x + 0.5, y + 0.5);

    private static GlyphShape ShapeWhere(Func<int, int, bool> inked)
    {
        var parts = new double[GlyphShape.PartCount];
        for (var row = 0; row < GlyphShape.Rows; row++)
        {
            for (var column = 0; column < GlyphShape.Columns; column++)
                parts[row * GlyphShape.Columns + column] = inked(column, row) ? 1 : 0;
        }

        return new GlyphShape(parts);
    }
}
