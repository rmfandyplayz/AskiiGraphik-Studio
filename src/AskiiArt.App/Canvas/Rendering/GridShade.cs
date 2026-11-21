// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// How strongly one frame's grid shows over each visible cell (ADR 0061), so it shows less under the selection's
/// hatching and doesn't crowd it. Like the cell backgrounds (ADR 0038), it's an alpha image with a pixel per visible
/// cell, drawn stretched over those cells without smoothing.
/// </summary>
internal sealed class GridShade : IDisposable
{
    public GridShade(SKImage image, PixelRect bounds)
    {
        Image = image;
        Bounds = bounds;
    }

    /// <summary>A pixel per visible cell, as opaque as the grid shows over the cell.</summary>
    public SKImage Image { get; }

    /// <summary>Where the image goes, in device pixels: over the visible cells.</summary>
    public PixelRect Bounds { get; }

    public void Dispose() => Image.Dispose();
}
