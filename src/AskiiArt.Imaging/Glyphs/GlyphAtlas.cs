// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// A cache of rasterized glyphs for one (font, pixel size, options) combination (ARCHITECTURE §11,
/// ADR 0014). Each glyph is rasterized once, on first use, into a cell-sized slot on an
/// alpha-only page bitmap: the pixel values are ink coverage, so the canvas can draw them in any
/// colour. Glyph bitmaps are never scaled; another zoom level gets another atlas.
/// Not thread-safe.
/// </summary>
public sealed class GlyphAtlas : IDisposable
{
    /// <summary>The largest side of a page bitmap, in pixels, unless one cell is bigger.</summary>
    public const int DefaultPageSizeInPixels = 1024;

    private readonly GlyphRasterizer _rasterizer;
    private readonly Dictionary<Rune, GlyphAtlasEntry> _entries = [];
    private readonly List<AtlasPage> _pages = [];
    private readonly int _columnsPerPage;
    private readonly int _rowsPerPage;
    private bool _isDisposed;

    /// <summary>
    /// Creates an empty atlas. It doesn't own <paramref name="font"/> or
    /// <paramref name="fallbackFonts"/>.
    /// </summary>
    public GlyphAtlas(LoadedFont font, float pixelSize, GlyphRenderOptions options,
        IFallbackFontProvider fallbackFonts)
        : this(font, pixelSize, options, fallbackFonts, DefaultPageSizeInPixels)
    {
    }

    internal GlyphAtlas(LoadedFont font, float pixelSize, GlyphRenderOptions options,
        IFallbackFontProvider fallbackFonts, int pageSizeInPixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSizeInPixels, 1);

        _rasterizer = new GlyphRasterizer(font, pixelSize, options, fallbackFonts);
        _columnsPerPage = Math.Max(1, pageSizeInPixels / Metrics.Width);
        _rowsPerPage = Math.Max(1, pageSizeInPixels / Metrics.Height);
    }

    /// <summary>The document's font.</summary>
    public LoadedFont Font => _rasterizer.Font;

    /// <summary>The rasterization settings.</summary>
    public GlyphRenderOptions Options => _rasterizer.Options;

    /// <summary>The size of every glyph slot.</summary>
    public CellMetrics Metrics => _rasterizer.Metrics;

    /// <summary>The number of glyphs rasterized so far.</summary>
    public int GlyphCount => _entries.Count;

    /// <summary>The number of page bitmaps.</summary>
    public int PageCount => _pages.Count;

    private int SlotsPerPage => _columnsPerPage * _rowsPerPage;

    /// <summary>
    /// Returns where <paramref name="rune"/> sits in the atlas, rasterizing it first if needed.
    /// </summary>
    public GlyphAtlasEntry GetOrAdd(Rune rune)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_entries.TryGetValue(rune, out var entry))
            return entry;

        var slot = _entries.Count;
        var pageIndex = slot / SlotsPerPage;
        if (pageIndex == _pages.Count)
            _pages.Add(new AtlasPage(_columnsPerPage * Metrics.Width, _rowsPerPage * Metrics.Height));

        var slotInPage = slot % SlotsPerPage;
        var x = slotInPage % _columnsPerPage * Metrics.Width;
        var y = slotInPage / _columnsPerPage * Metrics.Height;

        // Full-alpha ink, so each pixel's alpha is exactly the glyph's coverage.
        var source = _rasterizer.Draw(_pages[pageIndex].Canvas, x, y, rune, SKColors.Black);
        entry = new GlyphAtlasEntry(pageIndex, SKRectI.Create(x, y, Metrics.Width, Metrics.Height), source);
        _entries.Add(rune, entry);
        return entry;
    }

    /// <summary>
    /// A page of glyphs as an alpha-only bitmap (<see cref="SKColorType.Alpha8"/>). The atlas
    /// owns it; draw from it, but don't change or dispose it.
    /// </summary>
    public SKBitmap GetPage(int pageIndex)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _pages[pageIndex].Bitmap;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var page in _pages)
            page.Dispose();

        _pages.Clear();
        _entries.Clear();
        _rasterizer.Dispose();
    }

    private sealed class AtlasPage : IDisposable
    {
        public AtlasPage(int width, int height)
        {
            Bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Alpha8, SKAlphaType.Premul));
            Bitmap.Erase(SKColors.Transparent);
            Canvas = new SKCanvas(Bitmap);
        }

        public SKBitmap Bitmap { get; }

        public SKCanvas Canvas { get; }

        public void Dispose()
        {
            Canvas.Dispose();
            Bitmap.Dispose();
        }
    }
}
