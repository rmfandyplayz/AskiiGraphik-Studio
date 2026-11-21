// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Builds a frame's <see cref="SelectionHatch"/>es on the UI thread (ADR 0061): one over the visible cells that aren't
/// selected, its lines rising to the right, and one over the cells a gesture would deselect, its lines falling, in the
/// mode's colours. It also builds the <see cref="GridShade"/> that shows the grid less under them. Only visible cells
/// are visited, a run of selected cells at a time, so a big selection costs what shows. The buffer is kept from frame
/// to frame.
/// </summary>
internal sealed class SelectionHatchBuilder
{
    private const byte Hatched = byte.MaxValue;
    private const byte Clear = 0;
    private const byte FullStrength = byte.MaxValue;

    private byte[] _cells = [];

    /// <summary>
    /// The hatching over the visible cells outside the display's selection and outside the cells it would deselect,
    /// its lines moved <paramref name="shift"/> device pixels to the left, or null when there's none: nothing is or
    /// was selected, or the look draws no hatching.
    /// </summary>
    public SelectionHatch? BuildUnselected(CanvasViewport viewport, SelectionDisplay? display, SelectionLook look,
        double shift)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var visible = viewport.VisibleCells;
        if (!HatchesUnselected(display, look, visible))
            return null;

        Start(visible, Hatched);
        Mark(visible, display.Selected, Clear);
        if (display.Lost is { } lost)
            Mark(visible, lost, Clear);

        return Complete(viewport, visible, look, rises: true, look.HatchLine, look.HatchBackdrop, shift);
    }

    /// <summary>
    /// The hatching over the visible cells the display's gesture would deselect, in its mode's colours, its lines
    /// moved <paramref name="shift"/> device pixels to the left, or null when there's none.
    /// </summary>
    public SelectionHatch? BuildLost(CanvasViewport viewport, SelectionDisplay? display, SelectionLook look,
        CanvasColors colors, double shift)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var visible = viewport.VisibleCells;
        if (VisibleLostOf(display, look, visible) is not { } lost)
            return null;

        Start(visible, Clear);
        Mark(visible, lost, Hatched);
        var (line, backdrop) = look.ColoursOf(display!.LostStyle, colors);
        return Complete(viewport, visible, look, rises: false, SelectionLook.Faded(line, look.LostHatchOpacity),
            SelectionLook.Faded(backdrop, look.LostHatchOpacity), shift);
    }

    /// <summary>
    /// How strongly the grid shows over each visible cell: less under the hatching over cells that aren't selected,
    /// and a little less under the hatching over cells about to be deselected. Null when there's no hatching, so the
    /// grid shows at full strength everywhere.
    /// </summary>
    public GridShade? BuildGridShade(CanvasViewport viewport, SelectionDisplay? display, SelectionLook look)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var visible = viewport.VisibleCells;
        var hatchesUnselected = HatchesUnselected(display, look, visible);
        var lost = VisibleLostOf(display, look, visible);
        if (!hatchesUnselected && lost is null)
            return null;

        if (hatchesUnselected)
        {
            Start(visible, Strength(look.GridUnderHatchOpacity));
            Mark(visible, display!.Selected, FullStrength);
        }
        else
        {
            Start(visible, FullStrength);
        }

        if (lost is not null)
            Mark(visible, lost, Strength(look.GridUnderLostHatchOpacity));

        var image = CellImage(visible);
        return image is null ? null : new GridShade(image, BoundsOf(viewport, visible));
    }

    private static bool HatchesUnselected([NotNullWhen(true)] SelectionDisplay? display, SelectionLook look,
        CellRect visible) => display is { HatchesUnselected: true } && CanHatch(look) && !visible.IsEmpty;

    private static SelectionMask? VisibleLostOf(SelectionDisplay? display, SelectionLook look, CellRect visible)
    {
        if (display?.Lost is not { IsEmpty: false } lost || !CanHatch(look) || !lost.Bounds.Intersects(visible))
            return null;

        return lost;
    }

    // Lines closer than two pixels apart would be a smear, and a look without tokens has none.
    private static bool CanHatch(SelectionLook look) => look.HatchPeriod >= 2;

    private static byte Strength(double opacity) => (byte)Math.Round(byte.MaxValue * Math.Clamp(opacity, 0, 1));

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
        SKColor line, SKColor backdrop, double shift)
    {
        var cellMask = CellImage(visible);
        if (cellMask is null)
            return null;

        var thickness = (float)Math.Max(1, Math.Round(look.HatchThickness));
        var tile = CreateTile(look.HatchTilePeriod, thickness, rises, line, backdrop);
        return new SelectionHatch(cellMask, tile, BoundsOf(viewport, visible), shift);
    }

    private SKImage? CellImage(CellRect visible)
    {
        var info = new SKImageInfo(visible.Width, visible.Height, SKColorType.Alpha8, SKAlphaType.Premul);
        return SKImage.FromPixelCopy(info, _cells.AsSpan(0, visible.Width * visible.Height));
    }

    private static PixelRect BoundsOf(CanvasViewport viewport, CellRect visible)
    {
        var cell = viewport.CellSize;
        var origin = viewport.CanvasOrigin;
        return new PixelRect(origin.X + visible.Left * cell.Width, origin.Y + visible.Top * cell.Height,
            visible.Width * cell.Width, visible.Height * cell.Height);
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
