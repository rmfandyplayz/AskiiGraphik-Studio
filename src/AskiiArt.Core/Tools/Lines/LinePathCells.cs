// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// One cell a path passes through: which way the path runs there, in cells, and whether the path turns a corner in it.
/// <paramref name="Offset"/> is how far a precise match measures the path from where it lies, to centre it in the
/// cell where no glyph could follow it near the cell's side (see <see cref="PreciseLineMatcher"/>).
/// </summary>
internal readonly record struct LinePathCell(
    CellPoint Cell,
    double DirectionX,
    double DirectionY,
    bool IsCorner,
    CanvasPoint Offset = default);

/// <summary>
/// The cells a path passes through, one to a column where it runs more across than down and one to a row elsewhere, as
/// Bresenham's line takes them, each touching the one before it, in the order the path goes.
/// </summary>
internal static class LinePathCells
{
    // A turn sharper than this, as it looks on screen, makes a corner.
    private const double CornerDegrees = 45;

    // Allows for rounding where a path runs exactly along a cell's middle or edge.
    private const double Tolerance = 1e-9;

    // A path running across is centred from top to bottom in its cells, for precise matching, by more the steeper it
    // is: not at all up to this many rows per column, fully at one.
    private const double CentredFromSlope = 0.5;

    /// <summary>
    /// The cells <paramref name="path"/> passes through, each once. <paramref name="cellWidthToHeight"/> is how wide a
    /// cell looks next to its height, which decides what looks like a corner.
    /// </summary>
    public static List<LinePathCell> Of(LinePath path, double cellWidthToHeight)
    {
        ArgumentNullException.ThrowIfNull(path);
        var samples = new List<LinePathCell>();
        if (path.PieceCount == 0)
        {
            samples.Add(new LinePathCell(path.Points[0].Cell, 1, 0, IsCorner: false));
            return samples;
        }

        for (var index = 0; index < path.PieceCount; index++)
        {
            var (from, to) = path.Piece(index);
            var lastOfOpenPath = !path.IsClosed && index == path.PieceCount - 1;
            if (!path.IsClosed && index == 0)
                samples.Add(CellOnPiece(from.Cell, from, to));

            Sample(from, to, lastOfOpenPath, samples);
            if (lastOfOpenPath)
                samples.Add(CellOnPiece(to.Cell, from, to));
        }

        var cells = Joined(samples, path.IsClosed);
        MarkCorners(path, cellWidthToHeight, cells);
        return cells;
    }

    // Where the piece crosses the middle of each column (or row, when it runs more down than across), the cell it's
    // in. The piece's end counts only when it ends the path, since the next piece starts there.
    private static void Sample(CanvasPoint from, CanvasPoint to, bool includeEnd, List<LinePathCell> samples)
    {
        var across = to.X - from.X;
        var down = to.Y - from.Y;
        if (across == 0 && down == 0)
            return;

        var runsAcross = Math.Abs(across) >= Math.Abs(down);
        var start = runsAcross ? from.X : from.Y;
        var end = runsAcross ? to.X : to.Y;
        var step = end > start ? 1 : -1;
        var (first, last) = MiddlesBetween(start, end, step, includeEnd);
        for (var line = first; step > 0 ? line <= last : line >= last; line += step)
        {
            var share = (line + 0.5 - start) / (end - start);
            CellPoint cell;
            if (runsAcross)
                cell = new CellPoint(line, (int)Math.Floor(from.Y + share * down + Tolerance));
            else
                cell = new CellPoint((int)Math.Floor(from.X + share * across + Tolerance), line);

            samples.Add(CellOnPiece(cell, from, to));
        }
    }

