// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.InteropServices;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Collects a frame's <see cref="CellBackgrounds"/> on the UI thread. Its buffer is kept from frame
/// to frame, and is cleared only in frames that have a background, so a monochrome document costs
/// nothing.
/// </summary>
internal sealed class CellBackgroundsBuilder
{
    private SKColor[] _pixels = [];
    private int _width;
    private int _height;
    private bool _hasAny;

    /// <summary>
    /// Starts a frame <paramref name="width"/> × <paramref name="height"/> cells, none with a background.
    /// </summary>
    public void Start(int width, int height)
    {
        var count = width * height;
        if (_pixels.Length < count)
            _pixels = new SKColor[count];

        _width = width;
        _height = height;
        _hasAny = false;
    }

    /// <summary>Sets the background of a cell, counted from the frame's top-left cell.</summary>
    public void Set(int column, int row, SKColor color)
    {
        if (!_hasAny)
        {
            Array.Clear(_pixels, 0, _width * _height);
            _hasAny = true;
        }

        _pixels[row * _width + column] = color;
    }

    /// <summary>The frame's backgrounds, to draw over <paramref name="bounds"/>, or null when it has none.</summary>
    public CellBackgrounds? Complete(PixelRect bounds)
    {
        if (!_hasAny)
            return null;

        // An SKColor is a uint holding 0xAARRGGBB, so on the little-endian machines .NET runs on its
        // bytes are blue, green, red, alpha. The colours are opaque or fully transparent, which are
        // the same premultiplied.
        var info = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var pixels = MemoryMarshal.AsBytes(_pixels.AsSpan(0, _width * _height));
        var image = SKImage.FromPixelCopy(info, pixels);
        return image is null ? null : new CellBackgrounds(image, bounds);
    }
}
