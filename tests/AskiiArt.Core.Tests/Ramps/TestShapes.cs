// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

/// <summary>Glyph shapes and cell targets for tests, made in a few simple patterns.</summary>
internal static class TestShapes
{
    /// <summary>A shape whose top half of rows has <paramref name="top"/> ink and bottom half <paramref name="bottom"/>.
    /// </summary>
    public static GlyphShape Bands(double top, double bottom) => new(BandParts(top, bottom));

    /// <summary>
    /// A shape whose left half of columns has <paramref name="left"/> ink and right half <paramref name="right"/>.
    /// </summary>
    public static GlyphShape Sides(double left, double right) => new(SideParts(left, right));

    /// <summary>A target, or part weights, with <paramref name="top"/> in the top half and <paramref name="bottom"/>
    /// in the bottom half.</summary>
    public static double[] BandParts(double top, double bottom) =>
        Parts((_, row) => row < GlyphShape.Rows / 2 ? top : bottom);

    /// <summary>A target, or part weights, with <paramref name="left"/> in the left half and
    /// <paramref name="right"/> in the right half.</summary>
    public static double[] SideParts(double left, double right) =>
        Parts((column, _) => column < GlyphShape.Columns / 2 ? left : right);

    /// <summary>A target, or part weights, with every part at <paramref name="value"/>.</summary>
    public static double[] EvenParts(double value) => Parts((_, _) => value);

    private static double[] Parts(Func<int, int, double> valueAt)
    {
        var parts = new double[GlyphShape.PartCount];
        for (var row = 0; row < GlyphShape.Rows; row++)
        {
            for (var column = 0; column < GlyphShape.Columns; column++)
                parts[row * GlyphShape.Columns + column] = valueAt(column, row);
        }

        return parts;
    }
}
