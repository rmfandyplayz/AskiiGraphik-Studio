// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>
/// Picks the level of a ramp whose glyph best shows a cell whose parts should be unevenly dark (ARCHITECTURE §7,
/// ADR 0068): the PATS matching (perceptually accurate text-based shapes), which M6's image conversion builds on.
/// <para>
/// The target is an intensity for each <see cref="GlyphShape"/> part, from 0 to 1. A level is judged by two things:
/// how far its intensity is from the target's average, which keeps the ramp in charge of how dark things get, and how
/// much of the way the target strays from its average the glyph's ink can show. A glyph shows its ink's pattern scaled
/// down as far as fits best, never up, so a glyph is never blamed for being bolder than a faint edge, and one whose
/// ink lies the wrong way shows nothing of the edge. An even target has nothing to show, so it always gets the level
/// nearest its intensity, as without matching.
/// </para>
/// </summary>
public sealed class RampShapeMatcher
{
    // How much more where the ink lies counts than how dark the level is, part for part. Tuned by eye on the bundled
    // fonts' Fine and Standard ramps: at 1, a faint edge cell keeps a glyph whose ink is on the wrong side, because a
    // level's worth of darkness outweighs it.
    private const double ShapeWeight = 4;

    private readonly double[] _intensities;

    // Each level's ink, part by part, less its intensity: how the glyph strays from an even spread of its darkness.
    private readonly double[][] _strays;
    private readonly double[] _strayWeights;

    /// <summary>
    /// Creates a matcher for <paramref name="ramp"/>'s glyphs, shaped as <paramref name="glyphs"/> measures them.
    /// </summary>
    public RampShapeMatcher(Ramp ramp, IGlyphDensityProvider glyphs)
    {
        ArgumentNullException.ThrowIfNull(ramp);
        ArgumentNullException.ThrowIfNull(glyphs);

        Ramp = ramp;
        var levels = ramp.TopLevel + 1;
        _intensities = new double[levels];
        _strays = new double[levels][];
        _strayWeights = new double[levels];
        for (var level = 0; level < levels; level++)
        {
            _intensities[level] = ramp.IntensityOf(level);
            var shape = ramp.GlyphOf(level) is { } glyph ? glyphs.GetShape(glyph) : GlyphShape.Empty;
            _strays[level] = StraysOf(shape, _intensities[level]);
            _strayWeights[level] = _strays[level].Sum(stray => stray * stray);
        }
    }

    /// <summary>The ramp whose levels this picks.</summary>
    public Ramp Ramp { get; }

    /// <summary>The level that best shows <paramref name="target"/>, from any of the ramp's levels.</summary>
    /// <param name="target">The intensity each part of the cell should have, from 0 to 1.</param>
    public int LevelFor(ReadOnlySpan<double> target) => LevelFor(target, 0, Ramp.TopLevel);

    /// <summary>
    /// The level from <paramref name="lowest"/> to <paramref name="highest"/> that best shows
    /// <paramref name="target"/>. Where two match equally, the darker wins, as <see cref="Ramp.LevelAt"/> rounds.
    /// </summary>
    /// <param name="target">The intensity each part of the cell should have, from 0 to 1.</param>
    /// <param name="lowest">The lightest level allowed.</param>
    /// <param name="highest">The darkest level allowed.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="target"/> doesn't have <see cref="GlyphShape.PartCount"/> parts, or the levels are out of order
    /// or outside the ramp.
    /// </exception>
    public int LevelFor(ReadOnlySpan<double> target, int lowest, int highest)
    {
        if (target.Length != GlyphShape.PartCount)
        {
            throw new ArgumentException($"A target has {GlyphShape.PartCount} parts, not {target.Length}.",
                nameof(target));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(lowest);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(highest, Ramp.TopLevel);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lowest, highest);

        if (lowest == highest)
            return lowest;

        var mean = Average(target);
        Span<double> targetStrays = stackalloc double[GlyphShape.PartCount];
        target.CopyTo(targetStrays);
        Blur(targetStrays);
        var blurredMean = Average(targetStrays);
        var targetStrayWeight = 0.0;
        for (var part = 0; part < targetStrays.Length; part++)
        {
            targetStrays[part] -= blurredMean;
            targetStrayWeight += targetStrays[part] * targetStrays[part];
        }

        // A level scores at least its tone gap's share, so the search starts at the level nearest in tone and stops,
        // each way, at the first level whose tone gap alone scores worse than the best so far: those beyond it are
        // further off still.
        var nearest = Math.Clamp(Ramp.LevelAt(mean), lowest, highest);
        var best = nearest;
        var bestScore = ScoreOf(nearest, mean, targetStrays, targetStrayWeight);
        for (var level = nearest + 1; level <= highest && ToneScoreOf(level, mean) <= bestScore; level++)
            Consider(level, ScoreOf(level, mean, targetStrays, targetStrayWeight), ref best, ref bestScore);

        for (var level = nearest - 1; level >= lowest && ToneScoreOf(level, mean) <= bestScore; level--)
            Consider(level, ScoreOf(level, mean, targetStrays, targetStrayWeight), ref best, ref bestScore);

        return best;
    }

