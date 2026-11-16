// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Ramps;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class GlyphDensityMeterTests
{
    // The bundled fonts that have the block characters themselves. Roboto Mono doesn't, and the tests use no fallback
    // font, so a system's fonts can't change the results.
    public static TheoryData<string> FontsWithBlocks => ["JetBrains Mono", "IBM Plex Mono", "Fira Code"];

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void ASpace_MeasuresNothing(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        Assert.Equal(0, meter.GetDensity(new Rune(' ')));
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void ADot_IsLighterThanAColon_WhichIsLighterThanAnAtSign(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        Assert.InRange(meter.GetDensity(new Rune('.')), 0.001, meter.GetDensity(new Rune(':')));
        Assert.True(meter.GetDensity(new Rune(':')) < meter.GetDensity(new Rune('@')));
    }

    [Theory]
    [MemberData(nameof(FontsWithBlocks))]
    public void BlockCharacters_MeasureTheShareOfTheCellTheyAreFittedTo(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        Assert.Equal(1, meter.GetDensity(new Rune('█')), 0.001);
        Assert.Equal(0.5, meter.GetDensity(new Rune('▀')), 0.02);
        Assert.Equal(0.5, meter.GetDensity(new Rune('▌')), 0.02);
        Assert.Equal(0.25, meter.GetDensity(new Rune('▖')), 0.02);
    }

    [Theory]
    [MemberData(nameof(FontsWithBlocks))]
    public void TheShades_GetDarkerFromLightToFull(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        var sorted = BuiltInRamps.Blocks.Ramp.SortedByDensity(meter);

        Assert.Equal(BuiltInRamps.Blocks.Ramp, sorted);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void SortingStandard_KeepsTheDotLightestAndTheAtSignDarkest(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        var sorted = BuiltInRamps.Standard.Ramp.SortedByDensity(meter);

        Assert.Equal(new Rune('.'), sorted.Glyphs[0]);
        Assert.Equal(new Rune('@'), sorted.Glyphs[^1]);
    }

    [Fact]
    public void AGlyphNoFontHas_IsMeasuredAsThePlaceholderBoxTheCanvasDraws()
    {
        using var font = TestFonts.Load("JetBrains Mono");
        using var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);

        Assert.Equal(GlyphSource.Placeholder, meter.GetSource(TestFonts.NoFontHasThis));
        Assert.InRange(meter.GetDensity(TestFonts.NoFontHasThis), 0.01, 0.5);
    }

    [Fact]
    public void AGlyphFromAFallbackFont_IsMeasuredInThatFont()
    {
        using var primary = TestFonts.Load("Roboto Mono");
        using var fallback = TestFonts.Load("JetBrains Mono");
        using var meter = new GlyphDensityMeter(primary, new FixedFallbackFontProvider(fallback));
        using var fallbackMeter = new GlyphDensityMeter(fallback, FixedFallbackFontProvider.None);

        Assert.Equal(GlyphSource.FallbackFont, meter.GetSource(new Rune('█')));
        Assert.Equal(fallbackMeter.GetDensity(new Rune('█')), meter.GetDensity(new Rune('█')), 0.001);
    }

    [Fact]
    public void GetDensity_AfterDispose_Throws()
    {
        using var font = TestFonts.Load("JetBrains Mono");
        var meter = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);
        meter.Dispose();

        Assert.Throws<ObjectDisposedException>(() => meter.GetDensity(new Rune('.')));
    }
}
