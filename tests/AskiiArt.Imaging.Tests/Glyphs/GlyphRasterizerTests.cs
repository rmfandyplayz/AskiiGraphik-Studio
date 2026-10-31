// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

public sealed class GlyphRasterizerTests
{
    private const float PixelSize = 32;

    [Fact]
    public void Draw_LetterFromTheDocumentFont_ReportsPrimaryAndInksTheCell()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, new Rune('M'), SKColors.Black);

        Assert.Equal(GlyphSource.PrimaryFont, source);
        Assert.True(TestBitmaps.CountInk(bitmap) > 0);
    }

    [Fact]
    public void Draw_Space_LeavesTheCellBlank()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, new Rune(' '), SKColors.Black);

        Assert.Equal(GlyphSource.PrimaryFont, source);
        Assert.Equal(0, TestBitmaps.CountInk(bitmap));
    }

    [Fact]
    public void Draw_FullBlockInTheMiddleCell_NeverTouchesTheNeighbouringCells()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 3);
        using var canvas = new SKCanvas(bitmap);
        var (width, height) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);

        rasterizer.Draw(canvas, width, 0, new Rune('█'), SKColors.Black);

        Assert.Equal(0, TestBitmaps.CountInk(bitmap, SKRectI.Create(0, 0, width, height)));
        Assert.Equal(0, TestBitmaps.CountInk(bitmap, SKRectI.Create(2 * width, 0, width, height)));
        Assert.True(TestBitmaps.CountInk(bitmap, SKRectI.Create(width, 0, width, height)) > width * height / 2);
    }

    [Fact]
    public void Draw_UsesTheInkColour()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        rasterizer.Draw(canvas, 0, 0, new Rune('█'), SKColors.Red);

        var middle = bitmap.GetPixel(rasterizer.Metrics.Width / 2, rasterizer.Metrics.Baseline - 2);
        Assert.Equal(SKColors.Red, middle);
    }

    [Fact]
    public void Draw_GlyphOnlyTheFallbackFontHas_ReportsFallbackAndInksTheCell()
    {
        using var primary = FontLoader.LoadBundled(BundledFonts.RobotoMono);
        using var fallback = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);
        var rune = TestFonts.FindRuneOnlyIn(fallback, primary);
        using var rasterizer = CreateRasterizer(primary, new FixedFallbackFontProvider(fallback));
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, rune, SKColors.Black);

        Assert.Equal(GlyphSource.FallbackFont, source);
        Assert.Equal(GlyphSource.FallbackFont, rasterizer.GetSource(rune));
        Assert.True(TestBitmaps.CountInk(bitmap) > 0);
    }

    [Fact]
    public void Draw_GlyphNoFontHas_DrawsAHollowPlaceholderBox()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, TestFonts.NoFontHasThis, SKColors.Black);

        Assert.Equal(GlyphSource.Placeholder, source);
        var box = TestBitmaps.InkBounds(bitmap, SKRectI.Create(0, 0, bitmap.Width, bitmap.Height));
        Assert.False(box.IsEmpty);
        Assert.Equal(0, bitmap.GetPixel(box.MidX, box.MidY).Alpha);
    }

    [Fact]
    public void Draw_EmojiOutsideTheBasicPlane_DoesNotThrow()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, new Rune(0x1F600), SKColors.Black);

        Assert.Equal(GlyphSource.Placeholder, source);
    }

    [Fact]
    public void Draw_WithoutAntialiasing_InksEachPixelFullyOrNotAtAll()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        var options = new GlyphRenderOptions { Antialias = false };
        using var rasterizer = new GlyphRasterizer(font, PixelSize, options, FixedFallbackFontProvider.None);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        rasterizer.Draw(canvas, 0, 0, new Rune('@'), SKColors.Black);

        var alphas = TestBitmaps.Alphas(bitmap, SKRectI.Create(0, 0, bitmap.Width, bitmap.Height));
        Assert.All(alphas, alpha => Assert.True(alpha is 0 or 255, $"alpha {alpha}"));
        Assert.Contains(alphas, alpha => alpha == 255);
    }

    // A CJK character from a system font is about twice as wide as a cell. Without shrinking it
    // would be cut off on both sides.
    [Fact]
    public void Draw_WideFallbackGlyph_IsShrunkToFitInsideTheCell()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);
        using var systemFonts = new SystemFallbackFontProvider();
        var rune = new Rune(0x6F22);
        Assert.SkipWhen(systemFonts.FindFontFor(rune, font) is null, "No installed font has a CJK glyph.");
        using var rasterizer = new GlyphRasterizer(font, 64, GlyphRenderOptions.Default, systemFonts);
        using var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);

        var source = rasterizer.Draw(canvas, 0, 0, rune, SKColors.Black);

        Assert.Equal(GlyphSource.FallbackFont, source);
        var ink = TestBitmaps.InkBounds(bitmap, SKRectI.Create(0, 0, bitmap.Width, bitmap.Height));
        Assert.False(ink.Left == 0 && ink.Right == bitmap.Width, $"Ink spans the whole cell: {ink}");
    }

    [Theory]
    [InlineData(10, 10, 20, 1)] // fits exactly
    [InlineData(20, 10, 20, 0.5)] // twice as wide
    [InlineData(10, 40, 20, 0.5)] // twice as tall
    [InlineData(0, 0, 20, 1)] // no ink, no advance
    public void ScaleToFit_ShrinksOnlyWhatDoesNotFit(float advance, float inkHeight, int cellHeight, float expected)
    {
        var inkBounds = SKRect.Create(0, -inkHeight, advance, inkHeight);

        var scale = GlyphRasterizer.ScaleToFit(advance, inkBounds, new SKSizeI(10, cellHeight));

        Assert.Equal(expected, scale, 0.0001f);
    }

    [Fact]
    public void Constructor_PixelSizeOutOfRange_Throws()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GlyphRasterizer(font, 0, GlyphRenderOptions.Default, FixedFallbackFontProvider.None));
    }

    private static GlyphRasterizer CreateRasterizer(LoadedFont font, IFallbackFontProvider? fallbackFonts = null) =>
        new(font, PixelSize, GlyphRenderOptions.Default, fallbackFonts ?? FixedFallbackFontProvider.None);

    private static SKBitmap CreateCellRow(GlyphRasterizer rasterizer, int cells) =>
        TestBitmaps.CreateTransparent(rasterizer.Metrics.Width * cells, rasterizer.Metrics.Height);
}
