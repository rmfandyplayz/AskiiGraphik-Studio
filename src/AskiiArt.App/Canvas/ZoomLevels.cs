// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's discrete zoom steps (ARCHITECTURE §12). A zoom factor scales the document's font
/// size; the cell size is then rounded to whole device pixels by the font metrics, so cells never
/// leave seams at any step.
/// </summary>
internal static class ZoomLevels
{
    public const double ActualSize = 1;

    // Steps like 1/3 aren't exact in floating point, so "the same step" allows for rounding.
    private const double Tolerance = 1e-6;

    public static IReadOnlyList<double> Factors { get; } =
        [0.25, 1.0 / 3, 0.5, 2.0 / 3, 0.75, 1, 1.25, 1.5, 2, 3, 4, 6, 8];

    public static double Minimum => Factors[0];

    public static double Maximum => Factors[^1];

    /// <summary>The next step above <paramref name="factor"/>, or the largest step.</summary>
    public static double StepIn(double factor)
    {
        foreach (var step in Factors)
        {
            if (step > factor + Tolerance)
                return step;
        }

        return Maximum;
    }

    /// <summary>The next step below <paramref name="factor"/>, or the smallest step.</summary>
    public static double StepOut(double factor)
    {
        for (var index = Factors.Count - 1; index >= 0; index--)
        {
            if (Factors[index] < factor - Tolerance)
                return Factors[index];
        }

        return Minimum;
    }
}
