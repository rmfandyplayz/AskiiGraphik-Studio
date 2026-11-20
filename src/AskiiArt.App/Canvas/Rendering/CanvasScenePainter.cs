// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Paints a <see cref="CanvasScene"/> onto a Skia canvas whose units are device pixels. Draw order
/// follows ARCHITECTURE §12: the composite (paper, cell backgrounds, missing-glyph flags, glyphs),
/// then grid lines, the selection outline, the hover highlight, the caret and the brush outline. Safe to
/// call from the render thread.
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
            DrawGrid(canvas, paint, viewport, colors.GridLine);

        DrawSelectionOutline(canvas, paint, scene);

        if (scene.HoveredCell is { } hovered)
            Fill(canvas, paint, hovered, colors.Hover);

        if (scene.Caret is { } caret)
            DrawOutline(canvas, paint, caret, scene.CaretThickness, colors.Caret);

        foreach (var strip in scene.BrushOutline)
            Fill(canvas, paint, strip, colors.Caret);

        canvas.Restore();
    }

    // One line along the top and left edge of every visible cell, plus the canvas's right and
    // bottom edges, each drawn inside the canvas.
    private static void DrawGrid(SKCanvas canvas, SKPaint paint, CanvasViewport viewport, SKColor color)
    {
        var visible = viewport.VisibleCells;
        if (visible.IsEmpty)
            return;

        var canvasBounds = viewport.CanvasBounds;
        var top = Math.Max(canvasBounds.Y, 0);
        var bottom = Math.Min(canvasBounds.Bottom, viewport.ViewportSize.Height);
        var left = Math.Max(canvasBounds.X, 0);
        var right = Math.Min(canvasBounds.Right, viewport.ViewportSize.Width);
        paint.Color = color;

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
    }

    // Each strip in the backdrop colour, with dashes of the line colour over it. The dashes are counted from the
    // canvas's corner, so they line up where strips meet and stay put on the art while it scrolls.
    private static void DrawSelectionOutline(SKCanvas canvas, SKPaint paint, CanvasScene scene)
    {
        var dash = scene.SelectionDashLength;
        var period = dash * 2;
        var origin = scene.Viewport.CanvasOrigin;
        foreach (var strip in scene.SelectionOutline)
        {
            var rect = strip.Rect;
            Fill(canvas, paint, rect, scene.Colors.SelectionBackdrop);
            paint.Color = scene.Colors.SelectionLine;

            var start = strip.IsHorizontal ? rect.X : rect.Y;
            var end = strip.IsHorizontal ? rect.Right : rect.Bottom;
            var canvasStart = strip.IsHorizontal ? origin.X : origin.Y;
            var dashStart = start - (((start - canvasStart) % period) + period) % period;
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
