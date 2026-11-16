// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Ramps;
using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Measures how dark each glyph of one font looks (ARCHITECTURE §7, ADR 0045). It draws the glyph alone in a cell
/// with the one rasterizer, exactly as the canvas draws it: box and block characters fitted to the cell, and missing
/// glyphs from a fallback font or as the placeholder box. The density is the share of the cell the ink covers.
/// Each glyph is measured once and remembered. Not thread-safe.
/// </summary>
public sealed class GlyphDensityMeter : IGlyphDensityProvider, IDisposable
{
    /// <summary>
    /// The size glyphs are measured at, in pixels per em. It's big enough that rounding to whole pixels barely
    /// changes the result, so the densities hold at every zoom.
    /// </summary>
    public const float MeasuringPixelSize = 64;

    private readonly GlyphRasterizer _rasterizer;
    private readonly SKBitmap _cell;
    private readonly SKCanvas _canvas;
    private readonly Dictionary<Rune, double> _densities = [];
    private bool _isDisposed;

    /// <summary>
    /// Creates a meter for <paramref name="font"/>. It doesn't own <paramref name="font"/> or
    /// <paramref name="fallbackFonts"/>.
    /// </summary>
    public GlyphDensityMeter(LoadedFont font, IFallbackFontProvider fallbackFonts)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(fallbackFonts);

        _rasterizer = new GlyphRasterizer(font, MeasuringPixelSize, GlyphRenderOptions.Default, fallbackFonts);

        // Only coverage matters, so the cell keeps an alpha channel and nothing else.
        var metrics = _rasterizer.Metrics;
        _cell = new SKBitmap(new SKImageInfo(metrics.Width, metrics.Height, SKColorType.Alpha8, SKAlphaType.Premul));
        _canvas = new SKCanvas(_cell);
    }

    /// <summary>The font this meter measures.</summary>
    public LoadedFont Font => _rasterizer.Font;

    /// <inheritdoc/>
    public double GetDensity(Rune glyph)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_densities.TryGetValue(glyph, out var density))
            return density;

        density = Measure(glyph);
        _densities.Add(glyph, density);
        return density;
    }

    /// <summary>
    /// Which font draws <paramref name="glyph"/>, so a caller can tell a fallback's measurement apart.
    /// </summary>
    public GlyphSource GetSource(Rune glyph)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _rasterizer.GetSource(glyph);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _canvas.Dispose();
        _cell.Dispose();
        _rasterizer.Dispose();
    }

    private double Measure(Rune glyph)
    {
        _canvas.Clear(SKColors.Transparent);
        _rasterizer.Draw(_canvas, 0, 0, glyph, SKColors.Black);
        _canvas.Flush();

        var pixels = _cell.GetPixelSpan();
        var rowBytes = _cell.RowBytes;
        long coverage = 0;
        for (var row = 0; row < _cell.Height; row++)
        {
            foreach (var alpha in pixels.Slice(row * rowBytes, _cell.Width))
                coverage += alpha;
        }

        return coverage / ((double)_cell.Width * _cell.Height * byte.MaxValue);
    }
}