    // A cell the piece from `from` to `to` passes through, with how far a precise match measures the piece from
    // where it lies. Glyphs ink the middle of their cells across, so a steep piece is centred across, measured where
    // it crosses the cell's middle row. Glyphs ink their cells at many heights, so a gentle slope keeps where it lies
    // up and down; but where a piece runs steeply through a column it clips the cell's corners, so it's centred up and
    // down too, by more the steeper it is.
    private static LinePathCell CellOnPiece(CellPoint cell, CanvasPoint from, CanvasPoint to)
    {
        var across = to.X - from.X;
        var down = to.Y - from.Y;
        CanvasPoint offset;
        if (across == 0 && down == 0)
        {
            offset = default;
        }
        else if (Math.Abs(across) < Math.Abs(down))
        {
            var x = from.X + (cell.Y + 0.5 - from.Y) / down * across;
            offset = new CanvasPoint(x - (cell.X + 0.5), 0);
        }
        else
        {
            var y = from.Y + (cell.X + 0.5 - from.X) / across * down;
            var slope = Math.Abs(down / across);
            var centring = Math.Clamp((slope - CentredFromSlope) / (1 - CentredFromSlope), 0, 1);
            offset = new CanvasPoint(0, (y - (cell.Y + 0.5)) * centring);
        }

        return new LinePathCell(cell, across, down, IsCorner: false, offset);
    }

    // The columns (or rows) whose middles lie from start, included, to end, included only when asked, going by step.
    private static (int First, int Last) MiddlesBetween(double start, double end, int step, bool includeEnd)
    {
        if (step > 0)
        {
            var first = (int)Math.Ceiling(start - 0.5 - Tolerance);
            var last = includeEnd
                ? (int)Math.Floor(end - 0.5 + Tolerance)
                : (int)Math.Ceiling(end - 0.5 - Tolerance) - 1;
            return (first, last);
        }

        var firstDown = (int)Math.Floor(start - 0.5 + Tolerance);
        var lastDown = includeEnd
            ? (int)Math.Ceiling(end - 0.5 - Tolerance)
            : (int)Math.Floor(end - 0.5 + Tolerance) + 1;
        return (firstDown, lastDown);
    }

    // Drops repeats and fills any gap between cells that don't touch, so the cells run unbroken.
    private static List<LinePathCell> Joined(List<LinePathCell> samples, bool closed)
    {
        var cells = new List<LinePathCell>(samples.Count);
        var seen = new HashSet<CellPoint>();
        LinePathCell? previous = null;
        foreach (var sample in samples)
        {
            if (previous is { } before)
                AddGap(before, sample, cells, seen);

            if (seen.Add(sample.Cell))
                cells.Add(sample);

            previous = sample;
        }

        if (closed && cells.Count > 1 && previous is { } lastSample)
            AddGap(lastSample, samples[0], cells, seen);

        return cells;
    }

    private static void AddGap(LinePathCell before, LinePathCell after, List<LinePathCell> cells,
        HashSet<CellPoint> seen)
    {
        if (Math.Abs(after.Cell.X - before.Cell.X) <= 1 && Math.Abs(after.Cell.Y - before.Cell.Y) <= 1)
            return;

        foreach (var cell in CellLine.Between(before.Cell, after.Cell))
        {
            if (seen.Add(cell))
                cells.Add(before with { Cell = cell, Offset = default });
        }
    }

    // A point where the path turns sharply, as it looks on screen, makes its cell a corner.
    private static void MarkCorners(LinePath path, double cellWidthToHeight, List<LinePathCell> cells)
    {
        var count = path.Points.Count;
        for (var index = 0; index < count; index++)
        {
            if (!path.IsClosed && (index == 0 || index == count - 1))
                continue;

            var point = path.Points[index];
            var before = path.Points[(index - 1 + count) % count];
            var after = path.Points[(index + 1) % count];
            var inAngle = Math.Atan2(point.Y - before.Y, (point.X - before.X) * cellWidthToHeight);
            var outAngle = Math.Atan2(after.Y - point.Y, (after.X - point.X) * cellWidthToHeight);
            var turn = Math.Abs(Math.IEEERemainder(outAngle - inAngle, 2 * Math.PI)) * 180 / Math.PI;
            if (turn <= CornerDegrees)
                continue;

            var position = cells.FindIndex(cell => cell.Cell == point.Cell);
            if (position >= 0)
                cells[position] = cells[position] with { IsCorner = true };
        }
    }
}
