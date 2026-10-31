// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CellTests
{
    [Fact]
    public void Default_IsTheEmptyCell()
    {
        var cell = default(Cell);

        Assert.True(cell.IsEmpty);
        Assert.False(cell.IsOccupied);
        Assert.Equal(Cell.Empty, cell);
        Assert.Equal(0, cell.Alpha);
    }

    [Fact]
    public void FromGlyph_MakesAnOccupiedCellWithTheReservedFieldsAtTheirDefaults()
    {
        var cell = Cell.FromGlyph(new Rune('A'));

        Assert.True(cell.IsOccupied);
        Assert.Equal(new Rune('A'), cell.Glyph);
        Assert.True(cell.Foreground.IsUnset);
        Assert.True(cell.Background.IsUnset);
        Assert.Equal(CellAttributes.None, cell.Attributes);
        Assert.Equal(Cell.OpaqueAlpha, cell.Alpha);
    }

    [Fact]
    public void FromGlyph_WithASpace_IsOccupiedAndNotEqualToEmpty()
    {
        var space = Cell.FromGlyph(new Rune(' '));

        Assert.True(space.IsOccupied);
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
    public void Size_IsWithinTheSixteenByteTarget()
    {
        Assert.InRange(Unsafe.SizeOf<Cell>(), 1, 16);
    }
}
