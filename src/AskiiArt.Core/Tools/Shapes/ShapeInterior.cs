// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Shapes;

/// <summary>The cells inside a closed outline, for filling a rectangle or an ellipse (ADR 0080).</summary>
internal static class ShapeInterior
{
    /// <summary>
    /// The cells of <paramref name="box"/> that <paramref name="outline"/> closes in, row by row: those that can't be
    /// reached from outside the box by stepping across, up or down without crossing an outline cell. An outline that
    /// steps diagonally from cell to cell still closes, since such a step can't be crossed that way.
    /// </summary>
    public static List<CellPoint> Of(CellRect box, IEnumerable<CellPoint> outline)
    {
        ArgumentNullException.ThrowIfNull(outline);

        // The box with a ring of cells round it, so that everywhere outside the outline joins up.
        var width = box.Width + 2;
        var height = box.Height + 2;
        var isOutline = new bool[width * height];
        foreach (var cell in outline)
        {
            if (box.Contains(cell))
                isOutline[Index(cell.X - box.Left + 1, cell.Y - box.Top + 1)] = true;
        }

        var isOutside = new bool[width * height];
        var waiting = new Stack<int>();
        isOutside[0] = true;
        waiting.Push(0);
        while (waiting.TryPop(out var index))
        {
            var x = index % width;
            var y = index / width;
            Reach(x - 1, y);
            Reach(x + 1, y);
            Reach(x, y - 1);
            Reach(x, y + 1);
        }

        var inside = new List<CellPoint>();
        for (var y = 1; y < height - 1; y++)
        {
            for (var x = 1; x < width - 1; x++)
            {
                var index = Index(x, y);
                if (!isOutline[index] && !isOutside[index])
                    inside.Add(new CellPoint(box.Left + x - 1, box.Top + y - 1));
            }
        }

        return inside;

        int Index(int x, int y) => y * width + x;

        void Reach(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            var index = Index(x, y);
            if (isOutline[index] || isOutside[index])
                return;

            isOutside[index] = true;
            waiting.Push(index);
        }
    }
}
