// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The zoom that brings cells into view, after a paste or a run of arrow presses (ADR 0077): the current zoom when they
/// fit the usable area with room on every side, else the largest of the usual zoom steps below it at which they do, or
/// the smallest step when none does, where the biggest still won't fit. It never zooms in. Pure maths, so it's tested
/// without a window.
/// </summary>
internal static class ViewFit
{
    // Steps like 1/3 aren't exact in floating point, so "the current step" allows for rounding.
    private const double Tolerance = 1e-6;

    /// <summary>The zoom factor that fits <paramref name="cells"/> in view.</summary>
    /// <param name="cells">The cells to bring into view.</param>
    /// <param name="currentZoom">The zoom factor now, the largest the fit can be.</param>
    /// <param name="usable">The size of the view's usable area, in device pixels.</param>
    /// <param name="room">The space to leave on every side of the cells, in device pixels.</param>
    /// <param name="cellSizeAt">The cell size at a zoom factor, in device pixels.</param>
    public static double ZoomFor(CellRect cells, double currentZoom, PixelSize usable, int room,
        Func<double, PixelSize> cellSizeAt)
    {
        ArgumentNullException.ThrowIfNull(cellSizeAt);

        if (Fits(cells, cellSizeAt(currentZoom), usable, room))
            return currentZoom;

        var factors = ZoomLevels.Factors;
        for (var index = factors.Count - 1; index >= 0; index--)
        {
            var factor = factors[index];
            if (factor < currentZoom - Tolerance && Fits(cells, cellSizeAt(factor), usable, room))
                return factor;
        }

        return ZoomLevels.Minimum;
    }

    private static bool Fits(CellRect cells, PixelSize cellSize, PixelSize usable, int room) =>
        (long)cells.Width * cellSize.Width + 2L * room <= usable.Width &&
        (long)cells.Height * cellSize.Height + 2L * room <= usable.Height;
}
