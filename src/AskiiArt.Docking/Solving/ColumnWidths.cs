// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Solving;

/// <summary>
/// How wide each column is when the app window is too narrow for them all (docs/DOCKING.md §7). They shrink together,
/// in proportion, but none below the narrowest a window can be. Once every column is that narrow, the canvas region gives
/// way instead. The widths the user asked for stay in the layout, so regrowing the app window brings them back.
/// </summary>
internal static class ColumnWidths
{
    /// <summary>The widths to use, in the same order as <paramref name="wanted"/>.</summary>
    /// <param name="wanted">The widths the user asked for.</param>
    /// <param name="room">How much width the columns have between them.</param>
    /// <param name="minimum">The narrowest a column can be. One the user asked to be narrower keeps its own width.</param>
    public static double[] Fit(IReadOnlyList<double> wanted, double room, double minimum)
    {
        var widths = wanted.ToArray();
        if (widths.Sum() <= room)
            return widths;

        var floors = wanted.Select(width => Math.Min(width, minimum)).ToArray();
        if (floors.Sum() >= room)
            return floors;

        // Shrink the columns still above their floors by one scale. Any that would go below are held at their floor, and
        // the rest share what's left, until none would.
        var held = new bool[widths.Length];
        while (true)
        {
            var heldWidth = 0.0;
            var freeWidth = 0.0;
            for (var index = 0; index < widths.Length; index++)
            {
                if (held[index])
                    heldWidth += floors[index];
                else
                    freeWidth += wanted[index];
            }

            var scale = freeWidth > 0 ? (room - heldWidth) / freeWidth : 1;
            var heldMore = false;
            for (var index = 0; index < widths.Length; index++)
            {
                if (held[index] || wanted[index] * scale >= floors[index])
                    continue;

                held[index] = true;
                heldMore = true;
            }

            if (heldMore)
                continue;

            for (var index = 0; index < widths.Length; index++)
                widths[index] = held[index] ? floors[index] : wanted[index] * scale;

            return widths;
        }
    }
}
