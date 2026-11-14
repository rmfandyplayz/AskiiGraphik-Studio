// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Fits one font's box-drawing and block glyphs to the cell (ADR 0039). Each outline is stretched so the font's full
/// block (█) covers the cell exactly, which makes neighbours join without seams at every size. When snapping, the
/// lines of box-drawing glyphs and the edges of block glyphs then move onto whole pixels, so they stay crisp. The
/// outlines are unhinted, so the result is the same on every operating system.
/// </summary>
internal sealed class CellFitting : IDisposable
{
    private const string FullBlock = "█";

    // A full block shorter than this share of the font's line can't be the box the other glyphs fill; a damaged or
    // unusual font gets its line instead.
    private const float MinimumFullBlockHeightPerLine = 0.5f;

    // The straight lines, light, heavy and double, that the font's other box-drawing glyphs are built from.
    private static readonly string[] _horizontalLines = ["─", "━", "═"];
    private static readonly string[] _verticalLines = ["│", "┃", "║"];

    private readonly SKFont _outlineFont;
    private readonly SKSizeI _cellSize;
    private readonly float _lineTop;
    private readonly float _lineBottom;
    private readonly SKRect? _fullBlock;
    private readonly StrokeSnap? _snapAcross;
    private readonly StrokeSnap? _snapDown;

    /// <summary>
    /// Fits <paramref name="font"/>'s glyphs at <paramref name="pixelSize"/> to cells of <paramref name="cellSize"/>,
    /// moving lines and edges onto whole pixels when <paramref name="snapsToPixels"/>. Doesn't own the font.
    /// </summary>
    public CellFitting(LoadedFont font, float pixelSize, SKSizeI cellSize, bool snapsToPixels)
    {
        ArgumentNullException.ThrowIfNull(font);

        _outlineFont = CellMetrics.CreateMeasuringFont(font, pixelSize);
        _cellSize = cellSize;

        // The line, with half its gap above and half below, as the cell is built from it (CellMetrics).
        var vertical = CellMetrics.ReadVerticalMetrics(font, _outlineFont);
        var size = _outlineFont.Size;
        _lineTop = -(vertical.AscentPerEm + vertical.LineGapPerEm / 2) * size;
        _lineBottom = (vertical.DescentPerEm + vertical.LineGapPerEm / 2) * size;
        _fullBlock = MeasureFullBlock(font);
        if (!snapsToPixels)
            return;

        _snapAcross = StrokeSnap.Create(FindStrokes(font, _verticalLines, contour => (contour.Left, contour.Right)),
            cellSize.Width);
        _snapDown = StrokeSnap.Create(FindStrokes(font, _horizontalLines, contour => (contour.Top, contour.Bottom)),
            cellSize.Height);
    }

    /// <summary>
    /// The outline of <paramref name="rune"/> fitted to a cell whose top-left corner is at the origin, or null when
    /// the font has no outline for it (a bitmap glyph). The caller owns the path.
    /// </summary>
    public SKPath? CreateOutline(Rune rune)
    {
        var outline = CreateFittedOutline(rune.ToString());
        if (outline is null || _snapAcross is null || _snapDown is null)
            return outline;

        if (IsBoxDrawing(rune))
            return MovePoints(outline, point => new SKPoint(_snapAcross.Apply(point.X), _snapDown.Apply(point.Y)));

        if (HasStraightBlockEdges(rune))
        {
            return MovePoints(outline, point => new SKPoint(SnapBlockEdge(point.X, _cellSize.Width),
                SnapBlockEdge(point.Y, _cellSize.Height)));
        }

        return outline;
    }

    /// <inheritdoc/>
    public void Dispose() => _outlineFont.Dispose();

    private static bool IsBoxDrawing(Rune rune) => rune.Value is >= 0x2500 and <= 0x257F;

    // Block elements and the sextants, wedges and eighths of Symbols for Legacy Computing: filled shapes whose corners
    // sit at simple fractions of the cell. The shades (░▒▓) are left alone, as their dots get finer than a pixel.
    private static bool HasStraightBlockEdges(Rune rune) =>
        rune.Value is >= 0x2580 and <= 0x2590 or >= 0x2594 and <= 0x259F or >= 0x1FB00 and <= 0x1FB8B;

    // A corner inside the cell moves to the nearest whole pixel, but never onto the cell's edge, so a sliver like ▏
    // stays a pixel wide in a small cell instead of vanishing.
    private static float SnapBlockEdge(float position, int cellSize)
    {
        var snapped = StrokeSnap.RoundToPixel(position);
        var isOnOrPastAnEdge = position < StrokeSnap.EdgeTolerance || position > cellSize - StrokeSnap.EdgeTolerance;
        if (isOnOrPastAnEdge || cellSize < 2)
            return snapped;

        return Math.Clamp(snapped, 1, cellSize - 1);
    }

