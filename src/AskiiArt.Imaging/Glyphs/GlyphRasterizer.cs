// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// The one glyph rasterizer (ARCHITECTURE Principle 4, ADR 0014): the glyph atlas, PNG export and
/// glyph-density measurement all draw cells through it, so what you see is what you export.
/// Each glyph is drawn alone (never shaped as text, so no ligatures) and clipped to its cell.
/// Not thread-safe; give each thread its own rasterizer.
/// </summary>
public sealed class GlyphRasterizer : IDisposable
{
    // The placeholder box spans the inner 70% of the cell width, and from the baseline up by
    // roughly a capital letter's height.
    private const float PlaceholderSideInsetPerCellWidth = 0.15f;
    private const float PlaceholderHeightPerEm = 0.7f;
    private const float PlaceholderStrokePerEm = 1f / 14;

    // A document-font glyph may reach this far past either side of its cell (as a share of the
    // cell width) before it's squeezed. Most fonts let a few glyphs poke out slightly; only the
    // ones that would visibly lose a part (like the tips of ↔) are worth distorting.
    private const float AllowedOverhangPerCellWidth = 0.05f;

    private readonly IFallbackFontProvider _fallbackFonts;
    private readonly SKFont _primaryFont;
    private readonly SKFont _measuringFont;
    private readonly float _referenceAdvance;
    private readonly Dictionary<LoadedFont, SKFont> _fallbackSkFonts = [];
    private readonly SKPaint _paint;
    private bool _isDisposed;

