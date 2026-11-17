// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>
/// How strongly a soft tip paints each cell of its footprint (ADR 0049), from 0 to 1. The middle paints fully, and the
/// outer part of the radius fades towards the edge: none of it at 0 softness, so the tip is hard, and all of it at 1,
/// so it fades from the middle. The fade follows the shape as measured on the footprint, so a perceptual circle fades
/// round on screen and a square fades evenly towards its sides. Immutable.
/// </summary>
public sealed class BrushFalloff
{
    private readonly double[] _weights;

    private BrushFalloff(BrushFootprint footprint, double softness, double[] weights)
    {
        Footprint = footprint;
        Softness = softness;
        _weights = weights;
    }

    /// <summary>The footprint this weighs.</summary>
    public BrushFootprint Footprint { get; }

    /// <summary>How much of the radius fades, from 0 to 1.</summary>
    public double Softness { get; }

    /// <summary>The weights of <paramref name="footprint"/>'s cells with <paramref name="softness"/>.</summary>
    /// <param name="footprint">The footprint.</param>
    /// <param name="softness">How much of the radius fades, from 0 (a hard tip) to 1.</param>
    public static BrushFalloff Create(BrushFootprint footprint, double softness)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        if (double.IsNaN(softness) || softness < 0 || softness > 1)
            throw new ArgumentOutOfRangeException(nameof(softness), softness, "Must be from 0 to 1.");

        var width = footprint.Width;
        var height = footprint.Height;
        var weights = new double[width * height];
        for (var row = 0; row < height; row++)
        {
            var cells = footprint.Rows[row];
            for (var column = cells.Start; column < cells.End; column++)
            {
                var distance = DistanceFromMiddle(footprint.Tip.Shape, width, height, column, row);
                weights[row * width + column] = WeightAt(distance, softness);
            }
        }

        return new BrushFalloff(footprint, softness, weights);
    }

    /// <summary>
    /// The weight of the cell at (<paramref name="column"/>, <paramref name="row"/>) relative to the footprint's
    /// top-left, or 0 for a cell the footprint doesn't cover.
    /// </summary>
    public double WeightAt(int column, int row) => Footprint.Contains(column, row)
        ? _weights[row * Footprint.Width + column]
        : 0;

    // From 0 in the middle to 1 at the edge, with each axis scaled to the footprint's half size, so the measure has
    // the footprint's own proportions: round for a circle, and towards the nearest side for a square.
    private static double DistanceFromMiddle(BrushShape shape, int width, int height, int column, int row)
    {
        var x = Math.Abs(column + 0.5 - width / 2.0) / (width / 2.0);
        var y = Math.Abs(row + 0.5 - height / 2.0) / (height / 2.0);
        var distance = shape == BrushShape.Square ? Math.Max(x, y) : Math.Sqrt(x * x + y * y);
        return Math.Min(distance, 1);
    }

    // Full inside the hard part, then an S-shaped fade to nothing at the edge, so the soft part has no visible step
    // where it starts.
    private static double WeightAt(double distance, double softness)
    {
        var fadeStart = 1 - softness;
        if (distance <= fadeStart)
            return 1;

        var along = (distance - fadeStart) / softness;
        return 1 - along * along * (3 - 2 * along);
    }
}
