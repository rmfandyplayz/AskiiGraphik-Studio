// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class GlyphDensityCacheTests
{
    [Fact]
    public void GetMeter_TheSameFont_ReturnsTheSameMeter()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var cache = new GlyphDensityCache(FixedFallbackFontProvider.None);

        Assert.Same(cache.GetMeter(font), cache.GetMeter(font));
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void GetMeter_PastCapacity_DisposesTheLeastRecentlyUsedMeter()
    {
        using var first = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);
        using var second = FontLoader.LoadBundled(BundledFonts.FiraCode);
        using var third = FontLoader.LoadBundled(BundledFonts.IbmPlexMono);
        using var cache = new GlyphDensityCache(FixedFallbackFontProvider.None, capacity: 2);

        var firstMeter = cache.GetMeter(first);
        var secondMeter = cache.GetMeter(second);
        cache.GetMeter(first);
        cache.GetMeter(third);

        Assert.Equal(2, cache.Count);
        Assert.Same(firstMeter, cache.GetMeter(first));
        Assert.Throws<ObjectDisposedException>(() => secondMeter.GetDensity(new Rune('.')));
    }

    [Fact]
    public void Dispose_DisposesEveryMeter()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        var cache = new GlyphDensityCache(FixedFallbackFontProvider.None);
        var meter = cache.GetMeter(font);

        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => meter.GetDensity(new Rune('.')));
        Assert.Throws<ObjectDisposedException>(() => cache.GetMeter(font));
    }
}
