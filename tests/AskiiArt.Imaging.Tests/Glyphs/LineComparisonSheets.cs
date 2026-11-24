// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

/// <summary>
/// Step 5.1's comparison sheets: the same lines and shapes drawn in each <see cref="LineStyle"/>, one above another,
/// as PNGs for the owner to judge by eye. Not a test: it only runs when asked for, with
/// <c>dotnet test --project tests/AskiiArt.Imaging.Tests --filter-class "*LineComparisonSheets" --explicit only</c>,
/// and writes to <c>.local/line-sheets</c>. Temporary: step 5.2 removes it.
/// </summary>
public sealed class LineComparisonSheets
{
    private const float GlyphPixelSize = 16;
    private const float LabelPixelSize = 15;
    private const int Gutter = 12;
    private const int LabelWidth = 150;
    private const int HeaderHeight = 30;
    private static readonly Rune _singleCharacter = new('*');

    private static readonly (string Label, LineStyle? Style)[] _rows =
    [
        ("The path", null),
        ("Standard", LineStyle.Standard),
        ("Standard by step", LineStyle.StandardByStep),
        ("Precise", LineStyle.Precise),
        ("Single character", LineStyle.SingleCharacter)
    ];

    [Fact(Explicit = true)]
    public void RenderAll()
    {
        var folder = Path.Combine(RepositoryRoot(), ".local", "line-sheets");
        Directory.CreateDirectory(folder);
        foreach (var font in BundledFonts.All)
        {
            using var loaded = FontLoader.LoadBundled(font);
            using var densities = new GlyphDensityMeter(loaded, FixedFallbackFontProvider.None);
            var metrics = CellMetrics.Measure(loaded, GlyphPixelSize);
            var aspect = (double)metrics.Width / metrics.Height;
            var picker = new LineGlyphPicker(aspect, densities, CharacterSet.Ascii);
            var name = font.FamilyName.Replace(" ", "", StringComparison.Ordinal);
            RenderSheet(Path.Combine(folder, $"{name}-lines.png"), $"{font.FamilyName}: lines", loaded, metrics,
                picker, LinesTile(aspect));
            RenderSheet(Path.Combine(folder, $"{name}-shapes.png"), $"{font.FamilyName}: shapes", loaded, metrics,
                picker, ShapesTile(aspect));
        }
    }

    // Lines rising at angles from 5° to 85° as they look on screen, then a star of lines every 15°.
    private static (CanvasSize Size, List<LinePath> Paths) LinesTile(double aspect)
    {
        const double length = 11;
        double[] angles = [5, 10, 15, 20, 25, 30, 40, 50, 60, 70, 80, 85];
        var paths = new List<LinePath>();
        var left = 1.0;
        const double bottom = 12;
        foreach (var angle in angles)
        {
            var radians = angle * Math.PI / 180;
            var across = Math.Round(length * Math.Cos(radians) / aspect);
            var up = Math.Round(length * Math.Sin(radians));
            paths.Add(LinePath.Straight(new CanvasPoint(left + 0.5, bottom + 0.5),
                new CanvasPoint(left + across + 0.5, bottom - up + 0.5)));
            left += across + 3;
        }

        var width = (int)left + 2;
        var centreX = Math.Floor(width / 2.0) + 0.5;
        const double centreY = 26.5;
        const double inner = 3;
        const double outer = 11;
        for (var angle = 0; angle < 360; angle += 15)
        {
            var radians = angle * Math.PI / 180;
            var cos = Math.Cos(radians) / aspect;
            var sin = Math.Sin(radians);
            paths.Add(LinePath.Straight(
                new CanvasPoint(Math.Round(centreX + inner * cos - 0.5) + 0.5,
                    Math.Round(centreY + inner * sin - 0.5) + 0.5),
                new CanvasPoint(Math.Round(centreX + outer * cos - 0.5) + 0.5,
                    Math.Round(centreY + outer * sin - 0.5) + 0.5)));
        }

        return (new CanvasSize(width, 39), paths);
    }

    // Rectangles, circles that look round, wider ellipses, and curves.
    private static (CanvasSize Size, List<LinePath> Paths) ShapesTile(double aspect)
    {
        var circle = (int)Math.Round(13 / aspect);
        var smallCircle = (int)Math.Round(7 / aspect);
        List<LinePath> paths =
        [
            LinePath.Rectangle(new CellRect(1, 1, 18, 9)),
            LinePath.Rectangle(new CellRect(21, 1, 6, 3)),
            LinePath.Rectangle(new CellRect(21, 6, 3, 4)),
            LinePath.Ellipse(new CellRect(30, 1, circle, 13)),
            LinePath.Ellipse(new CellRect(32 + circle, 1, smallCircle, 7)),
            LinePath.Ellipse(new CellRect(32 + circle, 9, 8, 4)),
            LinePath.Ellipse(new CellRect(1, 13, 40, 11)),
            LinePath.Ellipse(new CellRect(43, 15, 14, 9)),
            LinePath.Ellipse(new CellRect(59, 15, 5, 9))
        ];

        var left = 35.0 + circle + smallCircle;
        paths.Add(Curve(left, 5, 8, 40, wave => Math.Sin(2 * Math.PI * wave)));
        paths.Add(Curve(left, 13, 4, 40, wave => Math.Sin(4 * Math.PI * wave)));
        paths.Add(Curve(left + 3, 18, -8, 34, wave => 1 - 4 * (wave - 0.5) * (wave - 0.5)));
        var spiralLeft = left + 44 + Math.Ceiling(11 / aspect);
        paths.Add(Spiral(spiralLeft, 12, aspect));
        return (new CanvasSize((int)(spiralLeft + 11 / aspect) + 3, 25), paths);
    }

