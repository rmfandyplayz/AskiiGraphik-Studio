// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// How strongly a soft tip paints each cell of its footprint (ADR 0049), from 0 to 1. The middle paints fully, and the
/// outer part of the radius fades towards the edge: none of it at 0 softness, so the tip is hard, and all of it at 1,
/// so it fades from the middle. The fade follows the shape as measured on the footprint, so a perceptual circle fades
/// round on screen and a square fades evenly towards its sides.
/// <para>
/// Where the weight changes across a cell, as at a circle's edge or across a steep fade, the cell also has a weight
/// for each <see cref="GlyphShape"/> part, so the Brush and the Soft Eraser can pick a glyph whose ink lies where the
/// tip paints most (ADR 0068). A tip with no middle cell, one with all four neighbours in the footprint, has no edge to
/// shape, so its cells paint evenly, and so do the cells the footprint's middle touches, so the middle still paints
/// fully. Immutable.
/// </para>
/// </summary>
public sealed class BrushFalloff
{
    /// <summary>
    /// How far apart the lightest and heaviest parts of a cell may weigh, and the cell still paint evenly. Below it the
    /// difference couldn't change the glyph, and the cell keeps its weight as a whole.
    /// </summary>
    public const double EvenSpread = 0.05;

    // How many points across and down each part is sampled at.
    private const int SamplesPerSide = 2;

    private readonly double[] _weights;

    // The weights of each covered cell's parts, by cell, or null for a cell that paints evenly.
    private readonly float[]?[] _parts;

    // The weight of each cell's heaviest part, or its weight where it paints evenly.
    private readonly float[] _peaks;

    private BrushFalloff(BrushFootprint footprint, double softness, bool shapesEdges, double[] weights,
        float[]?[] parts, float[] peaks)
    {
        Footprint = footprint;
        Softness = softness;
        ShapesEdges = shapesEdges;
        _weights = weights;
        _parts = parts;
        _peaks = peaks;
    }

    /// <summary>The footprint this weighs.</summary>
    public BrushFootprint Footprint { get; }

    /// <summary>How much of the radius fades, from 0 to 1.</summary>
    public double Softness { get; }

    /// <summary>
    /// Whether edge cells were weighed part by part. Only step 4.10a's comparison of the brush before step 4.7 turns it
    /// off (ADR 0074).
    /// </summary>
    public bool ShapesEdges { get; }

    /// <summary>True when some cell's parts weigh differently, so the tip has an edge to shape.</summary>
    public bool HasShapedCells => _parts.Any(parts => parts is not null);

    /// <summary>The weights of <paramref name="footprint"/>'s cells with <paramref name="softness"/>.</summary>
    /// <param name="footprint">The footprint.</param>
    /// <param name="softness">How much of the radius fades, from 0 (a hard tip) to 1.</param>
    /// <param name="shapesEdges">False to weigh every cell as a whole at its middle, as before step 4.7.</param>
    public static BrushFalloff Create(BrushFootprint footprint, double softness, bool shapesEdges = true)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        if (double.IsNaN(softness) || softness < 0 || softness > 1)
            throw new ArgumentOutOfRangeException(nameof(softness), softness, "Must be from 0 to 1.");

        var width = footprint.Width;
        var height = footprint.Height;
        var weights = new double[width * height];
        var parts = new float[]?[width * height];
        var peaks = new float[width * height];
        var shapesCells = shapesEdges && HasMiddle(footprint);
        for (var row = 0; row < height; row++)
        {
            var cells = footprint.Rows[row];
            for (var column = cells.Start; column < cells.End; column++)
            {
                var index = row * width + column;
                var distance = DistanceFromMiddle(footprint.Tip.Shape, width, height, column + 0.5, row + 0.5);
                weights[index] = FadeWeight(distance, softness);
                if (shapesCells && !HoldsMiddle(width, height, column, row))
                    parts[index] = PartWeightsOf(footprint, softness, column, row);

                peaks[index] = parts[index]?.Max() ?? (float)weights[index];
            }
        }

