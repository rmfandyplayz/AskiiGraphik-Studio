// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>The cells a straight line passes through, for tools that follow the pointer.</summary>
internal static class CellLine
{
    /// <summary>
    /// Every cell from <paramref name="from"/> to <paramref name="to"/>, both included, each touching
    /// the one before it (Bresenham's line algorithm). The points may lie outside the canvas.
    /// </summary>
    public static IEnumerable<CellPoint> Between(CellPoint from, CellPoint to)
    {
        var width = Math.Abs(to.X - from.X);
        var height = -Math.Abs(to.Y - from.Y);
        var stepX = from.X < to.X ? 1 : -1;
        var stepY = from.Y < to.Y ? 1 : -1;
        var error = width + height;
        var (x, y) = (from.X, from.Y);

        while (true)
        {
            yield return new CellPoint(x, y);
            if (x == to.X && y == to.Y)
                yield break;

            var doubledError = 2 * error;
            if (doubledError >= height)
            {
                error += height;
                x += stepX;
            }

            if (doubledError <= width)
            {
                error += width;
                y += stepY;
            }
        }
    }
}
