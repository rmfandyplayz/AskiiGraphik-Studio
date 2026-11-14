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

    // Skia can round a pixel's coverage a level differently on one side of a cell than on the other, which no one can
    // see, so lines count as the same when no pixel differs by more than this.
    private const int UnnoticeableAlphaDifference = 2;

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

    // Font sizes the canvas uses at its zoom steps, where neighbouring blocks showed seams in step 2b.2's test.
    private static readonly float[] _seamSizes = [5.28f, 8, 13.4f, 15, 16, 21.44f, 24, 30, 32, 48, 80, 96, 128];

    /// <summary>Every bundled font at each of <see cref="_seamSizes"/>.</summary>
    public static TheoryData<string, float> BundledFontsAtSeamSizes =>
        [.. BundledFonts.All.SelectMany(font => _seamSizes.Select(size => (font.FamilyName, size)))];

    [Theory]
    [MemberData(nameof(BundledFontsAtSeamSizes))]
    public void Draw_BlockElements_FillTheirPartOfTheCellToItsEdges(string familyName, float pixelSize)
    {
        using var fonts = new FontWithBlocks(familyName);
        using var rasterizer = fonts.CreateRasterizer(pixelSize);
        var (width, height) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);

        // Leaving a row or column next to a half's inner edge, where a font may round its own way.
        AssertSolid(rasterizer, '█', SKRectI.Create(0, 0, width, height));
        AssertSolid(rasterizer, '▀', SKRectI.Create(0, 0, width, height / 2 - 1));
        AssertSolid(rasterizer, '▄', SKRectI.Create(0, height - (height / 2 - 1), width, height / 2 - 1));
        AssertSolid(rasterizer, '▌', SKRectI.Create(0, 0, width / 2 - 1, height));
        AssertSolid(rasterizer, '▐', SKRectI.Create(width - (width / 2 - 1), 0, width / 2 - 1, height));
    }

    [Theory]
    [MemberData(nameof(BundledFontsAtSeamSizes))]
    public void Draw_BoxDrawingLines_MeetTheNextCellWithoutAGap(string familyName, float pixelSize)
    {
        using var fonts = new FontWithBlocks(familyName);
        using var rasterizer = fonts.CreateRasterizer(pixelSize);
        var (width, height) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);
        var columns = Enumerable.Range(0, width).Select(x => SKRectI.Create(x, 0, 1, height)).ToArray();
        var rows = Enumerable.Range(0, height).Select(y => SKRectI.Create(0, y, width, 1)).ToArray();

        // A line that reaches the cell's edge is as strong in the edge's pixels as just inside them; one that falls
        // short fades there, and that fade is the seam.
        foreach (var glyph in new[] { '─', '═' })
        {
            AssertSameLine(rasterizer, glyph, columns[0], columns[1]);
            AssertSameLine(rasterizer, glyph, columns[^1], columns[^2]);
        }

        foreach (var glyph in new[] { '│', '║' })
        {
            AssertSameLine(rasterizer, glyph, rows[0], rows[1]);
            AssertSameLine(rasterizer, glyph, rows[^1], rows[^2]);
        }

        // A crossing has its other line in the middle, so only its two edges, where the next cells carry the lines
        // on, are compared.
        AssertSameLine(rasterizer, '┼', columns[0], columns[^1]);
        AssertSameLine(rasterizer, '┼', rows[0], rows[^1]);
    }

    [Theory]
    [MemberData(nameof(BundledFontsAtSeamSizes))]
    public void Draw_StraightLines_LandOnWholePixels(string familyName, float pixelSize)
    {
        using var fonts = new FontWithBlocks(familyName);
        using var rasterizer = fonts.CreateRasterizer(pixelSize);
        var (width, height) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);
        var middleColumn = SKRectI.Create(width / 2, 0, 1, height);
        var middleRow = SKRectI.Create(0, height / 2, width, 1);

        // Thin lines stay crisp instead of fading across two pixels, and a double line stays two lines.
        AssertCrispLines(rasterizer, '─', middleColumn, 1);
        AssertCrispLines(rasterizer, '━', middleColumn, 1);
        AssertCrispLines(rasterizer, '═', middleColumn, 2);
        AssertCrispLines(rasterizer, '│', middleRow, 1);
        AssertCrispLines(rasterizer, '┃', middleRow, 1);
        AssertCrispLines(rasterizer, '║', middleRow, 2);
    }

    [Theory]
    [MemberData(nameof(BundledFontsAtSeamSizes))]
    public void Draw_HalfBlocks_SplitTheCellOnAWholePixel(string familyName, float pixelSize)
    {
        using var fonts = new FontWithBlocks(familyName);
        using var rasterizer = fonts.CreateRasterizer(pixelSize);

        AssertSplitTheCell(rasterizer, '▀', '▄');
        AssertSplitTheCell(rasterizer, '▌', '▐');
    }

    [Fact]
    public void Draw_EighthBlocks_StayVisibleInTheSmallestCells()
    {
        using var fonts = new FontWithBlocks(BundledFonts.Default.FamilyName);
        using var rasterizer = fonts.CreateRasterizer(_seamSizes[0]);

        // An eighth of a 3 × 7 cell is under half a pixel, which would round away to nothing.
        foreach (var glyph in new[] { '▏', '▕', '▁', '▔' })
        {
            Assert.Equal(GlyphSource.PrimaryFont, rasterizer.GetSource(new Rune(glyph)));
            using var bitmap = DrawAlone(rasterizer, glyph);
            Assert.True(TestBitmaps.Alphas(bitmap, SKRectI.Create(0, 0, bitmap.Width, bitmap.Height))
                .Contains(byte.MaxValue), $"{glyph} vanished.");
        }
    }

    [Fact]
    public void Draw_WithoutHinting_LeavesBoxDrawingLinesWhereTheFontPutsThem()
    {
        using var font = TestFonts.Load("IBM Plex Mono");
        var options = GlyphRenderOptions.Default with { Hinting = GlyphHinting.None };
        using var rasterizer = new GlyphRasterizer(font, 13.4f, options, FixedFallbackFontProvider.None);
        using var bitmap = DrawAlone(rasterizer, '─');

        // IBM Plex Mono's line is 0.9 px thick at this size, so unsnapped it only partly covers its row.
        var alphas = TestBitmaps.Alphas(bitmap, SKRectI.Create(rasterizer.Metrics.Width / 2, 0, 1, bitmap.Height));
        Assert.Contains(alphas, alpha => alpha is > 0 and < byte.MaxValue);
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

    [Theory]
    [InlineData(10, 0, 10, 1)] // fills the cell exactly
    [InlineData(10, -0.4f, 10.4f, 1)] // pokes out by 4%: within the allowance
    [InlineData(10, -1, 11, 10f / 12)] // pokes out by 10%: squeezed to the cell width
    [InlineData(10, 0, 13, 10f / 13)] // pokes out on one side only
    [InlineData(10, 0, 0, 1)] // no ink
    public void HorizontalSqueezeToFit_SqueezesOnlyGlyphsThatPokeOutClearly(float advance, float inkLeft,
        float inkRight, float expected)
    {
        var inkBounds = SKRect.Create(inkLeft, -10, inkRight - inkLeft, 10);

        var squeeze = GlyphRasterizer.HorizontalSqueezeToFit(advance, inkBounds, cellWidth: 10);

        Assert.Equal(expected, squeeze, 0.0001f);
    }

    [Fact]
    public void GetHorizontalSqueeze_DoubleArrowWiderThanTheCell_IsSqueezedToFit()
    {
        // JetBrains Mono's ↔ is about 1.3 cells wide, so clipping would cut off both tips.
        using var font = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);
        using var rasterizer = CreateRasterizer(font);

        var squeeze = rasterizer.GetHorizontalSqueeze(new Rune('↔'));

        Assert.InRange(squeeze, 0.7f, 0.85f);
    }

    [Theory]
    [InlineData('M')]
    [InlineData('─')] // box drawing pokes out on purpose so lines join
    [InlineData('█')]
    [InlineData('←')]
    public void GetHorizontalSqueeze_GlyphsThatFitOrJoinTheirNeighbours_AreNotSqueezed(char character)
    {
        using var font = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);
        using var rasterizer = CreateRasterizer(font);

        Assert.Equal(1, rasterizer.GetHorizontalSqueeze(new Rune(character)));
    }

    [Fact]
    public void Draw_SqueezedGlyph_StaysInsideItsCell()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.JetBrainsMono);
        using var rasterizer = CreateRasterizer(font);
        using var bitmap = CreateCellRow(rasterizer, 3);
        using var canvas = new SKCanvas(bitmap);
        var (width, height) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);

        var source = rasterizer.Draw(canvas, width, 0, new Rune('↔'), SKColors.Black);

        Assert.Equal(GlyphSource.PrimaryFont, source);
        Assert.Equal(0, TestBitmaps.CountInk(bitmap, SKRectI.Create(0, 0, width, height)));
        Assert.Equal(0, TestBitmaps.CountInk(bitmap, SKRectI.Create(2 * width, 0, width, height)));

        // Squeezed to the cell's width and centred, both tips reach (nearly) to the cell's edges.
        // An off-centre glyph would leave a gap on one side and lose a tip on the other.
        var ink = TestBitmaps.InkBounds(bitmap, SKRectI.Create(width, 0, width, height));
        Assert.InRange(ink.Left - width, 0, 1);
        Assert.InRange(2 * width - ink.Right, 0, 1);
    }

    [Fact]
    public void Constructor_PixelSizeOutOfRange_Throws()
    {
        using var font = FontLoader.LoadBundled(BundledFonts.Default);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GlyphRasterizer(font, 0, GlyphRenderOptions.Default, FixedFallbackFontProvider.None));
    }

    private static void AssertSolid(GlyphRasterizer rasterizer, char glyph, SKRectI region)
    {
        using var bitmap = DrawAlone(rasterizer, glyph);

        var alphas = TestBitmaps.Alphas(bitmap, region);
        if (alphas.Any(alpha => alpha != byte.MaxValue))
        {
            Assert.Fail($"{glyph} at {rasterizer.Metrics.PixelSize} px should fill {region} of its " +
                $"{rasterizer.Metrics.Width} × {rasterizer.Metrics.Height} cell; " +
                $"the lowest coverage there is {alphas.Min()}.");
        }
    }

    private static void AssertSameLine(GlyphRasterizer rasterizer, char glyph, SKRectI edge, SKRectI comparedTo)
    {
        using var bitmap = DrawAlone(rasterizer, glyph);

        var alphas = TestBitmaps.Alphas(bitmap, edge);
        var expected = TestBitmaps.Alphas(bitmap, comparedTo);
        var largestDifference = alphas.Zip(expected, (alpha, other) => Math.Abs(alpha - other)).Max();
        Assert.True(alphas.Any(alpha => alpha > 0), $"{glyph} at {rasterizer.Metrics.PixelSize} px misses {edge}.");
        Assert.True(largestDifference <= UnnoticeableAlphaDifference,
            $"{glyph} at {rasterizer.Metrics.PixelSize} px should look the same in {edge} as in {comparedTo}: " +
            $"[{string.Join(", ", alphas)}] against [{string.Join(", ", expected)}].");
    }

    private static void AssertCrispLines(GlyphRasterizer rasterizer, char glyph, SKRectI across, int lineCount)
    {
        using var bitmap = DrawAlone(rasterizer, glyph);

        var alphas = TestBitmaps.Alphas(bitmap, across);
        var lines = alphas.Where((alpha, index) => alpha > 0 && (index == 0 || alphas[index - 1] == 0)).Count();
        var description =
            $"{glyph} at {rasterizer.Metrics.PixelSize} px, across {across}: [{string.Join(", ", alphas)}]";
        Assert.True(alphas.All(alpha => alpha is 0 or byte.MaxValue), $"{description} should be solid or blank.");
        Assert.True(lines == lineCount, $"{description} should cross {lineCount} line(s).");
    }

    // Each half is solid or blank in every pixel, and the two together fill the cell without overlapping.
    private static void AssertSplitTheCell(GlyphRasterizer rasterizer, char half, char otherHalf)
    {
        using var bitmap = DrawAlone(rasterizer, half);
        using var otherBitmap = DrawAlone(rasterizer, otherHalf);

        var cell = SKRectI.Create(0, 0, bitmap.Width, bitmap.Height);
        var alphas = TestBitmaps.Alphas(bitmap, cell);
        var otherAlphas = TestBitmaps.Alphas(otherBitmap, cell);
        Assert.True(alphas.All(alpha => alpha is 0 or byte.MaxValue),
            $"{half} at {rasterizer.Metrics.PixelSize} px should be solid or blank in every pixel.");
        Assert.True(alphas.Zip(otherAlphas, (alpha, other) => alpha + other).All(sum => sum == byte.MaxValue),
            $"{half} and {otherHalf} at {rasterizer.Metrics.PixelSize} px should fill the cell between them.");
    }

    private static SKBitmap DrawAlone(GlyphRasterizer rasterizer, char glyph)
    {
        var bitmap = CreateCellRow(rasterizer, 1);
        using var canvas = new SKCanvas(bitmap);
        rasterizer.Draw(canvas, 0, 0, new Rune(glyph), SKColors.Black);
        return bitmap;
    }

    private static GlyphRasterizer CreateRasterizer(LoadedFont font, IFallbackFontProvider? fallbackFonts = null) =>
        new(font, PixelSize, GlyphRenderOptions.Default, fallbackFonts ?? FixedFallbackFontProvider.None);

    private static SKBitmap CreateCellRow(GlyphRasterizer rasterizer, int cells) =>
        TestBitmaps.CreateTransparent(rasterizer.Metrics.Width * cells, rasterizer.Metrics.Height);

    // A bundled font, with JetBrains Mono as its fallback. Roboto Mono has no box-drawing or block glyphs, so they come
    // from there, as they would from a system font in the app.
    private sealed class FontWithBlocks(string familyName) : IDisposable
    {
        private readonly LoadedFont _font = TestFonts.Load(familyName);
        private readonly LoadedFont _fallback = FontLoader.LoadBundled(BundledFonts.Default);

        public GlyphRasterizer CreateRasterizer(float pixelSize) =>
            new(_font, pixelSize, GlyphRenderOptions.Default, new FixedFallbackFontProvider(_fallback));

        public void Dispose()
        {
            _font.Dispose();
            _fallback.Dispose();
        }
    }
}
