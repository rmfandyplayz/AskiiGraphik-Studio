// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Keeps a <see cref="GlyphDensityMeter"/> for each of the most recently used fonts, so each glyph of a font is
/// measured once. When the cache is full, the least recently used meter is disposed. Get the meter again each time you
/// need it rather than keeping it, because a later call may evict it. Not thread-safe.
/// </summary>
public sealed class GlyphDensityCache : IDisposable
{
    /// <summary>The default number of fonts kept.</summary>
    public const int DefaultCapacity = 4;

    private readonly IFallbackFontProvider _fallbackFonts;

    // Most recently used first; the capacity is small, so a list is simpler than a linked map.
    private readonly List<GlyphDensityMeter> _meters = [];
    private bool _isDisposed;

    /// <summary>
    /// Creates an empty cache that keeps up to <paramref name="capacity"/> fonts' meters. It doesn't own
    /// <paramref name="fallbackFonts"/>.
    /// </summary>
    public GlyphDensityCache(IFallbackFontProvider fallbackFonts, int capacity = DefaultCapacity)
    {
        ArgumentNullException.ThrowIfNull(fallbackFonts);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _fallbackFonts = fallbackFonts;
        Capacity = capacity;
    }

    /// <summary>The most fonts kept at once.</summary>
    public int Capacity { get; }

    /// <summary>The number of fonts currently kept.</summary>
    public int Count => _meters.Count;

    /// <summary>
    /// Returns the meter for <paramref name="font"/>, creating it if it isn't cached. Fonts are compared by
    /// reference: two separately loaded copies are different fonts.
    /// </summary>
    public GlyphDensityMeter GetMeter(LoadedFont font)
    {
        ArgumentNullException.ThrowIfNull(font);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var index = _meters.FindIndex(meter => ReferenceEquals(meter.Font, font));
        if (index >= 0)
        {
            var cached = _meters[index];
            _meters.RemoveAt(index);
            _meters.Insert(0, cached);
            return cached;
        }

        var created = new GlyphDensityMeter(font, _fallbackFonts);
        _meters.Insert(0, created);
        if (_meters.Count <= Capacity)
            return created;

        _meters[^1].Dispose();
        _meters.RemoveAt(_meters.Count - 1);
        return created;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var meter in _meters)
            meter.Dispose();

        _meters.Clear();
    }
}
