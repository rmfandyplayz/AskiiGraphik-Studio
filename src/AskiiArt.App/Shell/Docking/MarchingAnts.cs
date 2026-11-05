// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Docking;

/// <summary>A pattern of dashes that goes around a closed line, and can run along it.</summary>
/// <param name="Dash">How long each dash is.</param>
/// <param name="Gap">How long each gap is.</param>
internal readonly record struct MarchingAnts(double Dash, double Gap)
{
    /// <summary>How long one dash and one gap are together.</summary>
    public double Period => Dash + Gap;

    /// <summary>
    /// The pattern nearest to the one asked for that fits a whole number of times around a line this long. Otherwise
    /// you'd see a seam where the line ends and starts, with a dash cut short or two run together, and the ants would
    /// jump as they crossed it.
    /// </summary>
    public static MarchingAnts Around(double lineLength, double dash, double gap)
    {
        var period = dash + gap;
        if (lineLength <= 0 || period <= 0)
            return new MarchingAnts(dash, gap);

        var times = Math.Max(1, Math.Round(lineLength / period));
        var stretch = lineLength / (times * period);

        return new MarchingAnts(dash * stretch, gap * stretch);
    }

    /// <summary>
    /// The offset into the pattern for a line to start at, once the dashes have run some distance. It falls as they
    /// run, because a larger offset moves dashes backwards along the line.
    /// </summary>
    public double OffsetAfter(double distanceRun)
    {
        if (Period <= 0)
            return 0;

        var alongThePattern = distanceRun % Period;
        return alongThePattern > 0 ? Period - alongThePattern : 0;
    }
}