    private static LinePath Curve(double left, double middle, double height, double width, Func<double, double> shape)
    {
        const int steps = 200;
        return LinePath.Through(
            Enumerable.Range(0, steps + 1).Select(step =>
            {
                var wave = (double)step / steps;
                return new CanvasPoint(left + 0.5 + wave * width, middle + 0.5 - height / 2 * shape(wave));
            }),
            closed: false);
    }

    private static LinePath Spiral(double centreX, double centreY, double aspect)
    {
        const int steps = 400;
        return LinePath.Through(
            Enumerable.Range(0, steps + 1).Select(step =>
            {
                var turn = 2.5 * 2 * Math.PI * step / steps;
                var radius = 1 + 10.0 * step / steps;
                return new CanvasPoint(centreX + radius * Math.Cos(turn) / aspect, centreY + radius * Math.Sin(turn));
            }),
            closed: false);
    }

    private static void RenderSheet(string path, string title, LoadedFont font, CellMetrics metrics,
        LineGlyphPicker picker, (CanvasSize Size, List<LinePath> Paths) tile)
    {
        var tileWidth = tile.Size.Width * metrics.Width;
        var tileHeight = tile.Size.Height * metrics.Height;
        var width = LabelWidth + tileWidth + Gutter;
        var height = HeaderHeight + _rows.Length * (tileHeight + Gutter);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var labelFont = new SKFont(SKTypeface.Default, LabelPixelSize);
        using var labelPaint = new SKPaint();
        labelPaint.Color = SKColors.Black;
        labelPaint.IsAntialias = true;
        using var tilePaint = new SKPaint();
        tilePaint.Color = new SKColor(0xF4, 0xF4, 0xF4);
        using var pathPaint = new SKPaint();
        pathPaint.Color = new SKColor(0xD0, 0x30, 0x30);
        pathPaint.IsAntialias = true;
        pathPaint.StrokeWidth = 1.5f;
        pathPaint.Style = SKPaintStyle.Stroke;
        using var glyphFont = new SKFont(font.Typeface, metrics.PixelSize);
        glyphFont.Subpixel = true;
        using var glyphPaint = new SKPaint();
        glyphPaint.Color = SKColors.Black;
        glyphPaint.IsAntialias = true;

        canvas.DrawText(title, 4, 20, labelFont, labelPaint);
        for (var row = 0; row < _rows.Length; row++)
        {
            var top = HeaderHeight + row * (tileHeight + Gutter);
            canvas.DrawText(_rows[row].Label, 4, top + 20, labelFont, labelPaint);
            canvas.DrawRect(LabelWidth, top, tileWidth, tileHeight, tilePaint);
            foreach (var linePath in tile.Paths)
            {
                if (_rows[row].Style is { } style)
                {
                    DrawGlyphs(canvas, picker.Pick(linePath, style, _singleCharacter), top, metrics, glyphFont,
                        glyphPaint);
                }
                else
                {
                    DrawPath(canvas, linePath, top, metrics, pathPaint);
                }
            }
        }

        Save(bitmap, path);
        var rowsFolder = Path.Combine(Path.GetDirectoryName(path)!, "rows");
        Directory.CreateDirectory(rowsFolder);
        for (var row = 0; row < _rows.Length; row++)
        {
            using var part = new SKBitmap();
            var top = HeaderHeight + row * (tileHeight + Gutter);
            bitmap.ExtractSubset(part, new SKRectI(LabelWidth, top, LabelWidth + tileWidth, top + tileHeight));
            Save(part, Path.Combine(rowsFolder, $"{Path.GetFileNameWithoutExtension(path)}-{row}.png"));
        }
    }

    private static void Save(SKBitmap bitmap, string path)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        data.SaveTo(file);
    }

    private static void DrawGlyphs(SKCanvas canvas, IReadOnlyList<LineGlyph> glyphs, int top, CellMetrics metrics,
        SKFont glyphFont, SKPaint glyphPaint)
    {
        foreach (var (cell, glyph) in glyphs)
        {
            canvas.DrawText(glyph.ToString(), LabelWidth + cell.X * metrics.Width,
                top + cell.Y * metrics.Height + metrics.Baseline, glyphFont, glyphPaint);
        }
    }

    private static void DrawPath(SKCanvas canvas, LinePath linePath, int top, CellMetrics metrics, SKPaint paint)
    {
        using var skPath = new SKPath();
        for (var index = 0; index < linePath.Points.Count; index++)
        {
            var point = linePath.Points[index];
            var x = LabelWidth + (float)(point.X * metrics.Width);
            var y = top + (float)(point.Y * metrics.Height);
            if (index == 0)
                skPath.MoveTo(x, y);
            else
                skPath.LineTo(x, y);
        }

        if (linePath.IsClosed)
            skPath.Close();

        canvas.DrawPath(skPath, paint);
    }

    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "AskiiArt.slnx")))
            folder = folder.Parent;

        return folder?.FullName ?? throw new InvalidOperationException("The repository's root wasn't found.");
    }
}
