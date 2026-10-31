// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class GlyphAtlasCacheTests
{
    [Fact]
    public void GetAtlas_SameFontSizeAndEqualOptions_ReturnsTheSameAtlas()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var cache = new GlyphAtlasCache(FixedFallbackFontProvider.None);

        var first = cache.GetAtlas(font, 16, new GlyphRenderOptions { Hinting = GlyphHinting.Normal });
        var second = cache.GetAtlas(font, 16, new GlyphRenderOptions { Hinting = GlyphHinting.Normal });

        Assert.Same(first, second);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void GetAtlas_DifferentSizeFontOrOptions_ReturnsDifferentAtlases()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var otherFont = FontLoader.LoadBundled(BundledFonts.FiraCode);
        using var cache = new GlyphAtlasCache(FixedFallbackFontProvider.None);

        GlyphAtlas[] atlases =
        [
            cache.GetAtlas(font, 16, GlyphRenderOptions.Default),
            cache.GetAtlas(font, 32, GlyphRenderOptions.Default),
            cache.GetAtlas(otherFont, 16, GlyphRenderOptions.Default),
            cache.GetAtlas(font, 16, new GlyphRenderOptions { Antialias = false })
        ];

        Assert.Equal(4, atlases.Distinct().Count());
        Assert.Equal(4, cache.Count);
    }

    [Fact]
    public void GetAtlas_OverCapacity_DisposesTheLeastRecentlyUsedAtlas()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var cache = new GlyphAtlasCache(FixedFallbackFontProvider.None, capacity: 2);
        var small = cache.GetAtlas(font, 10, GlyphRenderOptions.Default);
        var medium = cache.GetAtlas(font, 20, GlyphRenderOptions.Default);

        cache.GetAtlas(font, 10, GlyphRenderOptions.Default); // small is now the most recently used
        cache.GetAtlas(font, 30, GlyphRenderOptions.Default);

        Assert.Equal(2, cache.Count);
        Assert.Throws<ObjectDisposedException>(() => medium.GetOrAdd(new Rune('A')));
        Assert.Same(small, cache.GetAtlas(font, 10, GlyphRenderOptions.Default));
    }

    [Fact]
    public void Dispose_DisposesEveryCachedAtlas()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        var cache = new GlyphAtlasCache(FixedFallbackFontProvider.None);
        var atlas = cache.GetAtlas(font, 16, GlyphRenderOptions.Default);

        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => atlas.GetOrAdd(new Rune('A')));
        Assert.Throws<ObjectDisposedException>(() => cache.GetAtlas(font, 16, GlyphRenderOptions.Default));
    }
}
