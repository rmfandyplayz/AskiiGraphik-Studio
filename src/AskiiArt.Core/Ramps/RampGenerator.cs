// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// Makes a ramp from candidate characters by measuring them in a font (ARCHITECTURE §7, ADR 0052): of the candidates,
/// it picks the ones whose darkness is most evenly spread from no ink up to the darkest candidate.
/// </summary>
public static class RampGenerator
{
    /// <summary>
    /// The printable ASCII characters, without the space: the candidates the Ramps window starts with.
    /// </summary>
    public static string PrintableAscii { get; } =
        string.Concat(Enumerable.Range('!', '~' - '!' + 1).Select(code => (char)code));

    /// <summary>
    /// A ramp of up to <paramref name="count"/> of <paramref name="candidates"/>, as evenly spread in darkness as the
    /// candidates allow, ending with the darkest. Level k of n should look k / n as dark as the darkest, so the
    /// picks are those nearest those targets, judged by the sum of squared differences. Spaces, repeats, characters
    /// that can't fill a cell, and characters with no ink are left out first.
    /// </summary>
    /// <param name="candidates">The characters to pick from.</param>
    /// <param name="count">How many to pick, at least 1. Fewer usable candidates than that are all picked.</param>
    /// <param name="densities">How dark each character looks in the font.</param>
    /// <param name="zeroLevel">What the ramp paints at intensity 0.</param>
    /// <returns>The ramp, or null when no candidate is usable.</returns>
    public static Ramp? Generate(string candidates, int count, IGlyphDensityProvider densities,
        RampZeroLevel zeroLevel = RampZeroLevel.Empty)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(densities);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var usable = UsableCandidates(candidates, densities);
        if (usable.Length == 0)
            return null;

        var picks = Pick(usable, Math.Min(count, usable.Length));
        return new Ramp(picks.Select(index => usable[index].Glyph), zeroLevel);
    }

    // Lightest first. Characters that measure the same keep the order they were given in.
    private static (Rune Glyph, double Density)[] UsableCandidates(string candidates,
        IGlyphDensityProvider densities)
    {
        var seen = new HashSet<Rune>();
        var usable = new List<(Rune Glyph, double Density)>();
        foreach (var glyph in candidates.EnumerateRunes())
        {
            if (glyph.Value == ' ' || !CharacterSet.Unicode.Allows(glyph) || !seen.Add(glyph))
                continue;

            var density = densities.GetDensity(glyph);
            if (density > 0)
                usable.Add((glyph, density));
        }

        return [.. usable.OrderBy(candidate => candidate.Density)];
    }

    // Dynamic programming over the candidates in order of darkness: cost[j, i] is the least total error of picks for
    // levels 1 to j + 1 with candidate i at level j + 1. Picking in order keeps the ramp lightest first, and the last
    // pick is always the darkest candidate. O(levels × candidates).
    private static int[] Pick((Rune Glyph, double Density)[] usable, int levels)
    {
        var candidateCount = usable.Length;
        var darkest = usable[^1].Density;
        var cost = new double[levels, candidateCount];
        var previous = new int[levels, candidateCount];

        for (var level = 0; level < levels; level++)
        {
            var target = darkest * (level + 1) / levels;
            var bestBefore = double.PositiveInfinity;
            var bestBeforeIndex = -1;
            for (var index = 0; index < candidateCount; index++)
            {
                var error = usable[index].Density - target;
                if (level == 0)
                {
                    cost[level, index] = error * error;
                }
                else
                {
                    cost[level, index] = bestBefore + error * error;
                    previous[level, index] = bestBeforeIndex;
                }

                // Only candidates before the next one can come before it.
                if (level > 0 && cost[level - 1, index] < bestBefore)
                {
                    bestBefore = cost[level - 1, index];
                    bestBeforeIndex = index;
                }
            }
        }

        var picks = new int[levels];
        picks[levels - 1] = candidateCount - 1;
        for (var level = levels - 1; level > 0; level--)
            picks[level - 1] = previous[level, picks[level]];

        return picks;
    }
}