    /// <summary>
    /// Creates a rasterizer for <paramref name="font"/> at <paramref name="pixelSize"/> device pixels.
    /// The rasterizer doesn't own <paramref name="font"/> or <paramref name="fallbackFonts"/>.
    /// </summary>
    public GlyphRasterizer(LoadedFont font, float pixelSize, GlyphRenderOptions options,
        IFallbackFontProvider fallbackFonts)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fallbackFonts);

        Metrics = CellMetrics.Measure(font, pixelSize);
        Font = font;
        Options = options;
        _fallbackFonts = fallbackFonts;
        _primaryFont = CreateRenderingFont(font.Typeface, pixelSize);
        _measuringFont = CellMetrics.CreateMeasuringFont(font, pixelSize);
        _referenceAdvance = CellMetrics.MeasureReferenceAdvance(font, _measuringFont);
        _paint = new SKPaint { IsAntialias = options.Antialias, Style = SKPaintStyle.Fill };
    }

    /// <summary>The document's font.</summary>
    public LoadedFont Font { get; }

    /// <summary>The rasterization settings.</summary>
    public GlyphRenderOptions Options { get; }

    /// <summary>The cell size every glyph is drawn into.</summary>
    public CellMetrics Metrics { get; }

    /// <summary>Which font would draw <paramref name="rune"/>, without drawing it.</summary>
    public GlyphSource GetSource(Rune rune) => ResolveFont(rune, out _);

    /// <summary>
    /// Draws <paramref name="rune"/> in <paramref name="ink"/> into the cell whose top-left
    /// corner is at (<paramref name="x"/>, <paramref name="y"/>) on <paramref name="canvas"/>.
    /// Nothing outside the cell is touched. Never throws for a missing glyph: a fallback font or
    /// a placeholder box is drawn instead, and the result says which.
    /// </summary>
    public GlyphSource Draw(SKCanvas canvas, int x, int y, Rune rune, SKColor ink)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var cell = SKRectI.Create(x, y, Metrics.Width, Metrics.Height);
        var source = ResolveFont(rune, out var fallbackFont);
        _paint.Color = ink;

        canvas.Save();
        canvas.ClipRect(cell);
        switch (source)
        {
            case GlyphSource.PrimaryFont:
                DrawPrimary(canvas, rune, cell);
                break;
            case GlyphSource.FallbackFont:
                DrawFallback(canvas, GetFallbackSkFont(fallbackFont!), rune.ToString(), cell);
                break;
            case GlyphSource.Placeholder:
                DrawPlaceholder(canvas, cell);
                break;
        }

        canvas.Restore();
        return source;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _primaryFont.Dispose();
        _measuringFont.Dispose();
        foreach (var skFont in _fallbackSkFonts.Values)
            skFont.Dispose();

        _fallbackSkFonts.Clear();
        _paint.Dispose();
    }

    /// <summary>
    /// How much a glyph must shrink so its advance and ink fit in the cell; 1 when it already fits.
    /// </summary>
    internal static float ScaleToFit(float advance, SKRect inkBounds, SKSizeI cellSize)
    {
        var width = MathF.Max(advance, inkBounds.Width);
        var scale = width > 0 ? MathF.Min(1, cellSize.Width / width) : 1;
        if (inkBounds.Height > 0)
            scale = MathF.Min(scale, cellSize.Height / inkBounds.Height);

        return scale;
    }

    /// <summary>
    /// How much to squeeze a glyph sideways so its ink fits the cell; 1 when it already fits, or
    /// pokes out by no more than <see cref="AllowedOverhangPerCellWidth"/> on either side. The
    /// glyph is assumed centred in the cell by its advance, as <see cref="Draw"/> places it.
    /// </summary>
    internal static float HorizontalSqueezeToFit(float advance, SKRect inkBounds, float cellWidth)
    {
        if (inkBounds.Width <= 0 || cellWidth <= 0)
            return 1;

        var originX = (cellWidth - advance) / 2;
        var overhang = MathF.Max(-(originX + inkBounds.Left), originX + inkBounds.Right - cellWidth);
        if (overhang <= AllowedOverhangPerCellWidth * cellWidth)
            return 1;

        return MathF.Min(1, cellWidth / inkBounds.Width);
    }

    /// <summary>
    /// The sideways squeeze <see cref="Draw"/> applies to <paramref name="rune"/> from the
    /// document font; 1 for no squeeze. It's measured unhinted and relative to the cell, so a
    /// glyph is squeezed the same way at every size.
    /// </summary>
    internal float GetHorizontalSqueeze(Rune rune)
    {
        // These are drawn to reach the cell's edges, and slightly past them, on purpose: that's
        // how neighbouring lines and blocks join without gaps. Clipping is right for them.
        if (IsDrawnEdgeToEdge(rune))
            return 1;

        var advance = _measuringFont.MeasureText(rune.ToString(), out var inkBounds);
        return HorizontalSqueezeToFit(advance, inkBounds, _referenceAdvance);
    }

    // Box drawing, block elements, and the sextants and wedges of Symbols for Legacy Computing.
    private static bool IsDrawnEdgeToEdge(Rune rune) =>
        rune.Value is >= 0x2500 and <= 0x259F or >= 0x1FB00 and <= 0x1FBFF;

    private static SKFontHinting ToSkiaHinting(GlyphHinting hinting) => hinting switch
    {
        GlyphHinting.None => SKFontHinting.None,
        GlyphHinting.Slight => SKFontHinting.Slight,
        GlyphHinting.Normal => SKFontHinting.Normal,
        GlyphHinting.Full => SKFontHinting.Full,
        _ => throw new ArgumentOutOfRangeException(nameof(hinting), hinting, null)
    };

    private GlyphSource ResolveFont(Rune rune, out LoadedFont? fallbackFont)
    {
        fallbackFont = null;
        if (Font.HasGlyph(rune))
            return GlyphSource.PrimaryFont;

        fallbackFont = _fallbackFonts.FindFontFor(rune, Font);
        if (fallbackFont is not null && fallbackFont.HasGlyph(rune))
            return GlyphSource.FallbackFont;

        fallbackFont = null;
        return GlyphSource.Placeholder;
    }

    private SKFont CreateRenderingFont(SKTypeface typeface, float pixelSize) =>
        new(typeface, pixelSize)
        {
            // Grey-scale smoothing, never colour (LCD) smoothing: atlas glyphs are drawn later
            // in any ink colour on any background, and LCD fringes would show (ADR 0014).
            Edging = Options.Antialias ? SKFontEdging.Antialias : SKFontEdging.Alias,
            Hinting = ToSkiaHinting(Options.Hinting),
            Subpixel = false,
            BaselineSnap = true
        };

    private SKFont GetFallbackSkFont(LoadedFont fallbackFont)
    {
        if (_fallbackSkFonts.TryGetValue(fallbackFont, out var skFont))
            return skFont;

        skFont = CreateRenderingFont(fallbackFont.Typeface, Metrics.PixelSize);
        _fallbackSkFonts.Add(fallbackFont, skFont);
        return skFont;
    }

    // A document-font glyph much wider than its cell (like ↔ in JetBrains Mono, whose tips reach
    // well into the neighbouring cells) is squeezed sideways from its outline, so it keeps its
    // height and stays whole instead of losing its ends to the clip.
    private void DrawPrimary(SKCanvas canvas, Rune rune, SKRectI cell)
    {
        var text = rune.ToString();
        var squeeze = GetHorizontalSqueeze(rune);
        if (squeeze >= 1)
        {
            DrawOnBaseline(canvas, _primaryFont, text, cell);
            return;
        }

        using var squeezedFont = CreateRenderingFont(Font.Typeface, Metrics.PixelSize);
        squeezedFont.ScaleX = squeeze;
        squeezedFont.MeasureText(text, out var inkBounds);
        var originX = cell.Left + MathF.Round((cell.Width - inkBounds.Width) / 2 - inkBounds.Left);
        canvas.DrawText(text, originX, cell.Top + Metrics.Baseline, squeezedFont, _paint);
    }

    private void DrawOnBaseline(SKCanvas canvas, SKFont skFont, string text, SKRectI cell)
    {
        var advance = skFont.MeasureText(text);
        var originX = cell.Left + MathF.Round((cell.Width - advance) / 2);
        canvas.DrawText(text, originX, cell.Top + Metrics.Baseline, skFont, _paint);
    }

    // A fallback glyph that's too big for the cell (a CJK character, say) is drawn smaller and
    // centred, so it stays recognisable instead of being cut in half.
    private void DrawFallback(SKCanvas canvas, SKFont skFont, string text, SKRectI cell)
    {
        var advance = skFont.MeasureText(text, out var inkBounds);
        var scale = ScaleToFit(advance, inkBounds, cell.Size);
        if (scale >= 1)
        {
            DrawOnBaseline(canvas, skFont, text, cell);
            return;
        }

        using var shrunkFont = CreateRenderingFont(skFont.Typeface, skFont.Size * scale);
        shrunkFont.MeasureText(text, out var shrunkBounds);
        var originX = cell.Left + MathF.Round((cell.Width - shrunkBounds.Width) / 2 - shrunkBounds.Left);
        var originY = cell.Top + MathF.Round((cell.Height - shrunkBounds.Height) / 2 - shrunkBounds.Top);
        canvas.DrawText(text, originX, originY, shrunkFont, _paint);
    }

    private void DrawPlaceholder(SKCanvas canvas, SKRectI cell)
    {
        var stroke = Math.Max(1, (int)MathF.Round(Metrics.PixelSize * PlaceholderStrokePerEm));
        var inset = (int)MathF.Round(cell.Width * PlaceholderSideInsetPerCellWidth);
        var height = Math.Min(Metrics.Baseline, (int)MathF.Round(Metrics.PixelSize * PlaceholderHeightPerEm));
        var box = SKRectI.Create(cell.Left + inset, cell.Top + Metrics.Baseline - height, cell.Width - 2 * inset,
            height);

        // Too small for a hollow box: fill what there is so the cell is still visibly marked.
        if (box.Width <= 2 * stroke || box.Height <= 2 * stroke)
        {
            canvas.DrawRect(box is { Width: > 0, Height: > 0 } ? box : cell, _paint);
            return;
        }

        // Four filled edges rather than a stroked rectangle, so the lines land on whole pixels.
        canvas.DrawRect(SKRectI.Create(box.Left, box.Top, box.Width, stroke), _paint);
        canvas.DrawRect(SKRectI.Create(box.Left, box.Bottom - stroke, box.Width, stroke), _paint);
        canvas.DrawRect(SKRectI.Create(box.Left, box.Top, stroke, box.Height), _paint);
        canvas.DrawRect(SKRectI.Create(box.Right - stroke, box.Top, stroke, box.Height), _paint);
    }
}
