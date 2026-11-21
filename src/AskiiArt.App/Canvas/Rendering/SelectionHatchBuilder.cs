// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Builds a frame's <see cref="SelectionHatch"/>es on the UI thread (ADR 0061): one over the visible cells that aren't
/// selected, its lines rising to the right, and one over the cells a gesture would deselect, its lines falling, in the
/// mode's colours. Only visible cells are visited, a run of selected cells at a time, so a big selection costs what
/// shows. The buffer is kept from frame to frame.
/// </summary>
internal sealed class SelectionHatchBuilder
{
    private const byte Hatched = byte.MaxValue;
    private const byte Clear = 0;

    private byte[] _cells = [];

    /// <summary>
    /// The hatching over the visible cells outside the display's selection and outside the cells it would deselect,
    /// or null when there's none: nothing is selected, or the look draws no hatching.
    /// </summary>
    public SelectionHatch? BuildUnselected(CanvasViewport viewport, SelectionDisplay? display, SelectionLook look)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var visible = viewport.VisibleCells;
        if (display is null || display.Selected.IsEmpty || !CanHatch(look) || visible.IsEmpty)
            return null;

        Start(visible, Hatched);
        Mark(visible, display.Selected, Clear);
        if (display.Lost is { } lost)
            Mark(visible, lost, Clear);

        return Complete(viewport, visible, look, rises: true, look.HatchLine, look.HatchBackdrop);
    }

    /// <summary>
    /// The hatching over the visible cells the display's gesture would deselect, in its mode's colours, or null when
    /// there's none.
    /// </summary>
    public SelectionHatch? BuildLost(CanvasViewport viewport, SelectionDisplay? display, SelectionLook look,
        CanvasColors colors)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var visible = viewport.VisibleCells;
        if (display?.Lost is not { IsEmpty: false } lost || !CanHatch(look) || !lost.Bounds.Intersects(visible))
            return null;

        Start(visible, Clear);
        Mark(visible, lost, Hatched);
        var (line, backdrop) = look.ColoursOf(display.LostStyle, colors);
        return Complete(viewport, visible, look, rises: false, SelectionLook.Faded(line, look.LostHatchOpacity),
            SelectionLook.Faded(backdrop, look.LostHatchOpacity));
    }

    // Lines closer than two pixels apart would be a smear, and a look without tokens has none.
    private static bool CanHatch(SelectionLook look) => look.HatchPeriod >= 2;

    private void Start(CellRect visible, byte value)
    {
        var count = visible.Width * visible.Height;
        if (_cells.Length < count)
            _cells = new byte[count];

        _cells.AsSpan(0, count).Fill(value);
    }

    // Sets the visible cells of the mask to the value, a run at a time.
    private void Mark(CellRect visible, SelectionMask mask, byte value)
    {
        var top = Math.Max(visible.Top, mask.Bounds.Top);
        var bottom = Math.Min(visible.Bottom, mask.Bounds.Bottom);
        for (var row = top; row < bottom; row++)
        {
            var rowStart = (row - visible.Top) * visible.Width;
            foreach (var (column, length) in mask.RunsInRow(row))
            {
                var from = Math.Max(column, visible.Left);
                var to = Math.Min(column + length, visible.Right);
                if (to > from)
                    _cells.AsSpan(rowStart + from - visible.Left, to - from).Fill(value);
            }
        }
    }

    private SelectionHatch? Complete(CanvasViewport viewport, CellRect visible, SelectionLook look, bool rises,
        SKColor line, SKColor backdrop)
    {
        var info = new SKImageInfo(visible.Width, visible.Height, SKColorType.Alpha8, SKAlphaType.Premul);
        var cellMask = SKImage.FromPixelCopy(info, _cells.AsSpan(0, visible.Width * visible.Height));
        if (cellMask is null)
            return null;

        var cell = viewport.CellSize;
        var origin = viewport.CanvasOrigin;
        var bounds = new PixelRect(origin.X + visible.Left * cell.Width, origin.Y + visible.Top * cell.Height,
            visible.Width * cell.Width, visible.Height * cell.Height);
        var period = Math.Max(2, (int)Math.Round(look.HatchPeriod));
        var thickness = (float)Math.Max(1, Math.Round(look.HatchThickness));
        return new SelectionHatch(cellMask, CreateTile(period, thickness, rises, line, backdrop), bounds);
    }

    // A square tile with one diagonal line across it, plus the pieces of its neighbours that cross its corners, so the
    // lines join up when it repeats. The lighter line lies just beside the darker one.
    private static SKImage CreateTile(int period, float thickness, bool rises, SKColor line, SKColor backdrop)
    {
        var info = new SKImageInfo(period, period, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = thickness };
            DrawLines(canvas, paint, period, rises, thickness, backdrop);
            DrawLines(canvas, paint, period, rises, 0, line);
        }

        using var pixels = bitmap.PeekPixels();
        return SKImage.FromPixelCopy(pixels);
    }

    private static void DrawLines(SKCanvas canvas, SKPaint paint, int period, bool rises, float shift, SKColor color)
    {
        paint.Color = color;
        for (var tile = -1; tile <= 1; tile++)
        {
            var left = tile * period + shift - 1;
            var right = tile * period + shift + period + 1;
            if (rises)
                canvas.DrawLine(left, period + 1, right, -1, paint);
            else
                canvas.DrawLine(left, -1, right, period + 1, paint);
        }
    }
}
