// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Glyphs;

/// <summary>
/// Moves a font's line strokes onto whole pixels along one axis of a cell, as hinting would, and stretches everything
/// between them to follow (ADR 0039). The cell's edges stay put, so glyphs still join their neighbours, and every
/// glyph built from the same strokes moves the same way, so a line stays straight from cell to cell.
/// </summary>
internal sealed class StrokeSnap
{
    /// <summary>How close to the cell's edge, in pixels, a position counts as on it.</summary>
    internal const float EdgeTolerance = 0.01f;

    // Stroke edges closer together than this, in pixels, are the same edge.
    private const float SamePositionTolerance = 0.001f;

    // How many pixels past the ones either side of it an edge may move to make room for its neighbours.
    private const int ExtraPixelsToTry = 1;

    // A font's lines have a handful of stroke edges along each axis. Far more means the glyphs aren't simple lines,
    // and trying every arrangement of their edges would take too long, so they're just rounded.
    private const int MostEdgesToArrange = 10;

    // Where each anchor moves from and to, in increasing order; positions between two anchors are interpolated.
    private readonly float[] _from;
    private readonly float[] _to;

    private StrokeSnap(float[] from, float[] to)
    {
        _from = from;
        _to = to;
    }

    /// <summary>
    /// Snaps the strokes of a font's straight lines in a cell <paramref name="cellSize"/> pixels long.
    /// <paramref name="lines"/> holds each line glyph's strokes (one for a single line, two for a double), each a
    /// start and end along the axis. Every stroke edge moves to a whole pixel, as little as it can while every stroke,
    /// and the gap inside every double line, stays at least a pixel wide so it doesn't fade or merge. Fonts share
    /// edges between their lines (a double line's inner edges are often the single line's outer ones), so each edge
    /// moves once, the same for every line. Strokes that reach the cell's edge, or are half the cell thick, aren't
    /// lines and are ignored.
    /// </summary>
    public static StrokeSnap Create(IEnumerable<IReadOnlyList<(float Start, float End)>> lines, int cellSize)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var strokesByLine = lines
            .Select(strokes => strokes.Where(stroke => IsLineStroke(stroke, cellSize)).OrderBy(stroke => stroke.Start)
                .ToList())
            .ToList();
        var edges = MergeEdges(strokesByLine.SelectMany(strokes => strokes)
            .SelectMany(stroke => new[] { stroke.Start, stroke.End }));

        var strokeWidths = new List<(int Low, int High)>();
        var gaps = new List<(int Low, int High)>();
        foreach (var strokes in strokesByLine)
        {
            for (var index = 0; index < strokes.Count; index++)
            {
                strokeWidths.Add((IndexOf(edges, strokes[index].Start), IndexOf(edges, strokes[index].End)));
                if (index > 0 && strokes[index].Start - strokes[index - 1].End > SamePositionTolerance)
                    gaps.Add((IndexOf(edges, strokes[index - 1].End), IndexOf(edges, strokes[index].Start)));
            }
        }

        // A cell too small for every gap gives up the gaps first: a double line drawn as one is better than none.
        var targets = Arrange(edges, [.. strokeWidths, .. gaps], cellSize) ?? Arrange(edges, strokeWidths, cellSize) ??
            edges.Select(RoundToPixel).ToArray();
        return new StrokeSnap([0, .. edges, cellSize], [0, .. targets, cellSize]);
    }

    /// <summary>Rounds to the nearest whole pixel, halves up, the same way in every glyph.</summary>
    public static float RoundToPixel(float position) => MathF.Floor(position + 0.5f);

    /// <summary>
    /// Where <paramref name="position"/> moves to. Positions outside the cell move with its nearest edge.
    /// </summary>
    public float Apply(float position)
    {
        if (position <= _from[0])
            return position - _from[0] + _to[0];

        if (position >= _from[^1])
            return position - _from[^1] + _to[^1];

        var index = Array.BinarySearch(_from, position);
        if (index >= 0)
            return _to[index];

        var upper = ~index;
        var lower = upper - 1;
        var share = (position - _from[lower]) / (_from[upper] - _from[lower]);
        return _to[lower] + share * (_to[upper] - _to[lower]);
    }

    private static bool IsLineStroke((float Start, float End) stroke, int cellSize)
    {
        var thickness = stroke.End - stroke.Start;
        return stroke.Start >= EdgeTolerance && stroke.End <= cellSize - EdgeTolerance && thickness > 0 &&
            thickness < cellSize / 2f;
    }

    // The distinct edges in increasing order, each strictly inside the cell.
    private static float[] MergeEdges(IEnumerable<float> positions)
    {
        var edges = new List<float>();
        foreach (var position in positions.Order())
        {
            if (edges.Count == 0 || position - edges[^1] > SamePositionTolerance)
                edges.Add(position);
        }

        return [.. edges];
    }

    private static int IndexOf(float[] edges, float position)
    {
        var nearest = 0;
        for (var index = 1; index < edges.Length; index++)
        {
            if (MathF.Abs(edges[index] - position) < MathF.Abs(edges[nearest] - position))
                nearest = index;
        }

        return nearest;
    }

    // Whole-pixel positions for the edges, in their order, that keep each pair in mustSeparate at least a pixel apart
    // and move the edges as little as possible (by the sum of squared distances); null when none can. Of equally good
    // arrangements, the one with the lowest positions wins, so the result never depends on anything else.
    private static float[]? Arrange(float[] edges, IReadOnlyList<(int Low, int High)> mustSeparate, int cellSize)
    {
        if (edges.Length > MostEdgesToArrange)
            return null;

        var positions = new int[edges.Length];
        int[]? best = null;
        var bestCost = float.MaxValue;
        Place(0, 0, 0);
        return best?.Select(position => (float)position).ToArray();

        void Place(int index, int lowestPosition, float cost)
        {
            if (cost >= bestCost)
                return;

            if (index == edges.Length)
            {
                bestCost = cost;
                best = (int[])positions.Clone();
                return;
            }

            var first = Math.Max(lowestPosition, (int)MathF.Floor(edges[index]) - ExtraPixelsToTry);
            var last = Math.Min(cellSize, (int)MathF.Ceiling(edges[index]) + ExtraPixelsToTry);
            for (var position = first; position <= last; position++)
            {
                positions[index] = position;
                if (!KeepsApart(index))
                    continue;

                var distance = position - edges[index];
                Place(index + 1, position, cost + distance * distance);
            }
        }

        // Every pair ending at this edge has both ends placed by now, as edges are placed in order.
        bool KeepsApart(int index)
        {
            foreach (var (low, high) in mustSeparate)
            {
                if (high == index && positions[high] - positions[low] < 1)
                    return false;
            }

            return true;
        }
    }
}
