// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.Core.Tests.GlyphSets;

public sealed class BuiltInGlyphSetsTests
{
    [Fact]
    public void EachSet_HasItsOwnIdAndName()
    {
        Assert.Equal(BuiltInGlyphSets.All.Count, BuiltInGlyphSets.All.Select(set => set.Id).Distinct().Count());
        Assert.Equal(BuiltInGlyphSets.All.Count, BuiltInGlyphSets.All.Select(set => set.NameKey).Distinct().Count());
        Assert.All(BuiltInGlyphSets.All,
            set => Assert.StartsWith("askiiart.glyphs.", set.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void BasicLatin_IsFirst_AndHoldsThePrintableAsciiCharacters()
    {
        Assert.Same(BuiltInGlyphSets.BasicLatin, BuiltInGlyphSets.All[0]);
        Assert.Equal(94, BuiltInGlyphSets.BasicLatin.Glyphs.Glyphs.Count);
        Assert.Equal(new Rune('!'), BuiltInGlyphSets.BasicLatin.Glyphs.Glyphs[0]);
        Assert.Equal(new Rune('~'), BuiltInGlyphSets.BasicLatin.Glyphs.Glyphs[^1]);
    }

    [Fact]
    public void TheSets_HoldTheirBlocksCharacters()
    {
        Assert.Contains(new Rune(0x2500), Find("boxDrawing").Glyphs);
        Assert.Contains(new Rune(0x2593), Find("blocks").Glyphs);
        Assert.Contains(new Rune(0x28FF), Find("braille").Glyphs);
        Assert.DoesNotContain(new Rune(0x0378), Find("greek").Glyphs);
    }

    [Fact]
    public void Find_GivesASetByItsId()
    {
        Assert.Same(BuiltInGlyphSets.BasicLatin, BuiltInGlyphSets.Find(BuiltInGlyphSets.BasicLatin.Id));
        Assert.Null(BuiltInGlyphSets.Find("user.1"));
    }

    private static GlyphSet Find(string name) => BuiltInGlyphSets.Find("askiiart.glyphs." + name)!.Glyphs;
}
