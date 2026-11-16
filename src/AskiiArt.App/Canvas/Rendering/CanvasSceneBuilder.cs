// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Imaging.Glyphs;
using Avalonia;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Builds each frame's <see cref="CanvasScene"/> on the UI thread: reads the visible cells from the
/// composite, collects their backgrounds, rasterizes any glyphs the atlas doesn't have yet, and
/// groups the glyphs by atlas page with their colours. Only visible cells are read, so the cost
/// follows the viewport, not the document (ARCHITECTURE §12).
/// </summary>
internal sealed class CanvasSceneBuilder
{
    private const int SpaceCodePoint = ' ';

    private readonly AtlasPageImages _pageImages = new();
    private readonly CellBackgroundsBuilder _backgrounds = new();

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
        _backgrounds.Start(visible.Width, visible.Height);

        for (var y = visible.Top; y < visible.Bottom; y++)
        {
            var row = composite.GetRow(y);
            var cellTop = origin.Y + y * cellSize.Height;

            for (var x = visible.Left; x < visible.Right; x++)
            {
                var cell = row[x];
                if (!cell.Background.IsUnset)
                {
                    _backgrounds.Set(x - visible.Left, y - visible.Top,
                        CellColorRendering.BackgroundColor(cell.Background));
                }

                // No glyph to draw: none at all, or a space, which has no ink in any font.
                if (!cell.HasGlyph || cell.Glyph.Value == SpaceCodePoint)
                    continue;

                var entry = atlas.GetOrAdd(cell.Glyph);
                var cellLeft = origin.X + x * cellSize.Width;
                GetBatch(batchesByPage, entry.PageIndex).Add(entry.Bounds, cellLeft, cellTop,
                    CellColorRendering.TextColor(cell.Foreground, colors.Ink));

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

        var visibleBounds = new PixelRect(origin.X + visible.Left * cellSize.Width,
            origin.Y + visible.Top * cellSize.Height, visible.Width * cellSize.Width, visible.Height * cellSize.Height);

        return new CanvasScene
        {
            Viewport = viewport,
            Colors = colors,
            Backgrounds = _backgrounds.Complete(visibleBounds),
            Glyphs = batches,
            FallbackGlyphCells = fallbackCells,
            PlaceholderGlyphCells = placeholderCells,
            IsGridVisible = overlays.IsGridVisible,
            HoveredCell = overlays.Brush is null ? BoundsOf(viewport, overlays.HoveredCell) : null,
            Caret = BoundsOf(viewport, overlays.Caret),
            CaretThickness = overlays.CaretThickness,
            BrushOutline = OutlineOf(viewport, overlays.Brush, overlays.CaretThickness)
        };
    }

    // Each edge becomes a strip inside its cells, as the caret's outline is, so it lands on whole pixels. Edges off
    // the canvas are kept: the tip covers them, even though it can't paint there.
    private static PixelRect[] OutlineOf(CanvasViewport viewport, BrushOutline? brush, int thickness)
    {
        if (brush is not { } outline)
            return [];

        var cell = viewport.CellSize;
        var width = Math.Clamp(thickness, 1, Math.Max(1, Math.Min(cell.Width, cell.Height) / 2));
        var origin = viewport.CanvasOrigin;
        var left = origin.X + outline.TopLeft.X * cell.Width;
        var top = origin.Y + outline.TopLeft.Y * cell.Height;

        var edges = outline.Footprint.Edges;
        var strips = new PixelRect[edges.Count];
        for (var index = 0; index < edges.Count; index++)
        {
            var edge = edges[index];
            var x = left + edge.Column * cell.Width;
            var y = top + edge.Row * cell.Height;
            strips[index] = edge.Side switch
            {
                FootprintEdgeSide.Top => new PixelRect(x, y, edge.Length * cell.Width, width),
                FootprintEdgeSide.Bottom => new PixelRect(x, y + cell.Height - width, edge.Length * cell.Width, width),
                FootprintEdgeSide.Left => new PixelRect(x, y, width, edge.Length * cell.Height),
                _ => new PixelRect(x + cell.Width - width, y, width, edge.Length * cell.Height)
            };
        }

        return strips;
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
