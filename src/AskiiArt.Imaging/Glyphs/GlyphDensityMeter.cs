// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Ramps;
using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Measures how dark each glyph of one font looks, and where its ink lies (ARCHITECTURE §7, ADRs 0045 and 0068). It
/// draws the glyph alone in a cell with the one rasterizer, exactly as the canvas draws it: box and block characters
/// fitted to the cell, and missing glyphs from a fallback font or as the placeholder box. The density is the share of
/// the cell the ink covers, and the shape the share of each part of the cell. Each glyph is measured once and
/// remembered. Not thread-safe.
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
    private readonly Dictionary<Rune, (double Density, GlyphShape Shape)> _measured = [];

    // How each pixel column and row splits between the parts of the cell it overlaps, so every part covers exactly
    // its share of the cell, though the cell's pixels rarely divide evenly into parts.
    private readonly PixelSplit[] _columnSplits;
    private readonly PixelSplit[] _rowSplits;
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
        _columnSplits = SplitsAlong(metrics.Width, GlyphShape.Columns);
        _rowSplits = SplitsAlong(metrics.Height, GlyphShape.Rows);
    }

    /// <summary>The font this meter measures.</summary>
    public LoadedFont Font => _rasterizer.Font;

    /// <inheritdoc/>
    public double GetDensity(Rune glyph) => MeasuredOf(glyph).Density;

    /// <inheritdoc/>
    public GlyphShape GetShape(Rune glyph) => MeasuredOf(glyph).Shape;

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

    private (double Density, GlyphShape Shape) MeasuredOf(Rune glyph)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_measured.TryGetValue(glyph, out var measured))
            return measured;

        measured = Measure(glyph);
        _measured.Add(glyph, measured);
        return measured;
    }

    private (double Density, GlyphShape Shape) Measure(Rune glyph)
    {
        _canvas.Clear(SKColors.Transparent);
        _rasterizer.Draw(_canvas, 0, 0, glyph, SKColors.Black);
        _canvas.Flush();

        var pixels = _cell.GetPixelSpan();
        var rowBytes = _cell.RowBytes;
        long coverage = 0;
        Span<double> partCoverage = stackalloc double[GlyphShape.PartCount];
        Span<double> lineCoverage = stackalloc double[GlyphShape.Columns];
        for (var row = 0; row < _cell.Height; row++)
        {
            lineCoverage.Clear();
            var line = pixels.Slice(row * rowBytes, _cell.Width);
            for (var column = 0; column < line.Length; column++)
            {
                coverage += line[column];
                _columnSplits[column].AddTo(lineCoverage, line[column], 0, 1);
            }

            for (var partColumn = 0; partColumn < GlyphShape.Columns; partColumn++)
            {
                _rowSplits[row].AddTo(partCoverage, lineCoverage[partColumn], partColumn,
                    GlyphShape.Columns);
            }
        }

        var partArea = (double)_cell.Width * _cell.Height / GlyphShape.PartCount * byte.MaxValue;
        Span<double> shares = stackalloc double[GlyphShape.PartCount];
        for (var part = 0; part < shares.Length; part++)
            shares[part] = Math.Min(partCoverage[part] / partArea, 1);

        var density = coverage / ((double)_cell.Width * _cell.Height * byte.MaxValue);
        return (density, new GlyphShape(shares));
    }

    // How each of length pixels splits between count equal parts. A part is more than a pixel long at the size glyphs
    // are measured at, so a pixel overlaps two parts at most.
    private static PixelSplit[] SplitsAlong(int length, int count)
    {
        var splits = new PixelSplit[length];
        var partLength = (double)length / count;
        for (var pixel = 0; pixel < length; pixel++)
        {
            var part = Math.Min((int)(pixel / partLength), count - 1);
            var partEnd = (part + 1) * partLength;
            var share = part == count - 1 ? 1 : Math.Min(partEnd - pixel, 1);
            splits[pixel] = new PixelSplit(part, share);
        }

        return splits;
    }

    // A pixel's share of the part it starts in; the rest of it is in the next part.
    private readonly record struct PixelSplit(int Part, double Share)
    {
        // Adds value to the parts the pixel overlaps, by its share of each. The parts along this pixel's line are
        // stride apart in totals, starting at offset.
        public void AddTo(Span<double> totals, double value, int offset, int stride)
        {
            totals[offset + Part * stride] += value * Share;
            if (Share < 1)
                totals[offset + (Part + 1) * stride] += value * (1 - Share);
        }
    }
}
