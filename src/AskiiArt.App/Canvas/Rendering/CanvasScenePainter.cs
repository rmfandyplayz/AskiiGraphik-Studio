// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Paints a <see cref="CanvasScene"/> onto a Skia canvas whose units are device pixels. Draw order
/// follows ARCHITECTURE §12: the composite (paper, cell backgrounds, missing-glyph flags, glyphs),
/// then grid lines, the selection's hatching (left off the hovered cell) and outlines, the hover highlight, the
/// caret, the brush outline, and the tool's line and markers. Safe to call from the render thread.
/// </summary>
internal static class CanvasScenePainter
{
    /// <summary>Below this cell width the grid would cover most of each cell, so it's not drawn.</summary>
    public const int MinimumCellWidthForGrid = 4;

    private const int GridLineWidth = 1;

    public static void Paint(SKCanvas canvas, CanvasScene scene)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(scene);

        var viewport = scene.Viewport;
        var colors = scene.Colors;
        using var paint = new SKPaint();

        canvas.Save();
        canvas.ClipRect(SKRect.Create(0, 0, viewport.ViewportSize.Width, viewport.ViewportSize.Height));
        canvas.Clear(colors.Pasteboard);
        Fill(canvas, paint, viewport.CanvasBounds, colors.Paper);

        // Cells are whole device pixels (§12), so without smoothing each pixel fills its cell exactly.
        var unsmoothed = new SKSamplingOptions(SKFilterMode.Nearest);
        if (scene.Backgrounds is { } backgrounds)
            canvas.DrawImage(backgrounds.Image, ToSkRect(backgrounds.Bounds), unsmoothed);

        foreach (var cell in scene.FallbackGlyphCells)
            Fill(canvas, paint, cell, colors.FallbackGlyph);

        foreach (var cell in scene.PlaceholderGlyphCells)
            Fill(canvas, paint, cell, colors.PlaceholderGlyph);

        // The atlas pages are alpha-only, so a page draws in the paint's colour (white) where it covers
        // its glyph. Modulate multiplies that by each glyph's own colour.
        paint.Color = SKColors.White;
        foreach (var batch in scene.Glyphs)
        {
            if (batch.Page is { } page)
            {
                canvas.DrawAtlas(page.Image, batch.Sprites, batch.Transforms, batch.Colors, SKBlendMode.Modulate,
                    unsmoothed, paint);
            }
        }

        if (scene.IsGridVisible && viewport.CellSize.Width >= MinimumCellWidthForGrid)
            DrawGrid(canvas, paint, viewport, colors.GridLine, scene.GridShade);

        // The hatching leaves the hovered cell out, so its faint highlight reads as on top rather than striped.
        canvas.Save();
        if (scene.HoveredCell is { } underPointer)
            canvas.ClipRect(ToSkRect(underPointer), SKClipOperation.Difference);

        DrawHatch(canvas, paint, scene.UnselectedHatch, viewport);
        DrawHatch(canvas, paint, scene.LostHatch, viewport);
        canvas.Restore();
        foreach (var pass in scene.SelectionOutlines)
            DrawSelectionOutline(canvas, paint, pass, scene.SelectionDashLength, viewport.CanvasOrigin);

        if (scene.HoveredCell is { } hovered)
            Fill(canvas, paint, hovered, colors.Hover);

        if (scene.Caret is { } caret)
            DrawOutline(canvas, paint, caret, scene.CaretThickness, colors.Caret);

        foreach (var strip in scene.BrushOutline)
            Fill(canvas, paint, strip, colors.Caret);

        DrawClosingLine(canvas, paint, scene);
        DrawToolPath(canvas, paint, scene);
        DrawMarkers(canvas, paint, scene);

