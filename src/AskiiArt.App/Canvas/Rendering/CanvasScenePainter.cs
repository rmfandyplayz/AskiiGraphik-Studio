// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Paints a <see cref="CanvasScene"/> onto a Skia canvas whose units are device pixels. Draw order
/// follows ARCHITECTURE §12: the composite (paper, missing-glyph flags, glyphs), then grid lines,
/// then the hover highlight. Safe to call from the render thread.
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

        foreach (var cell in scene.FallbackGlyphCells)
            Fill(canvas, paint, cell, colors.FallbackGlyph);

        foreach (var cell in scene.PlaceholderGlyphCells)
            Fill(canvas, paint, cell, colors.PlaceholderGlyph);

        // The atlas pages are alpha-only, so Skia draws each glyph in the paint's colour.
        paint.Color = colors.Ink;
        foreach (var batch in scene.Glyphs)
        {
            if (batch.Page is { } page)
            {
                canvas.DrawAtlas(page.Image, batch.Sprites, batch.Transforms,
                    new SKSamplingOptions(SKFilterMode.Nearest), paint);
            }
        }

        if (scene.IsGridVisible && viewport.CellSize.Width >= MinimumCellWidthForGrid)
            DrawGrid(canvas, paint, viewport, colors.GridLine);

        if (scene.HoveredCell is { } hovered)
            Fill(canvas, paint, hovered, colors.Hover);

        if (scene.Caret is { } caret)
            DrawOutline(canvas, paint, caret, scene.CaretThickness, colors.Caret);

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
        canvas.DrawRect(SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height), paint);
    }
}
