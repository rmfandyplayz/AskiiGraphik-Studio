// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Compositing;

/// <summary>
/// Flattens a frame's visible layers into one grid of cells (ARCHITECTURE §5.6). This is the only
/// compositing implementation: the renderer, exporters, clipboard and magic wand all use it.
/// </summary>
/// <remarks>
/// Glyphs and backgrounds are composited separately (ADR 0036). At each cell, the glyph and its
/// text colour come from the topmost visible layer whose cell hides the glyphs below (by that
/// layer's <see cref="CompositingPolicy"/>), and the background from the topmost visible layer
/// whose cell has one. Either can be missing, so the result can hold a glyph, a background, both,
/// or neither. Layer opacity and blend modes are reserved and ignored (§18).
/// </remarks>
public static class Compositor
{
    /// <summary>Returns the composited cell at <paramref name="point"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the canvas.</exception>
    public static Cell CompositeCell(Frame frame, CellPoint point)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var layers = frame.Layers;
        var glyphs = Cell.Empty;
        var hasFoundGlyph = false;
        var background = CellColor.Unset;
        for (var index = layers.Count - 1; index >= 0 && (!hasFoundGlyph || background.IsUnset); index--)
        {
            var layer = layers[index];
            if (!layer.Properties.IsVisible)
                continue;

            var cell = layer.GetCell(point);
            if (background.IsUnset)
                background = cell.Background;

            if (!hasFoundGlyph && CompositingPolicy.For(layer.Properties.SpaceHandling).HidesGlyphsBelow(cell))
            {
                glyphs = cell;
                hasFoundGlyph = true;
            }
        }

        return glyphs.WithBackground(background);
    }

    /// <summary>
    /// Composites every cell of <paramref name="region"/> into <paramref name="destination"/>, row
    /// by row, in the same layout as <see cref="Layer.CopyCells"/>. Doesn't allocate.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The region isn't inside the canvas.</exception>
    /// <exception cref="ArgumentException">The destination is shorter than the region's area.</exception>
    public static void CompositeRegion(Frame frame, CellRect region, Span<Cell> destination)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var canvasSize = frame.Document.CanvasSize;
        if (!canvasSize.Bounds.Contains(region))
            throw new ArgumentOutOfRangeException(nameof(region), region, $"The region isn't inside the {canvasSize} canvas.");

        if (destination.Length < region.Area)
            throw new ArgumentException("The destination is shorter than the region's area.", nameof(destination));

        var result = destination[..region.Area];
        result.Clear();

        // Painting from the bottom up, each layer's cell goes over what's below it, so the topmost
        // glyph and the topmost background win. That gives the same result as CompositeCell while
        // reading whole rows at once.
        foreach (var layer in frame.Layers)
        {
            if (!layer.Properties.IsVisible)
                continue;

            var policy = CompositingPolicy.For(layer.Properties.SpaceHandling);
            for (var row = 0; row < region.Height; row++)
            {
                var source = layer.GetRow(region.Y + row).Slice(region.X, region.Width);
                var target = result.Slice(row * region.Width, region.Width);
                for (var column = 0; column < source.Length; column++)
                {
                    var cell = source[column];
                    if (!cell.IsEmpty)
                        target[column] = PaintOver(target[column], cell, policy);
                }
            }
        }
    }

    /// <summary>
    /// What <paramref name="cell"/>, on a layer with <paramref name="policy"/>, shows over <paramref name="below"/>.
    /// </summary>
    internal static Cell PaintOver(Cell below, Cell cell, CompositingPolicy policy)
    {
        var glyphs = policy.HidesGlyphsBelow(cell) ? cell : below;
        var background = cell.HasBackground ? cell.Background : below.Background;
        return glyphs.WithBackground(background);
    }
}
