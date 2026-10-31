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

    private readonly IFallbackFontProvider _fallbackFonts;
    private readonly SKFont _primaryFont;
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
                DrawOnBaseline(canvas, _primaryFont, rune.ToString(), cell);
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