        canvas.Restore();
    }

    // The tile's lines repeat from the canvas's corner, moved along by how far they've gone, so they stay put on the
    // art while it scrolls, and show only where the cell mask, stretched over the visible cells without smoothing, has
    // a cell. The tile is smoothed, so a move of part of a pixel spreads each line over the two pixels it lies across,
    // and the lines glide rather than jump (ADR 0062). Moved a whole pixel, it lands exactly on the pixels.
    private static void DrawHatch(SKCanvas canvas, SKPaint paint, SelectionHatch? hatch, CanvasViewport viewport)
    {
        if (hatch is null)
            return;

        var bounds = hatch.Bounds;
        var cell = viewport.CellSize;
        var origin = viewport.CanvasOrigin;
        var unsmoothed = new SKSamplingOptions(SKFilterMode.Nearest);
        using var cells = hatch.CellMask.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, unsmoothed,
            SKMatrix.CreateScaleTranslation(cell.Width, cell.Height, bounds.X, bounds.Y));
        using var lines = hatch.Tile.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat,
            new SKSamplingOptions(SKFilterMode.Linear), SKMatrix.CreateTranslation((float)(origin.X - hatch.Shift),
                origin.Y));

        // Keeps the lines (the destination) only where the cells (the source) are opaque.
        using var shader = SKShader.CreateBlend(SKBlendMode.DstIn, lines, cells);
        paint.Color = SKColors.White;
        paint.Shader = shader;
        canvas.DrawRect(ToSkRect(bounds), paint);
        paint.Shader = null;
    }

    // One line along the top and left edge of every visible cell, plus the canvas's right and
    // bottom edges, each drawn inside the canvas. With a shade, each line shows as strongly as the shade has the cell
    // it lies in.
    private static void DrawGrid(SKCanvas canvas, SKPaint paint, CanvasViewport viewport, SKColor color,
        GridShade? shade)
    {
        var visible = viewport.VisibleCells;
        if (visible.IsEmpty)
            return;

        var canvasBounds = viewport.CanvasBounds;
        var top = Math.Max(canvasBounds.Y, 0);
        var bottom = Math.Min(canvasBounds.Bottom, viewport.ViewportSize.Height);
        var left = Math.Max(canvasBounds.X, 0);
        var right = Math.Min(canvasBounds.Right, viewport.ViewportSize.Width);
        using var shaded = GridShaderOf(color, shade, viewport);
        paint.Color = shaded is null ? color : SKColors.White;
        paint.Shader = shaded;

        for (var column = visible.Left; column <= visible.Right; column++)
        {
            var x = Math.Min(canvasBounds.X + column * viewport.CellSize.Width, canvasBounds.Right - GridLineWidth);
            canvas.DrawRect(SKRect.Create(x, top, GridLineWidth, bottom - top), paint);
        }

        for (var row = visible.Top; row <= visible.Bottom; row++)
        {
            var y = Math.Min(canvasBounds.Y + row * viewport.CellSize.Height, canvasBounds.Bottom - GridLineWidth);
            canvas.DrawRect(SKRect.Create(left, y, right - left, GridLineWidth), paint);
        }

        paint.Shader = null;
    }

    // The grid's colour, kept (the destination) only as far as the shade (the source) is opaque over each cell. The
    // shade is stretched over the visible cells without smoothing, and its edge cells carry on past them.
    private static SKShader? GridShaderOf(SKColor color, GridShade? shade, CanvasViewport viewport)
    {
        if (shade is null)
            return null;

        var bounds = shade.Bounds;
        var cell = viewport.CellSize;
        using var colour = SKShader.CreateColor(color);
        using var strengths = shade.Image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
            new SKSamplingOptions(SKFilterMode.Nearest),
            SKMatrix.CreateScaleTranslation(cell.Width, cell.Height, bounds.X, bounds.Y));
        return SKShader.CreateBlend(SKBlendMode.DstIn, colour, strengths);
    }

    // Each strip in the backdrop colour, with dashes of the line colour over it. The dashes are counted from the
    // canvas's corner, so they line up where strips meet and stay put on the art while it scrolls, and then moved
    // along by how far they've run, clockwise around the cells.
    private static void DrawSelectionOutline(SKCanvas canvas, SKPaint paint, SelectionOutlinePass pass, int dash,
        PixelPoint origin)
    {
        var period = dash * 2;
        var run = pass.DashOffset % period;
        foreach (var strip in pass.Strips)
        {
            var rect = strip.Rect;
            Fill(canvas, paint, rect, pass.Backdrop);
            paint.Color = pass.Line;

            var start = strip.IsHorizontal ? rect.X : rect.Y;
            var end = strip.IsHorizontal ? rect.Right : rect.Bottom;
            var firstDash = (strip.IsHorizontal ? origin.X : origin.Y) + (strip.RunsForward ? run : -run);
            var dashStart = start - (((start - firstDash) % period) + period) % period;
            for (; dashStart < end; dashStart += period)
            {
                var from = Math.Max(dashStart, start);
                var to = Math.Min(dashStart + dash, end);
                if (to <= from)
                    continue;

                var piece = strip.IsHorizontal
                    ? SKRect.Create(from, rect.Y, to - from, rect.Height)
                    : SKRect.Create(rect.X, from, rect.Width, to - from);
                canvas.DrawRect(piece, paint);
            }
        }
    }

    // The line in the selection outline's colours, a thin line along a backdrop three times as wide, so it shows on any
    // art. A placeholder look (docs/TEMPORARY-UI.md).
    private static void DrawToolPath(SKCanvas canvas, SKPaint paint, CanvasScene scene)
    {
        var points = scene.ToolPath;
        if (points.Count < 2)
            return;

        using var path = new SKPath();
        path.MoveTo((float)points[0].X, (float)points[0].Y);
        for (var index = 1; index < points.Count; index++)
            path.LineTo((float)points[index].X, (float)points[index].Y);

        paint.Style = SKPaintStyle.Stroke;
        paint.IsAntialias = true;
        paint.StrokeJoin = SKStrokeJoin.Round;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeWidth = scene.ToolPathThickness * 3;
        paint.Color = scene.Colors.SelectionBackdrop;
        canvas.DrawPath(path, paint);
        paint.StrokeWidth = scene.ToolPathThickness;
        paint.Color = scene.Colors.SelectionLine;
        canvas.DrawPath(path, paint);
        UseFill(paint);
    }

    // Like the tool's line, but faded and dashed, so it reads as where the line would go rather than where it is.
    private static void DrawClosingLine(SKCanvas canvas, SKPaint paint, CanvasScene scene)
    {
        var opacity = scene.Look.ClosingLineOpacity;
        if (scene.ToolPathClosingLine is not var (from, to) || opacity <= 0)
            return;

        var start = new SKPoint((float)from.X, (float)from.Y);
        var end = new SKPoint((float)to.X, (float)to.Y);
        paint.Style = SKPaintStyle.Stroke;
        paint.IsAntialias = true;
        paint.StrokeCap = SKStrokeCap.Butt;
        paint.StrokeWidth = scene.ToolPathThickness * 3;
        paint.Color = SelectionLook.Faded(scene.Colors.SelectionBackdrop, opacity);
        canvas.DrawLine(start, end, paint);

        var dash = scene.SelectionDashLength;
        using var dashes = SKPathEffect.CreateDash([dash, dash], 0);
        paint.PathEffect = dashes;
        paint.StrokeWidth = scene.ToolPathThickness;
        paint.Color = SelectionLook.Faded(scene.Colors.SelectionLine, opacity);
        canvas.DrawLine(start, end, paint);
        paint.PathEffect = null;
        UseFill(paint);
    }

    // The corners first, so the marker that finishes the shape is never hidden under one.
    private static void DrawMarkers(SKCanvas canvas, SKPaint paint, CanvasScene scene)
    {
        if (scene.ToolMarkers.Count == 0)
            return;

        paint.IsAntialias = true;
        foreach (var marker in scene.ToolMarkers)
        {
            if (marker.Kind == ToolMarkerKind.Corner)
                DrawCornerMarker(canvas, paint, scene, marker.Centre);
        }

        foreach (var marker in scene.ToolMarkers)
        {
            if (marker.Kind != ToolMarkerKind.Corner)
                DrawFinishMarker(canvas, paint, scene, marker);
        }

        UseFill(paint);
    }

    // A small dot in the backdrop colour, ringed in the line colour, both a little faded.
    private static void DrawCornerMarker(SKCanvas canvas, SKPaint paint, CanvasScene scene, Point centre)
    {
        var radius = (float)scene.Look.CornerMarkerRadius;
        if (radius <= 0)
            return;

        var opacity = scene.Look.CornerMarkerOpacity;
        var thickness = scene.ToolPathThickness;
        var point = new SKPoint((float)centre.X, (float)centre.Y);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SelectionLook.Faded(scene.Colors.SelectionBackdrop, opacity);
        canvas.DrawCircle(point, radius, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = thickness;
        paint.Color = SelectionLook.Faded(scene.Colors.SelectionLine, opacity);
        canvas.DrawCircle(point, Math.Max(radius - thickness / 2f, thickness / 2f), paint);
    }

    // A disc in the backdrop colour with a ring and a dot of the line colour in it, all swapped round while the
    // pointer is on it, to show a click there finishes the shape.
    private static void DrawFinishMarker(SKCanvas canvas, SKPaint paint, CanvasScene scene, SceneMarker marker)
    {
        var radius = (float)scene.Look.FinishMarkerRadius;
        if (radius <= 0)
            return;

        var isUnderPointer = marker.Kind == ToolMarkerKind.FinishUnderPointer;
        var disc = isUnderPointer ? scene.Colors.SelectionLine : scene.Colors.SelectionBackdrop;
        var ink = isUnderPointer ? scene.Colors.SelectionBackdrop : scene.Colors.SelectionLine;
        var point = new SKPoint((float)marker.Centre.X, (float)marker.Centre.Y);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = disc;
        canvas.DrawCircle(point, radius, paint);
        canvas.DrawCircle(point, radius * 0.3f, Ink(paint, ink));
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = scene.ToolPathThickness;
        canvas.DrawCircle(point, radius * 0.7f, paint);
    }

    private static SKPaint Ink(SKPaint paint, SKColor color)
    {
        paint.Color = color;
        return paint;
    }

    // Back to plain filled rectangles, as everything else is drawn with.
    private static void UseFill(SKPaint paint)
    {
        paint.Style = SKPaintStyle.Fill;
        paint.IsAntialias = false;
        paint.StrokeWidth = 0;
        paint.PathEffect = null;
    }

    // Four filled edges inside the rect, so the outline lands on whole pixels and never covers
    // the neighbouring cells.
    private static void DrawOutline(SKCanvas canvas, SKPaint paint, PixelRect rect, int thickness, SKColor color)
    {
        var width = Math.Clamp(thickness, 1, Math.Max(1, Math.Min(rect.Width, rect.Height) / 2));
        Fill(canvas, paint, new PixelRect(rect.X, rect.Y, rect.Width, width), color);
        Fill(canvas, paint, new PixelRect(rect.X, rect.Bottom - width, rect.Width, width), color);
        Fill(canvas, paint, new PixelRect(rect.X, rect.Y, width, rect.Height), color);
        Fill(canvas, paint, new PixelRect(rect.Right - width, rect.Y, width, rect.Height), color);
    }

    private static void Fill(SKCanvas canvas, SKPaint paint, PixelRect rect, SKColor color)
    {
        paint.Color = color;
        canvas.DrawRect(ToSkRect(rect), paint);
    }

    private static SKRect ToSkRect(PixelRect rect) => SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);
}
