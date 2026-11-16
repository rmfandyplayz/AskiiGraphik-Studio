// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>The cells a footprint covers as a stroke moves it.</summary>
public static class FootprintSweep
{
    /// <summary>
    /// The cells inside <paramref name="canvas"/> that <paramref name="footprint"/> covers, stamped at every step of a
    /// straight line from <paramref name="fromTopLeft"/> to <paramref name="toTopLeft"/>. Each cell comes once, row by
    /// row, because a fast drag stamps the same cells many times over.
    /// </summary>
    public static IReadOnlyList<CellPoint> Cover(BrushFootprint footprint, CellPoint fromTopLeft, CellPoint toTopLeft,
        CanvasSize canvas)
    {
        ArgumentNullException.ThrowIfNull(footprint);

        var left = Math.Max(Math.Min(fromTopLeft.X, toTopLeft.X), 0);
        var top = Math.Max(Math.Min(fromTopLeft.Y, toTopLeft.Y), 0);
        var right = Math.Min(Math.Max(fromTopLeft.X, toTopLeft.X) + footprint.Width, canvas.Width);
        var bottom = Math.Min(Math.Max(fromTopLeft.Y, toTopLeft.Y) + footprint.Height, canvas.Height);
        if (left >= right || top >= bottom)
            return [];

        // One flag per cell of the swept area, so overlapping stamps cost nothing extra.
        var width = right - left;
        var covered = new bool[width * (bottom - top)];
        foreach (var stamp in CellLine.Between(fromTopLeft, toTopLeft))
        {
            for (var row = 0; row < footprint.Height; row++)
            {
                var y = stamp.Y + row;
                if (y < top || y >= bottom)
                    continue;

                var cells = footprint.Rows[row];
                var start = Math.Max(stamp.X + cells.Start, left);
                var end = Math.Min(stamp.X + cells.End, right);
                if (start < end)
                    covered.AsSpan((y - top) * width + start - left, end - start).Fill(true);
            }
        }

        var points = new List<CellPoint>();
        for (var index = 0; index < covered.Length; index++)
        {
            if (covered[index])
                points.Add(new CellPoint(left + index % width, top + index / width));
        }

        return points;
    }
}
