// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

/// <summary>
/// Step 4.10a's comparison sheets (ADR 0074): the same Brush strokes painted with each variant of
/// <see cref="PatsVariant.SecondRound"/>, side by side, as PNGs for the owner to judge by eye. Not a test: it only runs when asked for, with
/// <c>dotnet test --project tests/AskiiArt.Imaging.Tests --filter-class "*PatsComparisonSheets" --explicit only</c>,
/// and writes to <c>.local/pats-sheets</c>. Temporary: step 4.10b removes it.
/// </summary>
public sealed class PatsComparisonSheets
{
    private const float GlyphPixelSize = 15;
    private const float LabelPixelSize = 15;
    private const int Gutter = 12;
    private const int LabelWidth = 70;
    private const int HeaderHeight = 48;
    private static readonly CanvasSize _tileCells = new(92, 20);
    private static readonly int[] _intensities = [25, 50, 100];
    private static readonly int[] _softnesses = [0, 50, 100];
    private static readonly string[] _fonts = ["JetBrains Mono", "Roboto Mono"];
    private static readonly BuiltInRamp[] _ramps = [BuiltInRamps.Fine, BuiltInRamps.Standard];

    [Fact(Explicit = true)]
    public void RenderAll()
    {
        var folder = Path.Combine(RepositoryRoot(), ".local", "pats-sheets");
        Directory.CreateDirectory(folder);
        foreach (var fontName in _fonts)
        {
            using var font = TestFonts.Load(fontName);
            using var densities = new GlyphDensityMeter(font, FixedFallbackFontProvider.None);
            var metrics = CellMetrics.Measure(font, GlyphPixelSize);
            foreach (var ramp in _ramps)
            {
                foreach (var softness in _softnesses)
                {
                    var name = $"{fontName.Replace(" ", "", StringComparison.Ordinal)}-{ramp.Id.Split('.')[^1]}" +
                               $"-soft{softness}.png";
                    RenderSheet(Path.Combine(folder, name), font, densities, metrics, ramp, softness);
                }
            }
        }
    }

