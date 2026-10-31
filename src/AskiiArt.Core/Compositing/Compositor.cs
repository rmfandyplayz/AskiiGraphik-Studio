// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Compositing;

/// <summary>
/// Flattens a frame's visible layers into one grid of cells (ARCHITECTURE §5.6). This is the only
/// compositing implementation: the renderer, exporters, clipboard and magic wand all use it.
/// </summary>
/// <remarks>
/// At each cell, the topmost visible layer whose cell hides the layers below (by the
/// <see cref="CompositingPolicy"/>) wins. If no layer does, the result is <see cref="Cell.Empty"/>.
/// Layer opacity and blend modes are reserved and ignored (§18).
/// </remarks>
public static class Compositor
{
    /// <summary>Returns the composited cell at <paramref name="point"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the canvas.</exception>
    public static Cell CompositeCell(Frame frame, CellPoint point, CompositingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(policy);

        var layers = frame.Layers;
        for (var index = layers.Count - 1; index >= 0; index--)
        {
            var layer = layers[index];
            if (!layer.Properties.IsVisible)
                continue;

            var cell = layer.GetCell(point);
            if (policy.HidesLayersBelow(cell))
                return cell;
        }

        return Cell.Empty;
    }

    /// <summary>
    /// Composites every cell of <paramref name="region"/> into <paramref name="destination"/>, row
    /// by row, in the same layout as <see cref="Layer.CopyCells"/>. Doesn't allocate.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The region isn't inside the canvas.</exception>
    /// <exception cref="ArgumentException">The destination is shorter than the region's area.</exception>
    public static void CompositeRegion(Frame frame, CellRect region, CompositingPolicy policy, Span<Cell> destination)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(policy);

        var canvasSize = frame.Document.CanvasSize;
        if (!canvasSize.Bounds.Contains(region))
            throw new ArgumentOutOfRangeException(nameof(region), region, $"The region isn't inside the {canvasSize} canvas.");

        if (destination.Length < region.Area)
            throw new ArgumentException("The destination is shorter than the region's area.", nameof(destination));

        var result = destination[..region.Area];
        result.Clear();

        // Painting from the bottom up, each hiding cell overwrites what's below it, so the topmost
        // one wins. That gives the same result as CompositeCell while reading whole rows at once.
        foreach (var layer in frame.Layers)
        {
            if (!layer.Properties.IsVisible)
                continue;

            for (var row = 0; row < region.Height; row++)
            {
                var source = layer.GetRow(region.Y + row).Slice(region.X, region.Width);
                var target = result.Slice(row * region.Width, region.Width);
                for (var column = 0; column < source.Length; column++)
                {
                    if (policy.HidesLayersBelow(source[column]))
                        target[column] = source[column];
                }
            }
        }
    }
}
