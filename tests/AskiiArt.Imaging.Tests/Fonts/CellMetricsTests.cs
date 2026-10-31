// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Tests.Fonts;

public sealed class CellMetricsTests
{
    // JetBrains Mono: 1000 units per em, advance 600, ascent 1020, descent 300, no line gap. At
    // 20 px: width 12, height 26.4 → 26, baseline 20.4 → 20. Being exact here (not "about")
    // also proves the numbers don't depend on the operating system's font engine.
    [Fact]
    public void Measure_JetBrainsMonoAt20Pixels_MatchesTheFontFile()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);

        var metrics = CellMetrics.Measure(font, 20);

        Assert.Equal(20, metrics.PixelSize);
        Assert.Equal(12, metrics.Width);
        Assert.Equal(26, metrics.Height);
        Assert.Equal(20, metrics.Baseline);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void Measure_BundledFont_GivesATallNarrowCellWithTheBaselineInside(string familyName)
    {
        using var font = TestFonts.Load(familyName);

        var metrics = CellMetrics.Measure(font, 16);

        Assert.InRange(metrics.Width, 8, 11);
        Assert.InRange(metrics.Height, metrics.Width + 1, 32);
        Assert.InRange(metrics.Baseline, metrics.Height / 2, metrics.Height - 1);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void Measure_DoublingThePixelSize_RoughlyDoublesTheCell(string familyName)
    {
        using var font = TestFonts.Load(familyName);

        var small = CellMetrics.Measure(font, 16);
        var large = CellMetrics.Measure(font, 32);

        Assert.InRange(large.Width, 2 * small.Width - 1, 2 * small.Width + 1);
        Assert.InRange(large.Height, 2 * small.Height - 1, 2 * small.Height + 1);
    }

    [Fact]
    public void Measure_TinyPixelSize_StillGivesAtLeastOnePixel()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);

        var metrics = CellMetrics.Measure(font, CellMetrics.MinimumPixelSize);

        Assert.True(metrics.Width >= 1);
        Assert.True(metrics.Height >= 1);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(-4f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(CellMetrics.MaximumPixelSize + 1)]
    public void Measure_PixelSizeOutOfRange_Throws(float pixelSize)
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);

        Assert.Throws<ArgumentOutOfRangeException>(() => CellMetrics.Measure(font, pixelSize));
    }
}
