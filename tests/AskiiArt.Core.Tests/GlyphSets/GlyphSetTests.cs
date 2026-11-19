// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.Core.Tests.GlyphSets;

public sealed class GlyphSetTests
{
    [Fact]
    public void FromText_SkipsSpacesAndLineBreaks_AndCountsARepeatOnce()
    {
        var set = GlyphSet.FromText(" a b\nc\r\na\tb ");

        Assert.Equal("abc", set.ToString());
    }

    [Fact]
    public void ACharacterBeyondTheBasicPlane_IsOneCharacter()
    {
        var set = GlyphSet.FromText("\U0001F642x");

        Assert.Equal([new Rune(0x1F642), new Rune('x')], set.Glyphs);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \n\t ")]
    public void TextWithNothingButSpaces_IsEmpty(string text)
    {
        Assert.Equal(GlyphSetTextProblem.Empty, GlyphSet.Check(text).Problem);
        Assert.Throws<ArgumentException>(() => GlyphSet.FromText(text));
    }

    [Theory]
    [InlineData("a\u0007b", 0x0007)]
    [InlineData("á", 0x0301)]
    [InlineData("ab​", 0x200B)]
    public void ACharacterThatCantBePainted_IsUnusable_AndNamed(string text, int unusable)
    {
        var check = GlyphSet.Check(text);

        Assert.Equal(GlyphSetTextProblem.Unusable, check.Problem);
        Assert.Equal(new Rune(unusable), check.Glyph);
        Assert.False(check.IsValid);
    }

    [Fact]
    public void TheConstructor_RefusesNoCharacters_AndOnesASetCantHold()
    {
        Assert.Throws<ArgumentException>(() => new GlyphSet([]));
        Assert.Throws<ArgumentException>(() => new GlyphSet([new Rune('a'), new Rune(' ')]));
        Assert.Throws<ArgumentException>(() => new GlyphSet([new Rune('\n')]));
    }

    [Fact]
    public void CanHold_TakesPrintableCharacters_NotSpacesOrControls()
    {
        Assert.True(GlyphSet.CanHold(new Rune('#')));
        Assert.True(GlyphSet.CanHold(new Rune(0x2588)));
        Assert.False(GlyphSet.CanHold(new Rune(' ')));
        Assert.False(GlyphSet.CanHold(new Rune(0x00A0)));
        Assert.False(GlyphSet.CanHold(new Rune(0x001B)));
    }

    [Fact]
    public void FromRange_LeavesOutUnassignedCharacters()
    {
        // U+0378 and U+0379 are unassigned; U+037A is the Greek ypogegrammeni.
        var set = GlyphSet.FromRange(0x0378, 0x037A);

        Assert.Equal([new Rune(0x037A)], set.Glyphs);
    }

    [Fact]
    public void FromRange_LeavesOutPrivateUseCharacters_SoARangeOfOnlyThoseHoldsNone()
    {
        Assert.Throws<ArgumentException>(() => GlyphSet.FromRange(0xE000, 0xE0FF));
    }

    [Fact]
    public void SetsWithTheSameCharactersInTheSameOrder_AreEqual()
    {
        Assert.Equal(GlyphSet.FromText("ab"), new GlyphSet([new Rune('a'), new Rune('b')]));
        Assert.Equal(GlyphSet.FromText("ab").GetHashCode(), GlyphSet.FromText("a b").GetHashCode());
        Assert.NotEqual(GlyphSet.FromText("ab"), GlyphSet.FromText("ba"));
    }
}
