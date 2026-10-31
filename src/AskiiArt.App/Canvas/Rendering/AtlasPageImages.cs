// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Immutable copies of a glyph atlas's pages, for the render thread. The atlas keeps drawing new
/// glyphs into its pages on the UI thread, so the render thread never reads a page directly; it
/// draws a copy, which is refreshed only when the page has changed. Belongs to the UI thread.
/// </summary>
internal sealed class AtlasPageImages
{
    private readonly List<PageCopy?> _copies = [];
    private GlyphAtlas? _atlas;

    /// <summary>
    /// An up-to-date copy of a page. The copy stays valid until the next call for the same page;
    /// to keep it longer (for a frame being drawn), take a reference.
    /// </summary>
    public SharedImage Get(GlyphAtlas atlas, int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(atlas);

        if (!ReferenceEquals(atlas, _atlas))
        {
            ReleaseAll();
            _atlas = atlas;
        }

        while (_copies.Count <= pageIndex)
            _copies.Add(null);

        var version = atlas.GetPageVersion(pageIndex);
        if (_copies[pageIndex] is { } copy && copy.Version == version)
            return copy.Image;

        _copies[pageIndex]?.Image.Release();

        // FromBitmap copies the pixels, because the page bitmap is still mutable.
        var image = new SharedImage(SKImage.FromBitmap(atlas.GetPage(pageIndex)));
        _copies[pageIndex] = new PageCopy(image, version);
        return image;
    }

    /// <summary>Releases every copy; the next <see cref="Get"/> makes fresh ones.</summary>
    public void Clear()
    {
        ReleaseAll();
        _atlas = null;
    }

    private void ReleaseAll()
    {
        foreach (var copy in _copies)
            copy?.Image.Release();

        _copies.Clear();
    }

    private sealed record PageCopy(SharedImage Image, int Version);
}
