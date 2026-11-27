// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Imaging.Glyphs;
using Avalonia;
using SkiaSharp;

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
    private readonly SelectionHatchBuilder _hatches = new();

    public CanvasScene Build(CompositeCache composite, GlyphAtlas atlas, CanvasViewport viewport,
        CanvasColors colors, CanvasOverlays overlays, ViewTransform? transform = null)
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
        var toolPath = PathOf(viewport, overlays.ToolPath);
        GridShade? gridShade = null;
        if (overlays.IsGridVisible)
            gridShade = _hatches.BuildGridShade(viewport, overlays.Selection, overlays.Look);

        return new CanvasScene
        {
            Viewport = viewport,
            Transform = transform ?? ViewTransform.Identity,
            Colors = colors,
            Backgrounds = _backgrounds.Complete(visibleBounds),
            Glyphs = batches,
            FallbackGlyphCells = fallbackCells,
            PlaceholderGlyphCells = placeholderCells,
            IsGridVisible = overlays.IsGridVisible,
            HoveredCell = overlays.Brush is null ? BoundsOf(viewport, overlays.HoveredCell) : null,
            Caret = BoundsOf(viewport, overlays.Caret),
            CaretThickness = overlays.CaretThickness,
            BrushOutline = OutlineOf(viewport, overlays.Brush, overlays.CaretThickness),
            GridShade = gridShade,
            UnselectedHatch = _hatches.BuildUnselected(viewport, overlays.Selection, overlays.Look,
                overlays.SelectionMotion.UnselectedHatchShift),
            LostHatch = _hatches.BuildLost(viewport, overlays.Selection, overlays.Look,
                overlays.SelectionMotion.LostHatchShift),
            SelectionOutlines = SelectionOutlinesOf(viewport, overlays, colors),
            SelectionDashLength = Math.Max(1, overlays.SelectionDashLength),
            ToolPath = toolPath,
            ToolPathThickness = Math.Max(1, overlays.SelectionThickness),
            ToolPathClosingLine = ClosingLineOf(toolPath, overlays.ShowsToolPathClosing),
            ToolMarkers = MarkersOf(viewport, overlays.ToolMarkers),
            ToolHandles = HandlesOf(viewport, overlays.ToolHandles),
            Look = overlays.Look
        };
    }

    // The points in device pixels. The line may run off the canvas, as the pointer can.
    private static Point[] PathOf(CanvasViewport viewport, IReadOnlyList<CanvasPoint>? path)
    {
        if (path is null)
            return [];

        var points = new Point[path.Count];
        for (var index = 0; index < points.Length; index++)
            points[index] = ToDevicePixels(viewport, path[index]);

        return points;
    }

    private static (Point From, Point To)? ClosingLineOf(Point[] path, bool showsClosing)
    {
        if (!showsClosing || path.Length < 3)
            return null;

        return (path[^1], path[0]);
    }

    private static SceneMarker[] MarkersOf(CanvasViewport viewport, IReadOnlyList<ToolMarker>? markers)
    {
        if (markers is null)
            return [];

        var sceneMarkers = new SceneMarker[markers.Count];
        for (var index = 0; index < sceneMarkers.Length; index++)
        {
            var marker = markers[index];
            sceneMarkers[index] = new SceneMarker(ToDevicePixels(viewport, marker.Position), marker.Kind);
        }

        return sceneMarkers;
    }

    private static SceneHandle[] HandlesOf(CanvasViewport viewport, IReadOnlyList<ToolHandle>? handles)
    {
        if (handles is null)
            return [];

        var sceneHandles = new SceneHandle[handles.Count];
        for (var index = 0; index < sceneHandles.Length; index++)
        {
            var handle = handles[index];
            sceneHandles[index] = new SceneHandle(ToDevicePixels(viewport, handle.Anchor),
                ToDevicePixels(viewport, handle.End), handle.IsStrong);
        }

        return sceneHandles;
    }

    private static Point ToDevicePixels(CanvasViewport viewport, CanvasPoint point)
    {
        var origin = viewport.CanvasOrigin;
        var cell = viewport.CellSize;
        return new Point(origin.X + point.X * cell.Width, origin.Y + point.Y * cell.Height);
    }

    // Each of the display's outlines that shows: a bright one in full colour with its dashes running, and a faint or
    // vanishing one in colours faded by its strength, standing still. Past the canvas's edges each is weaker, running
    // as it does, or, while a gesture would drop that part, faint and still (ADR 0071).
    private static SelectionOutlinePass[] SelectionOutlinesOf(CanvasViewport viewport, CanvasOverlays overlays,
        CanvasColors colors)
    {
        if (overlays.Selection is not { } display)
            return [];

        var look = overlays.Look;
        var passes = new List<SelectionOutlinePass>(display.Outlines.Count);
        foreach (var layer in display.Outlines)
        {
            var (onCanvas, offCanvas) = SelectionOutlineOf(viewport, layer.Mask, overlays.SelectionThickness);
            var (line, backdrop) = look.ColoursOf(layer.Style, colors);
            var isBright = layer.Strength == SelectionOutlineStrength.Bright;
            var dashRun = isBright ? overlays.SelectionMotion.DashRun : 0;
            AddPass(passes, onCanvas, line, backdrop, look.OpacityOf(layer.Strength), dashRun);
            var offCanvasOpacity = look.OpacityOf(layer.Strength) * look.OffCanvasOpacity;
            var offCanvasDashRun = dashRun;
            if (display.OffCanvas == OffCanvasLines.Dropped)
            {
                offCanvasOpacity = look.DroppedOffCanvasOpacity;
                offCanvasDashRun = 0;
            }

            AddPass(passes, offCanvas, line, backdrop, offCanvasOpacity, offCanvasDashRun);
        }

        return [.. passes];
    }

    private static void AddPass(List<SelectionOutlinePass> passes, SelectionStrip[] strips, SKColor line,
        SKColor backdrop, double opacity, int dashRun)
    {
        if (strips.Length == 0 || opacity <= 0)
            return;

        passes.Add(new SelectionOutlinePass(strips, SelectionLook.Faded(line, opacity),
            SelectionLook.Faded(backdrop, opacity), dashRun));
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
                CellEdgeSide.Top => new PixelRect(x, y, edge.Length * cell.Width, width),
                CellEdgeSide.Bottom => new PixelRect(x, y + cell.Height - width, edge.Length * cell.Width, width),
                CellEdgeSide.Left => new PixelRect(x, y, width, edge.Length * cell.Height),
                _ => new PixelRect(x + cell.Width - width, y, width, edge.Length * cell.Height)
            };
        }

        return strips;
    }

    // The selection's edges that lie in the visible cells, on the canvas and past its edges, cut to them, so a
    // selection far bigger than the view costs only what shows. Each is a strip inside its cells, like the brush
    // outline, and lies on the canvas or past it as its cells do.
    private static (SelectionStrip[] OnCanvas, SelectionStrip[] OffCanvas) SelectionOutlineOf(CanvasViewport viewport,
        SelectionMask? selection, int thickness)
    {
        var visible = viewport.VisibleCellsAroundCanvas;
        if (selection is null || selection.IsEmpty || !selection.Bounds.Intersects(visible))
            return ([], []);

        var cell = viewport.CellSize;
        var width = Math.Clamp(thickness, 1, Math.Max(1, Math.Min(cell.Width, cell.Height) / 2));
        var canvas = viewport.CanvasSize.Bounds;
        var onCanvas = new List<SelectionStrip>();
        var offCanvas = new List<SelectionStrip>();
        foreach (var edge in selection.Edges)
        {
            var isAlongRow = edge.Side is CellEdgeSide.Top or CellEdgeSide.Bottom;
            var cells = isAlongRow
                ? new CellRect(edge.Column, edge.Row, edge.Length, 1)
                : new CellRect(edge.Column, edge.Row, 1, edge.Length);
            var shown = cells.Intersect(visible);
            if (shown.IsEmpty)
                continue;

            var inside = shown.Intersect(canvas);
            if (inside.IsEmpty)
            {
                offCanvas.Add(StripOf(viewport, shown, edge.Side, width));
                continue;
            }

            onCanvas.Add(StripOf(viewport, inside, edge.Side, width));
            var (before, after) = isAlongRow
                ? (CellRect.FromEdges(shown.Left, shown.Top, inside.Left, shown.Bottom),
                    CellRect.FromEdges(inside.Right, shown.Top, shown.Right, shown.Bottom))
                : (CellRect.FromEdges(shown.Left, shown.Top, shown.Right, inside.Top),
                    CellRect.FromEdges(shown.Left, inside.Bottom, shown.Right, shown.Bottom));
            if (!before.IsEmpty)
                offCanvas.Add(StripOf(viewport, before, edge.Side, width));

            if (!after.IsEmpty)
                offCanvas.Add(StripOf(viewport, after, edge.Side, width));
        }

        return ([.. onCanvas], [.. offCanvas]);
    }

    // A strip along one side of a row or column of cells, inside them.
    private static SelectionStrip StripOf(CanvasViewport viewport, CellRect cells, CellEdgeSide side, int width)
    {
        var cell = viewport.CellSize;
        var origin = viewport.CanvasOrigin;
        var x = origin.X + cells.X * cell.Width;
        var y = origin.Y + cells.Y * cell.Height;
        var rect = side switch
        {
            CellEdgeSide.Top => new PixelRect(x, y, cells.Width * cell.Width, width),
            CellEdgeSide.Bottom => new PixelRect(x, y + cell.Height - width, cells.Width * cell.Width, width),
            CellEdgeSide.Left => new PixelRect(x, y, width, cells.Height * cell.Height),
            _ => new PixelRect(x + cell.Width - width, y, width, cells.Height * cell.Height)
        };
        return new SelectionStrip(rect, side);
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
