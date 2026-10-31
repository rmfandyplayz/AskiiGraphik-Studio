// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Keeps the most recently used glyph atlases, one per (font, pixel size, options), so zooming
/// back to a recent level doesn't rasterize everything again (ADR 0014). When the cache is
/// full, the least recently used atlas is disposed. Get the atlas again each time you draw
/// rather than keeping it, because a later call may evict it. Not thread-safe.
/// </summary>
public sealed class GlyphAtlasCache : IDisposable
{
    /// <summary>The default number of atlases kept.</summary>
    public const int DefaultCapacity = 8;

    private readonly IFallbackFontProvider _fallbackFonts;

    // Most recently used first; the capacity is small, so a list is simpler than a linked map.
    private readonly List<CachedAtlas> _atlases = [];
    private bool _isDisposed;

    /// <summary>
    /// Creates an empty cache that keeps up to <paramref name="capacity"/> atlases. It doesn't own
    /// <paramref name="fallbackFonts"/>.
    /// </summary>
    public GlyphAtlasCache(IFallbackFontProvider fallbackFonts, int capacity = DefaultCapacity)
    {
        ArgumentNullException.ThrowIfNull(fallbackFonts);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _fallbackFonts = fallbackFonts;
        Capacity = capacity;
    }

    /// <summary>The most atlases kept at once.</summary>
    public int Capacity { get; }

    /// <summary>The number of atlases currently kept.</summary>
    public int Count => _atlases.Count;

    /// <summary>Returns the atlas for this combination, creating it if it isn't cached.</summary>
    public GlyphAtlas GetAtlas(LoadedFont font, float pixelSize, GlyphRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(options);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var key = new AtlasKey(font, pixelSize, options);
        var index = _atlases.FindIndex(cached => cached.Key == key);
        if (index >= 0)
        {
            var cached = _atlases[index];
            _atlases.RemoveAt(index);
            _atlases.Insert(0, cached);
            return cached.Atlas;
        }

        var atlas = new GlyphAtlas(font, pixelSize, options, _fallbackFonts);
        _atlases.Insert(0, new CachedAtlas(key, atlas));
        if (_atlases.Count <= Capacity)
            return atlas;

        _atlases[^1].Atlas.Dispose();
        _atlases.RemoveAt(_atlases.Count - 1);
        return atlas;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var cached in _atlases)
            cached.Atlas.Dispose();

        _atlases.Clear();
    }

    // The font is compared by reference: two separately loaded copies are different fonts.
    private readonly record struct AtlasKey(LoadedFont Font, float PixelSize, GlyphRenderOptions Options);

    private sealed record CachedAtlas(AtlasKey Key, GlyphAtlas Atlas);
}
