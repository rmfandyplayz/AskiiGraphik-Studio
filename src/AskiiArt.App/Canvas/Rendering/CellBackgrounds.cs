// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// One frame's cell backgrounds: an image with a pixel for each visible cell, drawn stretched over
/// those cells without smoothing, so each pixel fills its cell exactly. One image draws much faster
/// than a rectangle per cell, whatever the number of backgrounds (ADR 0038).
/// </summary>
internal sealed class CellBackgrounds : IDisposable
{
    public CellBackgrounds(SKImage image, PixelRect bounds)
    {
        Image = image;
        Bounds = bounds;
    }

    /// <summary>A pixel per visible cell, transparent where a cell has no background.</summary>
    public SKImage Image { get; }

    /// <summary>Where the image goes, in device pixels: over the visible cells.</summary>
    public PixelRect Bounds { get; }

    public void Dispose() => Image.Dispose();
}