    // A copy of the outline with every point moved; the original is disposed.
    private static SKPath MovePoints(SKPath outline, Func<SKPoint, SKPoint> move)
    {
        using (outline)
        {
            var moved = new SKPath { FillType = outline.FillType };
            using var iterator = outline.CreateRawIterator();
            var points = new SKPoint[4];
            SKPathVerb verb;
            while ((verb = iterator.Next(points)) != SKPathVerb.Done)
            {
                switch (verb)
                {
                    case SKPathVerb.Move:
                        moved.MoveTo(move(points[0]));
                        break;
                    case SKPathVerb.Line:
                        moved.LineTo(move(points[1]));
                        break;
                    case SKPathVerb.Quad:
                        moved.QuadTo(move(points[1]), move(points[2]));
                        break;
                    case SKPathVerb.Conic:
                        moved.ConicTo(move(points[1]), move(points[2]), iterator.ConicWeight());
                        break;
                    case SKPathVerb.Cubic:
                        moved.CubicTo(move(points[1]), move(points[2]), move(points[3]));
                        break;
                    case SKPathVerb.Close:
                        moved.Close();
                        break;
                }
            }

            return moved;
        }
    }

    // The bounds of each closed shape in the outline.
    private static List<SKRect> MeasureContours(SKPath outline)
    {
        var contours = new List<SKRect>();
        using var iterator = outline.CreateRawIterator();
        var points = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            if (verb == SKPathVerb.Move)
            {
                contours.Add(new SKRect(points[0].X, points[0].Y, points[0].X, points[0].Y));
                continue;
            }

            var pointCount = verb switch
            {
                SKPathVerb.Line => 1,
                SKPathVerb.Quad or SKPathVerb.Conic => 2,
                SKPathVerb.Cubic => 3,
                _ => 0
            };
            if (contours.Count == 0)
                continue;

            var bounds = contours[^1];
            foreach (var point in points.AsSpan(1, pointCount))
            {
                bounds = new SKRect(MathF.Min(bounds.Left, point.X), MathF.Min(bounds.Top, point.Y),
                    MathF.Max(bounds.Right, point.X), MathF.Max(bounds.Bottom, point.Y));
            }

            contours[^1] = bounds;
        }

        return contours;
    }

    // The strokes of each of the font's straight lines along one axis, fitted to the cell: each shape in a line glyph
    // is one stroke.
    private List<IReadOnlyList<(float Start, float End)>> FindStrokes(LoadedFont font, string[] lines,
        Func<SKRect, (float Start, float End)> extentAlongAxis)
    {
        var strokesByLine = new List<IReadOnlyList<(float Start, float End)>>();
        foreach (var line in lines)
        {
            if (!font.HasGlyph(Rune.GetRuneAt(line, 0)))
                continue;

            using var outline = CreateFittedOutline(line);
            if (outline is not null)
                strokesByLine.Add([.. MeasureContours(outline).Select(extentAlongAxis)]);
        }

        return strokesByLine;
    }

    // The glyph's unhinted outline, stretched so the font's full block would cover the cell exactly.
    private SKPath? CreateFittedOutline(string text)
    {
        var outline = _outlineFont.GetTextPath(text, SKPoint.Empty);
        var box = _fullBlock ?? new SKRect(0, _lineTop, _outlineFont.MeasureText(text), _lineBottom);
        if (outline is not { IsEmpty: false } || box.Width <= 0 || box.Height <= 0)
        {
            outline?.Dispose();
            return null;
        }

        var scaleX = _cellSize.Width / box.Width;
        var scaleY = _cellSize.Height / box.Height;
        outline.Transform(SKMatrix.CreateScaleTranslation(scaleX, scaleY, -box.Left * scaleX, -box.Top * scaleY));
        return outline;
    }

    private SKRect? MeasureFullBlock(LoadedFont font)
    {
        if (!font.HasGlyph(Rune.GetRuneAt(FullBlock, 0)))
            return null;

        using var outline = _outlineFont.GetTextPath(FullBlock, SKPoint.Empty);
        if (outline is not { IsEmpty: false })
            return null;

        var bounds = outline.TightBounds;
        if (bounds.Width <= 0 || bounds.Height < MinimumFullBlockHeightPerLine * (_lineBottom - _lineTop))
            return null;

        return bounds;
    }
}
