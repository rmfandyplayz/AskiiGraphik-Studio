// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class GlyphAtlasTests
{
    private const float PixelSize = 24;

    [Fact]
    public void GetOrAdd_SameRuneTwice_RasterizesItOnce()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var atlas = CreateAtlas(font);

        var first = atlas.GetOrAdd(new Rune('A'));
        var second = atlas.GetOrAdd(new Rune('A'));

        Assert.Equal(first, second);
        Assert.Equal(1, atlas.GlyphCount);
    }

    [Fact]
    public void GetOrAdd_DifferentRunes_GetSeparateCellSizedSlots()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var atlas = CreateAtlas(font);

        var entries = "ABC".Select(character => atlas.GetOrAdd(new Rune(character))).ToArray();

        var cellSize = new SKSizeI(atlas.Metrics.Width, atlas.Metrics.Height);
        Assert.Equal([cellSize, cellSize, cellSize], entries.Select(entry => entry.Bounds.Size));
        Assert.Equal(3, entries.Select(entry => (entry.PageIndex, entry.Bounds)).Distinct().Count());
        Assert.DoesNotContain(entries, entry => entries.Any(other =>
            other != entry && other.PageIndex == entry.PageIndex && other.Bounds.IntersectsWith(entry.Bounds)));
    }

    [Fact]
    public void GetOrAdd_MoreGlyphsThanAPageHolds_StartsANewPage()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);

        // A page this small holds exactly one glyph.
        using var atlas = new GlyphAtlas(font, PixelSize, GlyphRenderOptions.Default, FixedFallbackFontProvider.None,
            pageSizeInPixels: 1);

        var first = atlas.GetOrAdd(new Rune('A'));
        var second = atlas.GetOrAdd(new Rune('B'));

        Assert.Equal(0, first.PageIndex);
        Assert.Equal(1, second.PageIndex);
        Assert.Equal(2, atlas.PageCount);
    }

    [Fact]
    public void GetPage_IsAnAlphaOnlyBitmap()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var atlas = CreateAtlas(font);

        atlas.GetOrAdd(new Rune('A'));

        Assert.Equal(SKColorType.Alpha8, atlas.GetPage(0).ColorType);
    }

    // Principle 4: the atlas holds exactly what the rasterizer draws anywhere else.
    [Fact]
    public void GetOrAdd_SlotMatchesDrawingTheSameGlyphDirectly()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var atlas = CreateAtlas(font);
        using var rasterizer = new GlyphRasterizer(font, PixelSize, GlyphRenderOptions.Default,
            FixedFallbackFontProvider.None);
        using var direct = TestBitmaps.CreateTransparent(rasterizer.Metrics.Width, rasterizer.Metrics.Height,
            SKColorType.Alpha8);
        using var canvas = new SKCanvas(direct);
        atlas.GetOrAdd(new Rune('x'));

        var entry = atlas.GetOrAdd(new Rune('@'));
        rasterizer.Draw(canvas, 0, 0, new Rune('@'), SKColors.Black);

        var fromAtlas = TestBitmaps.Alphas(atlas.GetPage(entry.PageIndex), entry.Bounds);
        var fromRasterizer = TestBitmaps.Alphas(direct, SKRectI.Create(0, 0, direct.Width, direct.Height));
        Assert.Equal(fromRasterizer, fromAtlas);
        Assert.Contains(fromAtlas, alpha => alpha > 0);
    }

    [Fact]
    public void GetOrAdd_GlyphNoFontHas_RecordsThePlaceholderSource()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var atlas = CreateAtlas(font);

        Assert.Equal(GlyphSource.PrimaryFont, atlas.GetOrAdd(new Rune('A')).Source);
        Assert.Equal(GlyphSource.Placeholder, atlas.GetOrAdd(TestFonts.NoFontHasThis).Source);
    }

    [Fact]
    public void GetOrAdd_AfterDispose_ThrowsObjectDisposedException()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        var atlas = CreateAtlas(font);
        atlas.Dispose();

        Assert.Throws<ObjectDisposedException>(() => atlas.GetOrAdd(new Rune('A')));
    }

    private static GlyphAtlas CreateAtlas(LoadedFont font) =>
        new(font, PixelSize, GlyphRenderOptions.Default, FixedFallbackFontProvider.None);
}
