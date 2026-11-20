// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// The cells that selection shapes cover (ADR 0060). A cell is in a shape when its centre is, and the shape is cut to
/// the canvas.
/// </summary>
public static class SelectionShapes
{
    /// <summary>
    /// The ellipse that fits inside <paramref name="rect"/>: a circle when the rect is square in cells.
    /// </summary>
    public static SelectionMask Ellipse(CanvasSize canvasSize, CellRect rect)
    {
        var area = rect.Intersect(canvasSize.Bounds);
        if (area.IsEmpty)
            return SelectionMask.Empty(canvasSize);

        var builder = new SelectionMaskBuilder(canvasSize, area);
        var radiusX = rect.Width / 2.0;
        var radiusY = rect.Height / 2.0;
        var centreX = rect.Left + radiusX;
        var centreY = rect.Top + radiusY;
        for (var row = area.Top; row < area.Bottom; row++)
        {
            var across = (row + 0.5 - centreY) / radiusY;
            var halfWidth = radiusX * Math.Sqrt(Math.Max(0, 1 - across * across));
            AddCentresBetween(builder, row, centreX - halfWidth, centreX + halfWidth);
        }

        return builder.Build();
    }

    /// <summary>
    /// The cells inside the closed path through <paramref name="corners"/>, the last joined back to the first. Where
    /// the path crosses itself, everything it goes round is inside, so a figure eight covers both loops.
    /// </summary>
    public static SelectionMask Polygon(CanvasSize canvasSize, IReadOnlyList<CanvasPoint> corners)
    {
        ArgumentNullException.ThrowIfNull(corners);
        if (corners.Count < 3)
            return SelectionMask.Empty(canvasSize);

        var top = (int)Math.Floor(corners.Min(corner => corner.Y));
        var bottom = (int)Math.Ceiling(corners.Max(corner => corner.Y));
        var left = (int)Math.Floor(corners.Min(corner => corner.X));
        var right = (int)Math.Ceiling(corners.Max(corner => corner.X));
        var area = CellRect.FromEdges(left, top, Math.Max(left, right), Math.Max(top, bottom))
            .Intersect(canvasSize.Bounds);
        if (area.IsEmpty)
            return SelectionMask.Empty(canvasSize);

        var builder = new SelectionMaskBuilder(canvasSize, area);
        var crossings = new List<(double X, int Winding)>();
        for (var row = area.Top; row < area.Bottom; row++)
        {
            FindCrossings(corners, row + 0.5, crossings);
            AddInside(builder, row, crossings);
        }

        return builder.Build();
    }

    // Where the path crosses the line across the middle of a row, and which way: +1 going down, -1 going up. An edge
    // counts from its upper end down to just above its lower end, so a corner on the line is crossed once.
    private static void FindCrossings(IReadOnlyList<CanvasPoint> corners, double y,
        List<(double X, int Winding)> crossings)
    {
        crossings.Clear();
        for (var index = 0; index < corners.Count; index++)
        {
            var from = corners[index];
            var to = corners[(index + 1) % corners.Count];
            int winding;
            if (from.Y <= y && to.Y > y)
                winding = 1;
            else if (to.Y <= y && from.Y > y)
                winding = -1;
            else
                continue;

            var x = from.X + (y - from.Y) / (to.Y - from.Y) * (to.X - from.X);
            crossings.Add((x, winding));
        }

        crossings.Sort((first, second) => first.X.CompareTo(second.X));
    }

    // Between crossings, a cell is inside wherever the path has gone round it at least once.
    private static void AddInside(SelectionMaskBuilder builder, int row, List<(double X, int Winding)> crossings)
    {
        var winding = 0;
        for (var index = 0; index < crossings.Count - 1; index++)
        {
            winding += crossings[index].Winding;
            if (winding != 0)
                AddCentresBetween(builder, row, crossings[index].X, crossings[index + 1].X);
        }
    }

    // The cells of the row whose centres lie from `from` up to `to`.
    private static void AddCentresBetween(SelectionMaskBuilder builder, int row, double from, double to)
    {
        var first = (int)Math.Ceiling(from - 0.5);
        var last = (int)Math.Floor(to - 0.5);
        if (last >= first)
            builder.AddRun(row, first, last - first + 1);
    }
}
