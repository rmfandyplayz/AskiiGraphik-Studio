// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The zoom a paste brings the view to (ADR 0076): the largest of the usual zoom steps, up to
/// <see cref="LargestZoom"/>, at which the paste fits the usable area with room on every side, or the smallest step
/// when none does, where the biggest pastes still won't fit. Pure maths, so it's tested without a window.
/// </summary>
internal static class PasteFit
{
    /// <summary>The furthest in a paste zooms the view: 150%.</summary>
    public const double LargestZoom = 1.5;

    /// <summary>The zoom factor that fits <paramref name="cells"/> in view.</summary>
    /// <param name="cells">The paste's cells.</param>
    /// <param name="usable">The size of the view's usable area, in device pixels.</param>
    /// <param name="room">The space to leave on every side of the paste, in device pixels.</param>
    /// <param name="cellSizeAt">The cell size at a zoom factor, in device pixels.</param>
    public static double ZoomFor(CellRect cells, PixelSize usable, int room, Func<double, PixelSize> cellSizeAt)
    {
        ArgumentNullException.ThrowIfNull(cellSizeAt);

        var factors = ZoomLevels.Factors;
        for (var index = factors.Count - 1; index >= 0; index--)
        {
            var factor = factors[index];
            if (factor <= LargestZoom && Fits(cells, cellSizeAt(factor), usable, room))
                return factor;
        }

        return ZoomLevels.Minimum;
    }

    private static bool Fits(CellRect cells, PixelSize cellSize, PixelSize usable, int room) =>
        (long)cells.Width * cellSize.Width + 2L * room <= usable.Width &&
        (long)cells.Height * cellSize.Height + 2L * room <= usable.Height;
}
