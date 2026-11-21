// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// One frame's hatching over some of the visible cells (ADR 0061): a small tile of diagonal lines, repeated from the
/// canvas's corner, moved along by how far the lines have gone, so they stay put on the art while it scrolls, and
/// shown only over the cells the cell mask has.
/// Like the cell backgrounds (ADR 0038), the mask has a pixel per visible cell, drawn stretched over those cells
/// without smoothing, so one draw covers every hatched cell.
/// </summary>
internal sealed class SelectionHatch : IDisposable
{
    public SelectionHatch(SKImage cellMask, SKImage tile, PixelRect bounds, int shift)
    {
        CellMask = cellMask;
        Tile = tile;
        Bounds = bounds;
        Shift = shift;
    }

    /// <summary>A pixel per visible cell, opaque where the cell is hatched.</summary>
    public SKImage CellMask { get; }

    /// <summary>The lines, repeated across the hatched cells.</summary>
    public SKImage Tile { get; }

    /// <summary>Where the cell mask goes, in device pixels: over the visible cells.</summary>
    public PixelRect Bounds { get; }

    /// <summary>How far the lines have moved along a row, to the left, in device pixels.</summary>
    public int Shift { get; }

    public void Dispose()
    {
        CellMask.Dispose();
        Tile.Dispose();
    }
}
