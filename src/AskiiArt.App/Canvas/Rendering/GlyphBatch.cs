// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Buffers;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// The glyphs of one frame that come from one atlas page: where each one is on the page and where
/// it goes on screen, ready for a single <see cref="SKCanvas.DrawAtlas(SKImage, SKRect[], SKRotationScaleMatrix[], SKSamplingOptions, SKPaint)"/>
/// call.
/// </summary>
/// <remarks>
/// SkiaSharp's DrawAtlas takes whole arrays rather than a count, and the arrays come from a pool
/// (a zoomed-out frame can hold hundreds of thousands of glyphs), so they're usually longer than
/// <see cref="Count"/>. The unused tail holds empty rectangles, which draw nothing.
/// </remarks>
internal sealed class GlyphBatch : IDisposable
{
    private const int InitialCapacity = 256;

    private bool _isDisposed;

    public GlyphBatch(int pageIndex)
    {
        PageIndex = pageIndex;
    }

    public int PageIndex { get; }

    public int Count { get; private set; }

    /// <summary>The page image, once <see cref="Complete"/> has been called.</summary>
    public SharedImage? Page { get; private set; }

    /// <summary>Where each glyph is on the page; see the remarks for the tail.</summary>
    public SKRect[] Sprites { get; private set; } = ArrayPool<SKRect>.Shared.Rent(InitialCapacity);

    /// <summary>Where each glyph goes: a translation to its cell's top-left corner.</summary>
    public SKRotationScaleMatrix[] Transforms { get; private set; } =
        ArrayPool<SKRotationScaleMatrix>.Shared.Rent(InitialCapacity);

    public void Add(SKRectI source, int x, int y)
    {
        if (Count == Sprites.Length)
            Grow();

        Sprites[Count] = source;
        Transforms[Count] = SKRotationScaleMatrix.CreateTranslation(x, y);
        Count++;
    }

    /// <summary>Clears the unused tail and takes a reference to the page image to draw from.</summary>
    public void Complete(SharedImage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        Array.Clear(Sprites, Count, Sprites.Length - Count);
        Array.Clear(Transforms, Count, Transforms.Length - Count);
        Page = page.AddReference();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Page?.Release();
        ArrayPool<SKRect>.Shared.Return(Sprites);
        ArrayPool<SKRotationScaleMatrix>.Shared.Return(Transforms);
        Sprites = [];
        Transforms = [];
    }

    private void Grow()
    {
        var sprites = ArrayPool<SKRect>.Shared.Rent(Sprites.Length * 2);
        var transforms = ArrayPool<SKRotationScaleMatrix>.Shared.Rent(Transforms.Length * 2);
        Sprites.AsSpan(0, Count).CopyTo(sprites);
        Transforms.AsSpan(0, Count).CopyTo(transforms);
        ArrayPool<SKRect>.Shared.Return(Sprites);
        ArrayPool<SKRotationScaleMatrix>.Shared.Return(Transforms);
        Sprites = sprites;
        Transforms = transforms;
    }
}
