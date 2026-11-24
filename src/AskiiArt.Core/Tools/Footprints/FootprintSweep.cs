// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Numerics;
using System.Runtime.InteropServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// A cell a soft tip reached, and the greatest weight it reached it with as a whole. Where the tip's shape changed
/// across the cell, <paramref name="PartWeights"/> holds the greatest weight each of its parts reached (ADR 0068), in
/// the order of <see cref="AskiiArt.Core.Ramps.GlyphShape"/>'s parts, and <paramref name="Weight"/> is the lightest of
/// them, the weight the whole cell reached; it's null when every part weighs <paramref name="Weight"/>.
/// </summary>
/// <param name="Point">The cell.</param>
/// <param name="Weight">The greatest weight the whole cell reached.</param>
/// <param name="PartWeights">The greatest weight each part reached, or null.</param>
/// <param name="MiddleWeight">
/// For a cell with <paramref name="PartWeights"/>, the greatest weight its middle reached, which moves it when no
/// glyph would clearly show its edge (ADR 0075); 0 otherwise.
/// </param>
public readonly record struct WeightedCell(
    CellPoint Point,
    double Weight,
    float[]? PartWeights,
    double MiddleWeight = 0);

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

    /// <summary>
    /// As <see cref="Cover"/>, with each cell's greatest weight in <paramref name="falloff"/> over every stamp that
    /// reached it (ADR 0049), and where the tip's shape changed across the cell, the greatest weight of each of its
    /// parts (ADR 0068). Cells only reached with no weight are left out.
    /// </summary>
    public static IReadOnlyList<WeightedCell> CoverWeighted(BrushFalloff falloff, CellPoint fromTopLeft,
        CellPoint toTopLeft, CanvasSize canvas)
    {
        ArgumentNullException.ThrowIfNull(falloff);

        var footprint = falloff.Footprint;
        var left = Math.Max(Math.Min(fromTopLeft.X, toTopLeft.X), 0);
        var top = Math.Max(Math.Min(fromTopLeft.Y, toTopLeft.Y), 0);
        var right = Math.Min(Math.Max(fromTopLeft.X, toTopLeft.X) + footprint.Width, canvas.Width);
        var bottom = Math.Min(Math.Max(fromTopLeft.Y, toTopLeft.Y) + footprint.Height, canvas.Height);
        if (left >= right || top >= bottom)
            return [];

        // One weight per cell of the swept area, so overlapping stamps only ever raise it. Even cells go first, so a
        // shaped cell of a later stamp that an even one has already outweighed costs nothing.
        var width = right - left;
        var weights = new double[width * (bottom - top)];
        var stamps = CellLine.Between(fromTopLeft, toTopLeft).ToArray();
        foreach (var stamp in stamps)
        {
            for (var row = 0; row < footprint.Height; row++)
            {
                if (!TryGetRowSpan(footprint, stamp, row, left, top, right, bottom, out var start, out var end))
                    continue;

                var rowStart = (stamp.Y + row - top) * width - left;
                for (var x = start; x < end; x++)
                {
                    var column = x - stamp.X;
                    if (falloff.PartWeightsAt(column, row).IsEmpty)
                        weights[rowStart + x] = Math.Max(weights[rowStart + x], falloff.WeightAt(column, row));
                }
            }
        }

        var parts = falloff.HasShapedCells ? new float[]?[weights.Length] : null;
        var middles = parts is null ? null : new double[weights.Length];
        if (parts is not null)
        {
            foreach (var stamp in stamps)
            {
                for (var row = 0; row < footprint.Height; row++)
                {
                    if (!TryGetRowSpan(footprint, stamp, row, left, top, right, bottom, out var start, out var end))
                        continue;

                    var rowStart = (stamp.Y + row - top) * width - left;
                    for (var x = start; x < end; x++)
                    {
                        var column = x - stamp.X;
                        var index = rowStart + x;
                        middles![index] = Math.Max(middles[index], falloff.WeightAt(column, row));
                        if (falloff.PeakWeightAt(column, row) > weights[index])
                            RaiseParts(ref parts[index], falloff.PartWeightsAt(column, row));
                    }
                }
            }
        }

        var reached = new List<WeightedCell>();
        for (var index = 0; index < weights.Length; index++)
        {
            var cellParts = parts?[index] is { } raised ? EvenedOut(raised, ref weights[index]) : null;
            if (weights[index] > 0 || cellParts is not null)
            {
                var point = new CellPoint(left + index % width, top + index / width);
                var middle = cellParts is null ? 0 : middles![index];
                reached.Add(new WeightedCell(point, weights[index], cellParts, middle));
            }
        }

        return reached;
    }

    // The columns, inside the swept area, of the canvas cells that row of the footprint stamped at stamp covers, or
    // false when it covers none there.
    private static bool TryGetRowSpan(BrushFootprint footprint, CellPoint stamp, int row, int left, int top,
        int right, int bottom, out int start, out int end)
    {
        var y = stamp.Y + row;
        var cells = footprint.Rows[row];
        start = Math.Max(stamp.X + cells.Start, left);
        end = Math.Min(stamp.X + cells.End, right);
        return y >= top && y < bottom && start < end;
    }

    // Raises the cell's part weights to a shaped stamp's. An even stamp cell's weight is already in the cell's.
    private static void RaiseParts(ref float[]? cellParts, ReadOnlySpan<float> stampParts)
    {
        if (stampParts.IsEmpty)
            return;

        if (cellParts is null)
        {
            cellParts = stampParts.ToArray();
            return;
        }

        var cell = MemoryMarshal.Cast<float, Vector<float>>(cellParts.AsSpan());
        var stamp = MemoryMarshal.Cast<float, Vector<float>>(stampParts);
        for (var block = 0; block < cell.Length; block++)
            cell[block] = Vector.Max(cell[block], stamp[block]);

        for (var part = cell.Length * Vector<float>.Count; part < cellParts.Length; part++)
            cellParts[part] = Math.Max(cellParts[part], stampParts[part]);
    }

    // The parts raised to the cell's even weight, with that weight raised to the lightest part; or null when that
    // leaves them all at it.
    private static float[]? EvenedOut(float[] parts, ref double weight)
    {
        var lightest = float.PositiveInfinity;
        var heaviest = float.NegativeInfinity;
        for (var part = 0; part < parts.Length; part++)
        {
            parts[part] = (float)Math.Max(parts[part], weight);
            lightest = Math.Min(lightest, parts[part]);
            heaviest = Math.Max(heaviest, parts[part]);
        }

        weight = Math.Max(weight, lightest);
        return heaviest > lightest ? parts : null;
    }
}
