// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using GlyphAdvance = AskiiArt.Imaging.Fonts.MonospaceCheck.GlyphAdvance;

namespace AskiiArt.Imaging.Tests.Fonts;

public sealed class MonospaceCheckTests
{
    private const float EmSize = 1000;

    [Fact]
    public void Evaluate_EveryGlyphAsWideAsTheReference_IsMonospace()
    {
        var result = MonospaceCheck.Evaluate([Advance('0', 600), Advance('i', 600), Advance('W', 600)], EmSize);

        Assert.True(result.IsMonospace);
        Assert.Empty(result.MismatchedGlyphs);
    }

    [Fact]
    public void Evaluate_SomeGlyphsWiderOrNarrower_ReportsExactlyThose()
    {
        var result = MonospaceCheck.Evaluate(
            [Advance('0', 600), Advance('i', 250), Advance('m', 600), Advance('W', 900)], EmSize);

        Assert.False(result.IsMonospace);
        Assert.Equal([new Rune('i'), new Rune('W')], result.MismatchedGlyphs);
    }

    [Fact]
    public void Evaluate_DifferenceWithinOnePercentOfAnEm_StillCountsAsMonospace()
    {
        var result = MonospaceCheck.Evaluate([Advance('0', 600), Advance('i', 609)], EmSize);

        Assert.True(result.IsMonospace);
    }

    [Fact]
    public void Evaluate_NoReferenceGlyph_ComparesAgainstTheMostCommonAdvance()
    {
        var result = MonospaceCheck.Evaluate(
            [Advance('a', 500), Advance('b', 500), Advance('c', 500), Advance('W', 800)], EmSize);

        Assert.Equal([new Rune('W')], result.MismatchedGlyphs);
    }

    [Fact]
    public void Evaluate_NoGlyphsAtAll_HasNothingToComplainAbout()
    {
        Assert.True(MonospaceCheck.Evaluate([], EmSize).IsMonospace);
    }

    private static GlyphAdvance Advance(char character, float advance) => new(new Rune(character), advance);
}