        return new BrushFalloff(footprint, softness, shapesEdges, weights, parts, peaks);
    }

    /// <summary>
    /// The weight of the cell at (<paramref name="column"/>, <paramref name="row"/>) relative to the footprint's
    /// top-left, or 0 for a cell the footprint doesn't cover. It's the weight at the cell's middle.
    /// </summary>
    public double WeightAt(int column, int row) => Footprint.Contains(column, row)
        ? _weights[row * Footprint.Width + column]
        : 0;

    /// <summary>
    /// The weights of the parts of the cell at (<paramref name="column"/>, <paramref name="row"/>), one for each
    /// <see cref="GlyphShape"/> part, in its order; empty for a cell that paints evenly with <see cref="WeightAt"/>,
    /// or one the footprint doesn't cover.
    /// </summary>
    public ReadOnlySpan<float> PartWeightsAt(int column, int row) => Footprint.Contains(column, row)
        ? _parts[row * Footprint.Width + column]
        : [];

    /// <summary>
    /// The weight of the heaviest part of the cell at (<paramref name="column"/>, <paramref name="row"/>), or its
    /// <see cref="WeightAt"/> when it paints evenly; 0 for a cell the footprint doesn't cover.
    /// </summary>
    public float PeakWeightAt(int column, int row) => Footprint.Contains(column, row)
        ? _peaks[row * Footprint.Width + column]
        : 0;

    // A footprint with no cell whose four neighbours are all in it is all edge, so shaping it would only lighten it:
    // a one-cell tip would paint a lighter glyph in the middle of its cell.
    private static bool HasMiddle(BrushFootprint footprint)
    {
        for (var row = 1; row < footprint.Height - 1; row++)
        {
            var cells = footprint.Rows[row];
            for (var column = cells.Start; column < cells.End; column++)
            {
                var isSurrounded = footprint.Contains(column - 1, row) && footprint.Contains(column + 1, row) &&
                    footprint.Contains(column, row - 1) && footprint.Contains(column, row + 1);
                if (isSurrounded)
                    return true;
            }
        }

        return false;
    }

    // The middle paints fully (ADR 0049), but the average over a small fully soft tip's middle cell is well short of
    // full, so the cell or cells the middle touches keep the weight at their own middle.
    private static bool HoldsMiddle(int width, int height, int column, int row) =>
        column * 2 <= width && width <= (column + 1) * 2 && row * 2 <= height && height <= (row + 1) * 2;

    // The weight of each part, as the average of points spread over it, or null when the parts weigh too nearly the
    // same to matter. A cell whose corners are all inside the shape's fully weighted middle is weighted fully all
    // over, since both are convex, so it needs no sampling.
    private static float[]? PartWeightsOf(BrushFootprint footprint, double softness, int column, int row)
    {
        if (IsFull(footprint, softness, column, column + 1.0, row, row + 1.0))
            return null;

        var parts = new float[GlyphShape.PartCount];
        var lightest = double.PositiveInfinity;
        var heaviest = double.NegativeInfinity;
        const double partWidth = 1.0 / GlyphShape.Columns;
        const double partHeight = 1.0 / GlyphShape.Rows;
        for (var partRow = 0; partRow < GlyphShape.Rows; partRow++)
        {
            for (var partColumn = 0; partColumn < GlyphShape.Columns; partColumn++)
            {
                var total = 0.0;
                for (var sampleY = 0; sampleY < SamplesPerSide; sampleY++)
                {
                    for (var sampleX = 0; sampleX < SamplesPerSide; sampleX++)
                    {
                        var x = column + (partColumn + (sampleX + 0.5) / SamplesPerSide) * partWidth;
                        var y = row + (partRow + (sampleY + 0.5) / SamplesPerSide) * partHeight;
                        total += WeightAtPoint(footprint, softness, x, y);
                    }
                }

                var weight = total / (SamplesPerSide * SamplesPerSide);
                parts[partRow * GlyphShape.Columns + partColumn] = (float)weight;
                lightest = Math.Min(lightest, weight);
                heaviest = Math.Max(heaviest, weight);
            }
        }

        return heaviest - lightest <= EvenSpread ? null : parts;
    }

    private static bool IsFull(BrushFootprint footprint, double softness, double left, double right, double top,
        double bottom) =>
        WeightAtPoint(footprint, softness, left, top) >= 1 && WeightAtPoint(footprint, softness, right, top) >= 1 &&
        WeightAtPoint(footprint, softness, left, bottom) >= 1 && WeightAtPoint(footprint, softness, right, bottom) >= 1;

    // The weight at a point in cells from the footprint's top-left: none outside the shape.
    private static double WeightAtPoint(BrushFootprint footprint, double softness, double x, double y)
    {
        if (!footprint.ShapeContains(x, y))
            return 0;

        var distance = DistanceFromMiddle(footprint.Tip.Shape, footprint.Width, footprint.Height, x, y);
        return FadeWeight(distance, softness);
    }

    // From 0 in the middle to 1 at the edge, with each axis scaled to the footprint's half size, so the measure has
    // the footprint's own proportions: round for a circle, and towards the nearest side for a square.
    private static double DistanceFromMiddle(BrushShape shape, int width, int height, double x, double y)
    {
        var across = Math.Abs(x - width / 2.0) / (width / 2.0);
        var down = Math.Abs(y - height / 2.0) / (height / 2.0);
        var distance = shape == BrushShape.Square ? Math.Max(across, down) : Math.Sqrt(across * across + down * down);
        return Math.Min(distance, 1);
    }

    // Full inside the hard part, then an S-shaped fade to nothing at the edge, so the soft part has no visible step
    // where it starts.
    private static double FadeWeight(double distance, double softness)
    {
        var fadeStart = 1 - softness;
        if (distance <= fadeStart)
            return 1;

        var along = (distance - fadeStart) / softness;
        return 1 - along * along * (3 - 2 * along);
    }
}
