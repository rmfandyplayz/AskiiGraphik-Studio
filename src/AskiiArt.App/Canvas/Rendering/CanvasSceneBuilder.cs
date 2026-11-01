// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Glyphs;
using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Builds each frame's <see cref="CanvasScene"/> on the UI thread: reads the visible cells from the
/// composite, rasterizes any glyphs the atlas doesn't have yet, and groups the glyphs by atlas
/// page. Only visible cells are read, so the cost follows the viewport, not the document
/// (ARCHITECTURE §12).
/// </summary>
internal sealed class CanvasSceneBuilder
{
    private const int SpaceCodePoint = ' ';

    private readonly AtlasPageImages _pageImages = new();

    public CanvasScene Build(CompositeCache composite, GlyphAtlas atlas, CanvasViewport viewport,
        CanvasColors colors, CanvasOverlays overlays)
    {
        ArgumentNullException.ThrowIfNull(composite);
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(viewport);

        var batchesByPage = new List<GlyphBatch?>();
        var fallbackCells = new List<PixelRect>();
        var placeholderCells = new List<PixelRect>();
        var visible = viewport.VisibleCells;
        var origin = viewport.CanvasOrigin;
        var cellSize = viewport.CellSize;

        for (var y = visible.Top; y < visible.Bottom; y++)
        {
            var row = composite.GetRow(y);
            var cellTop = origin.Y + y * cellSize.Height;

            for (var x = visible.Left; x < visible.Right; x++)
            {
                var cell = row[x];

                // Nothing to draw: an empty cell, or a space, which has no ink in any font.
                if (cell.IsEmpty || cell.Glyph.Value == SpaceCodePoint)
                    continue;

                var entry = atlas.GetOrAdd(cell.Glyph);
                var cellLeft = origin.X + x * cellSize.Width;
                GetBatch(batchesByPage, entry.PageIndex).Add(entry.Bounds, cellLeft, cellTop);

                var flaggedCells = entry.Source switch
                {
                    GlyphSource.FallbackFont => fallbackCells,
                    GlyphSource.Placeholder => placeholderCells,
                    _ => null
                };
                flaggedCells?.Add(new PixelRect(cellLeft, cellTop, cellSize.Width, cellSize.Height));
            }
        }

        // Copy the pages only now, after every new glyph of this frame has been drawn into them.
        var batches = batchesByPage.OfType<GlyphBatch>().ToArray();
        foreach (var batch in batches)
            batch.Complete(_pageImages.Get(atlas, batch.PageIndex));

        return new CanvasScene
        {
            Viewport = viewport,
            Colors = colors,
            Glyphs = batches,
            FallbackGlyphCells = fallbackCells,
            PlaceholderGlyphCells = placeholderCells,
            IsGridVisible = overlays.IsGridVisible,
            HoveredCell = BoundsOf(viewport, overlays.HoveredCell),
            Caret = BoundsOf(viewport, overlays.Caret),
            CaretThickness = overlays.CaretThickness
        };
    }

    private static PixelRect? BoundsOf(CanvasViewport viewport, CellPoint? cell) =>
        cell is { } point && viewport.CanvasSize.Contains(point) ? viewport.GetCellBounds(point) : null;

    /// <summary>Lets go of the page copies kept between frames (when the canvas leaves the window).</summary>
    public void ReleaseCachedPages() => _pageImages.Clear();

    private static GlyphBatch GetBatch(List<GlyphBatch?> batchesByPage, int pageIndex)
    {
        while (batchesByPage.Count <= pageIndex)
            batchesByPage.Add(null);

        return batchesByPage[pageIndex] ??= new GlyphBatch(pageIndex);
    }
}