    // Where two levels score the same, the darker wins.
    private static void Consider(int level, double score, ref int best, ref double bestScore)
    {
        var isBetter = level > best ? score <= bestScore : score < bestScore;
        if (!isBetter)
            return;

        best = level;
        bestScore = score;
    }

    private double ScoreOf(int level, double mean, ReadOnlySpan<double> targetStrays, double targetStrayWeight) =>
        ToneScoreOf(level, mean) +
        ShapeWeight * UnshownStray(_strays[level], _strayWeights[level], targetStrays, targetStrayWeight);

    private double ToneScoreOf(int level, double mean)
    {
        var toneGap = _intensities[level] - mean;
        return GlyphShape.PartCount * toneGap * toneGap;
    }

    // How much of the target's stray from its average is left once the glyph's strays, scaled down as far as fits
    // best, are taken away from it. It can't be below 0, but rounding could take it there, and the search relies on it.
    private static double UnshownStray(double[] strays, double strayWeight, ReadOnlySpan<double> targetStrays,
        double targetStrayWeight)
    {
        if (strayWeight <= 0)
            return targetStrayWeight;

        var agreement = 0.0;
        for (var part = 0; part < strays.Length; part++)
            agreement += strays[part] * targetStrays[part];

        var scale = Math.Clamp(agreement / strayWeight, 0, 1);
        return Math.Max(targetStrayWeight - 2 * scale * agreement + scale * scale * strayWeight, 0);
    }

    // The glyph's ink spread as its blurred shape spreads it, scaled to average the level's intensity, less that
    // intensity.
    private static double[] StraysOf(GlyphShape shape, double intensity)
    {
        var strays = new double[GlyphShape.PartCount];
        if (shape.Ink <= 0)
            return strays;

        Span<double> parts = stackalloc double[GlyphShape.PartCount];
        shape.Parts.CopyTo(parts);
        Blur(parts);
        var ink = Average(parts);
        for (var part = 0; part < strays.Length; part++)
            strays[part] = intensity * (parts[part] / ink - 1);

        return strays;
    }

    private static double Average(ReadOnlySpan<double> parts)
    {
        var total = 0.0;
        foreach (var part in parts)
            total += part;

        return total / parts.Length;
    }

    // Softens each part with its neighbours, weighing them 1, 2, 1 across and then down, as the eye blurs thin
    // strokes; at the cell's sides a part counts as its own neighbour. Without it, matching follows which part a
    // glyph's thin stroke happens to fall in, and picks a few glyphs over and over.
    private static void Blur(Span<double> parts)
    {
        Span<double> before = stackalloc double[GlyphShape.PartCount];
        parts.CopyTo(before);
        for (var row = 0; row < GlyphShape.Rows; row++)
        {
            for (var column = 0; column < GlyphShape.Columns; column++)
            {
                var left = before[IndexOf(Math.Max(column - 1, 0), row)];
                var right = before[IndexOf(Math.Min(column + 1, GlyphShape.Columns - 1), row)];
                parts[IndexOf(column, row)] = (left + 2 * before[IndexOf(column, row)] + right) / 4;
            }
        }

        parts.CopyTo(before);
        for (var row = 0; row < GlyphShape.Rows; row++)
        {
            for (var column = 0; column < GlyphShape.Columns; column++)
            {
                var up = before[IndexOf(column, Math.Max(row - 1, 0))];
                var down = before[IndexOf(column, Math.Min(row + 1, GlyphShape.Rows - 1))];
                parts[IndexOf(column, row)] = (up + 2 * before[IndexOf(column, row)] + down) / 4;
            }
        }
    }

    private static int IndexOf(int column, int row) => row * GlyphShape.Columns + column;
}
