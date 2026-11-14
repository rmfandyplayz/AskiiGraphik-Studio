// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CellTests
{
    private static CellColor Red => TestCells.Ansi(AnsiColor.Red);

    private static CellColor Blue => TestCells.Ansi(AnsiColor.Blue);

    [Fact]
    public void Default_IsTheEmptyCell()
    {
        var cell = default(Cell);

        Assert.True(cell.IsEmpty);
        Assert.False(cell.HasGlyph);
        Assert.Equal(Cell.Empty, cell);
        Assert.Equal(0, cell.Alpha);
    }

    [Fact]
    public void FromGlyph_MakesAnOccupiedCellWithTheReservedFieldsAtTheirDefaults()
    {
        var cell = Cell.FromGlyph(new Rune('A'));

        Assert.True(cell.HasGlyph);
        Assert.Equal(new Rune('A'), cell.Glyph);
        Assert.True(cell.Foreground.IsUnset);
        Assert.True(cell.Background.IsUnset);
        Assert.Equal(CellAttributes.None, cell.Attributes);
        Assert.Equal(Cell.OpaqueAlpha, cell.Alpha);
    }

    [Fact]
    public void FromGlyph_WithASpace_HasGlyphAndNotEqualToEmpty()
    {
        var space = Cell.FromGlyph(new Rune(' '));

        Assert.True(space.HasGlyph);
        Assert.NotEqual(Cell.Empty, space);
    }

    [Fact]
    public void FromGlyph_AcceptsCharactersOutsideTheBasicMultilingualPlane()
    {
        var emoji = new Rune(0x1F600);

        Assert.Equal(emoji, Cell.FromGlyph(emoji).Glyph);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x1B)]
    [InlineData(0x7F)]
    [InlineData(0x85)]
    public void FromGlyph_RejectsControlCharacters(int codePoint)
    {
        Assert.Throws<ArgumentException>(() => Cell.FromGlyph(new Rune(codePoint)));
    }

    [Fact]
    public void Equality_ComparesOccupancyAndGlyph()
    {
        var first = TestCells.Glyph('x');
        var second = TestCells.Glyph('x');
        var other = TestCells.Glyph('y');

        Assert.True(first == second);
        Assert.True(first != other);
        Assert.False(first.Equals(Cell.Empty));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void FromGlyph_WithColors_KeepsBoth()
    {
        var cell = Cell.FromGlyph(new Rune('A'), Red, Blue);

        Assert.Equal(Red, cell.Foreground);
        Assert.Equal(Blue, cell.Background);
        Assert.True(cell.HasBackground);
    }

    [Fact]
    public void FromBackground_HoldsAColorWithoutAGlyph_AndIsNotEmpty()
    {
        var cell = Cell.FromBackground(Blue);

        Assert.False(cell.HasGlyph);
        Assert.True(cell.HasBackground);
        Assert.False(cell.IsEmpty);
        Assert.True(cell.Foreground.IsUnset);
        Assert.Equal(Cell.OpaqueAlpha, cell.Alpha);
        Assert.Equal(Cell.Empty, Cell.FromBackground(CellColor.Unset));
    }

    [Fact]
    public void WithForeground_OnACellWithoutAGlyph_Throws_UnlessTheColorIsUnset()
    {
        Assert.Throws<InvalidOperationException>(() => Cell.Empty.WithForeground(Red));
        Assert.Throws<InvalidOperationException>(() => Cell.FromBackground(Blue).WithForeground(Red));
        Assert.Equal(Cell.FromBackground(Blue), Cell.FromBackground(Blue).WithForeground(CellColor.Unset));
    }

    [Fact]
    public void WithBackground_AddsOrRemovesABackground()
    {
        Assert.Equal(Cell.FromBackground(Red), Cell.Empty.WithBackground(Red));
        Assert.Equal(Cell.Empty, Cell.FromBackground(Blue).WithBackground(CellColor.Unset));
        Assert.Equal(TestCells.Glyph('A', Red, Blue), TestCells.Glyph('A', Red, Red).WithBackground(Blue));
    }

    [Fact]
    public void WithoutGlyph_KeepsTheBackground_AndDropsTheTextColor()
    {
        Assert.Equal(Cell.FromBackground(Blue), Cell.FromGlyph(new Rune('A'), Red, Blue).WithoutGlyph());
        Assert.Equal(Cell.Empty, Cell.FromGlyph(new Rune('A'), Red, CellColor.Unset).WithoutGlyph());
    }

    [Fact]
    public void Equality_ComparesTheColorsToo()
    {
        var plain = TestCells.Glyph('x');
        var red = Cell.FromGlyph(new Rune('x'), Red, CellColor.Unset);
        var onRed = Cell.FromGlyph(new Rune('x'), CellColor.Unset, Red);

        Assert.NotEqual(plain, red);
        Assert.NotEqual(red, onRed);
        Assert.NotEqual(Cell.Empty, Cell.FromBackground(Red));
        Assert.Equal(red, plain.WithForeground(Red));
        Assert.Equal(red.GetHashCode(), plain.WithForeground(Red).GetHashCode());
    }

    [Fact]
    public void ToString_NamesTheGlyphAndItsColors()
    {
        Assert.Equal("(empty)", Cell.Empty.ToString());
        Assert.Equal("'A'", TestCells.Glyph('A').ToString());
        Assert.Equal("'A' in Red on #0000ff", Cell.FromGlyph(new Rune('A'), Red, TestCells.Rgb(0, 0, 255)).ToString());
        Assert.Equal("(no glyph) on Blue", Cell.FromBackground(Blue).ToString());
    }

    [Fact]
    public void Size_IsWithinTheSixteenByteTarget()
    {
        Assert.InRange(Unsafe.SizeOf<Cell>(), 1, 16);
    }
}
