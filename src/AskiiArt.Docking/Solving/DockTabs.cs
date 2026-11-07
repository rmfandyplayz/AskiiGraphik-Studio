// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Solving;

/// <summary>A tab's place along its tab strip.</summary>
/// <param name="Start">Where its left end is.</param>
/// <param name="End">Where its right end is.</param>
public readonly record struct TabSpan(double Start, double End)
{
    /// <summary>Halfway along it.</summary>
    public double Middle => (Start + End) / 2;
}

/// <summary>Where a dock group's tabs are along its tab strip, from what the view measured.</summary>
public static class DockTabs
{
    /// <summary>
    /// Each tab's place, left to right, with the strip scrolled as far as it is now. If any tab's length is unknown,
    /// they all share the strip equally, which is near enough for deciding where a dropped tab goes.
    /// </summary>
    /// <param name="windowIds">The group's windows, in tab order.</param>
    /// <param name="strip">The tab strip's rectangle.</param>
    /// <param name="measurements">The tab lengths and scroll offsets.</param>
    public static IReadOnlyList<TabSpan> Spans(IReadOnlyList<string> windowIds, LayoutRect strip,
        LayoutMeasurements measurements)
    {
        if (windowIds.Count == 0)
            return [];

        var allKnown = windowIds.All(measurements.TabLengths.ContainsKey);
        var offset = windowIds.Select(id => measurements.TabStripOffsets.GetValueOrDefault(id)).FirstOrDefault(value => value != 0);
        var spans = new List<TabSpan>(windowIds.Count);
        var start = strip.X - offset;

        foreach (var windowId in windowIds)
        {
            var length = allKnown ? measurements.TabLengths[windowId] : strip.Width / windowIds.Count;
            spans.Add(new TabSpan(start, start + length));
            start += length;
        }

        return spans;
    }
}
