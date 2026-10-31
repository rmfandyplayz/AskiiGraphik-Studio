// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

/// <summary>
/// Draws a "font sheet": sample glyphs in a grid at a few sizes, for the owner to check by eye.
/// Everything, labels included, goes through <see cref="GlyphRasterizer"/>, so the sheet shows
/// exactly what the canvas and PNG export will draw.
/// </summary>
internal static class FontSheet
{
    private const int Columns = 32;
    private const int Margin = 12;
    private const int GridLine = 1;

    /// <summary>The groups of glyphs on the sheet.</summary>
    public static IReadOnlyList<(string Title, string Glyphs)> Sections { get; } =
    [
        ("Printable ASCII", Range(0x20, 0x7E)),
        ("Latin-1", Range(0xA0, 0xFF)),
        ("Box drawing and blocks", Range(0x2500, 0x259F)),
        ("Arrows and shapes", "←↑→↓↔↕↖↗↘↙■□▪▫▲△▶▷▼▽◀◁◆◇○◎●◢◣◤◥★☆♠♣♥♦"),
        ("Fallback check (orange: fallback font; red: placeholder)",
            "漢字かなカナ한글☺☻♪♫😀🎨" + char.ConvertFromUtf32(0x10FFFD))
    ];

    private static SKColor Paper => SKColors.White;

    private static SKColor Ink => SKColors.Black;

    private static SKColor GridColor => new(0xFFD0D0D0);

    private static SKColor FallbackColor => new(0xFFFFD699);

    private static SKColor PlaceholderColor => new(0xFFFF9E9E);

    public static SKBitmap Render(LoadedFont font, IFallbackFontProvider fallbackFonts, IReadOnlyList<float> pixelSizes)
    {
        // Lay out and draw in one pass by recording, then size the bitmap to what was drawn.
        using var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(new SKRect(0, 0, 100_000, 100_000));
        canvas.Clear(Paper);

        var width = 0;
        var y = Margin;
        foreach (var pixelSize in pixelSizes)
        {
            using var rasterizer = new GlyphRasterizer(font, pixelSize, GlyphRenderOptions.Default, fallbackFonts);
            var metrics = rasterizer.Metrics;
            var title = string.Create(CultureInfo.InvariantCulture,
                $"{font.FamilyName}, {pixelSize} px, cell {metrics.Width} x {metrics.Height}");

            y = DrawLabel(canvas, rasterizer, title, y) + metrics.Height / 2;
            foreach (var (sectionTitle, glyphs) in Sections)
            {
                y = DrawLabel(canvas, rasterizer, sectionTitle, y);
                y = DrawGrid(canvas, rasterizer, glyphs, y) + metrics.Height / 2;
            }

            // Labels are ASCII, so their length is their width in cells.
            var longestLabel = Sections.Select(section => section.Title.Length).Append(title.Length).Max();
            var gridWidth = Columns * (metrics.Width + GridLine) + GridLine;
            width = Math.Max(width, 2 * Margin + Math.Max(gridWidth, longestLabel * metrics.Width));
            y += metrics.Height;
        }

        using var picture = recorder.EndRecording();
        var bitmap = new SKBitmap(width, y);
        using var bitmapCanvas = new SKCanvas(bitmap);
        bitmapCanvas.Clear(Paper);
        bitmapCanvas.DrawPicture(picture);
        return bitmap;
    }

    public static void SavePng(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        data.SaveTo(file);
    }

    private static int DrawLabel(SKCanvas canvas, GlyphRasterizer rasterizer, string text, int y)
    {
        var x = Margin;
        foreach (var rune in text.EnumerateRunes())
        {
            rasterizer.Draw(canvas, x, y, rune, Ink);
            x += rasterizer.Metrics.Width;
        }

        return y + rasterizer.Metrics.Height;
    }

    private static int DrawGrid(SKCanvas canvas, GlyphRasterizer rasterizer, string glyphs, int y)
    {
        var (cellWidth, cellHeight) = (rasterizer.Metrics.Width, rasterizer.Metrics.Height);
        var runes = glyphs.EnumerateRunes().ToArray();
        var rows = (runes.Length + Columns - 1) / Columns;

        using var gridPaint = new SKPaint();
        gridPaint.Color = GridColor;
        canvas.DrawRect(Margin, y, Columns * (cellWidth + GridLine) + GridLine,
            rows * (cellHeight + GridLine) + GridLine, gridPaint);

        using var cellPaint = new SKPaint();
        for (var index = 0; index < runes.Length; index++)
        {
            var cellX = Margin + GridLine + index % Columns * (cellWidth + GridLine);
            var cellY = y + GridLine + index / Columns * (cellHeight + GridLine);
            cellPaint.Color = rasterizer.GetSource(runes[index]) switch
            {
                GlyphSource.FallbackFont => FallbackColor,
                GlyphSource.Placeholder => PlaceholderColor,
                _ => Paper
            };
            canvas.DrawRect(cellX, cellY, cellWidth, cellHeight, cellPaint);
            rasterizer.Draw(canvas, cellX, cellY, runes[index], Ink);
        }

        return y + rows * (cellHeight + GridLine) + GridLine;
    }

    private static string Range(int first, int last)
    {
        var builder = new StringBuilder();
        for (var codePoint = first; codePoint <= last; codePoint++)
            builder.Append(char.ConvertFromUtf32(codePoint));

        return builder.ToString();
    }
}