    private static void RenderSheet(string path, LoadedFont font, GlyphDensityMeter densities, CellMetrics metrics,
        BuiltInRamp ramp, int softness)
    {
        var tileWidth = _tileCells.Width * metrics.Width;
        var tileHeight = _tileCells.Height * metrics.Height;
        var variants = PatsVariant.SecondRound;
        var width = LabelWidth + _intensities.Length * (tileWidth + Gutter);
        var height = HeaderHeight + variants.Count * (tileHeight + Gutter);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var labelFont = new SKFont(SKTypeface.Default, LabelPixelSize);
        using var labelPaint = new SKPaint();
        labelPaint.Color = SKColors.Black;
        labelPaint.IsAntialias = true;
        using var tilePaint = new SKPaint();
        tilePaint.Color = new SKColor(0xF4, 0xF4, 0xF4);
        using var glyphFont = new SKFont(font.Typeface, metrics.PixelSize);
        glyphFont.Subpixel = true;
        using var glyphPaint = new SKPaint();
        glyphPaint.Color = SKColors.Black;
        glyphPaint.IsAntialias = true;

        canvas.DrawText($"{font.FamilyName}, {ramp.Id.Split('.')[^1]} ramp, softness {softness}%", 4, 18,
            labelFont, labelPaint);
        for (var column = 0; column < _intensities.Length; column++)
        {
            canvas.DrawText($"intensity {_intensities[column]}%", LabelWidth + column * (tileWidth + Gutter), 40,
                labelFont, labelPaint);
        }

        var cellWidthToHeight = (double)metrics.Width / metrics.Height;
        for (var row = 0; row < variants.Count; row++)
        {
            var top = HeaderHeight + row * (tileHeight + Gutter);
            canvas.DrawText(variants[row].Name, 4, top + tileHeight / 2f, labelFont, labelPaint);
            for (var column = 0; column < _intensities.Length; column++)
            {
                var left = LabelWidth + column * (tileWidth + Gutter);
                canvas.DrawRect(left, top, tileWidth, tileHeight, tilePaint);
                var layer = Paint(variants[row], ramp, _intensities[column], softness, densities, cellWidthToHeight);
                DrawCells(canvas, layer, left, top, metrics, glyphFont, glyphPaint);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        data.SaveTo(file);
    }

    // A circle and a square stamped once each, a diagonal and a curve drawn with a small tip, and a stamp and a line
    // with a size-7 tip, where the owner found 4.6's brush better on the Fine ramp when fully soft.
    private static Layer Paint(PatsVariant variant, BuiltInRamp ramp, int intensity, int softness,
        IGlyphDensityProvider densities, double cellWidthToHeight)
    {
        var session = new EditingSession(Document.Create(_tileCells, "Layer", CharacterSet.Ascii));
        var settings = new ToolSettings();
        settings.For(RampOptions.Group).Set(RampOptions.Ramp, ramp.Id);
        var brushOptions = settings.For(RampBrushOptions.Group);
        brushOptions.Set(RampBrushOptions.Intensity, intensity);
        brushOptions.Set(RampBrushOptions.Softness, softness);
        var tip = settings.For(BrushTool.TipOptions);
        tip.Set(BrushTipOptions.Proportions, "perceptual");

        var context = new ToolContext(session, new PaintColors(), settings, () => cellWidthToHeight,
            () => densities, () => variant);
        var brush = BrushTool.Definition.Create(context);

        tip.Set(BrushTipOptions.Shape, "circle");
        tip.Set(BrushTipOptions.Size, 18);
        Stroke(brush, [(10.5, 10)]);

        tip.Set(BrushTipOptions.Shape, "square");
        tip.Set(BrushTipOptions.Size, 14);
        Stroke(brush, [(29, 10)]);

        // A tip three rows tall, the smallest that's shaped.
        tip.Set(BrushTipOptions.Shape, "circle");
        tip.Set(BrushTipOptions.Size, 6);
        Stroke(brush, Line((40, 3), (52, 17)));
        Stroke(brush, Curve());

        tip.Set(BrushTipOptions.Size, 7);
        Stroke(brush, [(81, 5)]);
        Stroke(brush, Line((76, 15), (90, 12)));

        brush.Deactivate();
        return session.Document.ActiveFrame.Layers[0];
    }

    private static void Stroke(ITool brush, List<(double X, double Y)> points)
    {
        brush.PointerPressed(Input(points[0]));
        foreach (var point in points.Skip(1))
            brush.PointerMoved(Input(point));

        brush.PointerReleased(Input(points[^1]));
    }

    private static ToolPointerInput Input((double X, double Y) point)
    {
        var cell = new CellPoint((int)Math.Floor(point.X), (int)Math.Floor(point.Y));
        return new ToolPointerInput(cell, point.X - cell.X, point.Y - cell.Y, ToolModifiers.None);
    }

    private static List<(double X, double Y)> Line((double X, double Y) from, (double X, double Y) to)
    {
        const int steps = 60;
        return
        [
            .. Enumerable.Range(0, steps + 1).Select(step => (
                from.X + (to.X - from.X) * step / steps,
                from.Y + (to.Y - from.Y) * step / steps))
        ];
    }

    // One wave of a sine, across and back.
    private static List<(double X, double Y)> Curve()
    {
        const int steps = 80;
        return
        [
            .. Enumerable.Range(0, steps + 1).Select(step => (
                56.0 + 16.0 * step / steps,
                10 + 6 * Math.Sin(2 * Math.PI * step / steps)))
        ];
    }

    private static void DrawCells(SKCanvas canvas, Layer layer, int left, int top, CellMetrics metrics,
        SKFont glyphFont, SKPaint glyphPaint)
    {
        for (var row = 0; row < _tileCells.Height; row++)
        {
            for (var column = 0; column < _tileCells.Width; column++)
            {
                var cell = layer.GetCell(new CellPoint(column, row));
                if (!cell.HasGlyph)
                    continue;

                canvas.DrawText(cell.Glyph.ToString(), left + column * metrics.Width,
                    top + row * metrics.Height + metrics.Baseline, glyphFont, glyphPaint);
            }
        }
    }

    private static string RepositoryRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "AskiiArt.slnx")))
            folder = folder.Parent;

        return folder?.FullName ?? throw new InvalidOperationException("The repository's root wasn't found.");
    }
}
