// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>
/// Double-clicking a window's title: it squeezes to its title, or opens again to the length it had before, or to its
/// minimum if that isn't known. In a stack, the windows showing their contents give it the room, levelled from the
/// largest down, and none goes below its minimum. One window in a stack always keeps its contents, so that one stays.
/// </summary>
public static class TitleOnlyToggle
{
    // Halving the range this many times leaves the level far finer than a pixel, for any window that fits on a screen.
    private const int LevelSearchSteps = 60;

    /// <summary>The layout with the window squeezed to its title, or opened from it. Unchanged when it can't be.</summary>
    /// <param name="layout">The layout it's in.</param>
    /// <param name="windowId">The window.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public static DockingLayout Toggle(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        if (layout.FloatingWindowNamed(windowId) is { } floating)
        {
            var toggled = floating with { IsTitleOnly = !floating.IsTitleOnly };
            return layout with
            {
                FloatingWindows = [.. layout.FloatingWindows.Select(window => window == floating ? toggled : window)],
            };
        }

        var side = layout.Sides.FirstOrDefault(side => side.Columns.Any(column => Holds(column, windowId)));
        if (side is null)
            return layout;

        var column = side.Columns.First(column => Holds(column, windowId));
        var solved = LayoutSolver.Solve(layout, measurements, metrics);
        var places = column.Groups.Select(group => solved.PlaceOfWindow(group.WindowIds[0])!.Value).ToList();
        var minimums = column.Groups.Select(group => MinimumLength(group, measurements, metrics)).ToList();
        var index = column.Groups.ToList().FindIndex(group => group.WindowIds.Contains(windowId, StringComparer.Ordinal));

        var groups = column.Groups[index].IsTitleOnly
            ? Opened(column.Groups, index, places, minimums)
            : Squeezed(column.Groups, index, places);

        if (groups is null)
            return layout;

        var updated = side with
        {
            Columns = [.. side.Columns.Select(other => ReferenceEquals(other, column) ? other with { Groups = groups } : other)],
        };
        return layout with { Sides = [.. layout.Sides.Select(other => ReferenceEquals(other, side) ? updated : other)] };
    }

    private static List<WindowGroup>? Squeezed(IReadOnlyList<WindowGroup> groups, int index,
        List<(LayoutRect Bounds, bool IsTitleOnly)> places)
    {
        if (groups.Count(group => !group.IsTitleOnly) <= 1)
            return null;

        // A window the app window is too small for shows only its title already, so its length says nothing.
        var group = groups[index];
        var openLength = places[index].IsTitleOnly ? group.OpenLength : places[index].Bounds.Height;

        List<WindowGroup> result = [.. groups];
        result[index] = group with { IsTitleOnly = true, OpenLength = openLength };
        return result;
    }

    // The others' new lengths become their shares, in logical pixels, as after dragging a gap.
    private static List<WindowGroup>? Opened(IReadOnlyList<WindowGroup> groups, int index,
        List<(LayoutRect Bounds, bool IsTitleOnly)> places, List<double> minimums)
    {
        var givers = Enumerable.Range(0, groups.Count).Where(other => other != index && !places[other].IsTitleOnly).ToList();
        var spare = givers.Sum(other => Math.Max(0, places[other].Bounds.Height - minimums[other]));
        var current = places[index].Bounds.Height;

        if (current + spare < minimums[index])
            return null;

        var wanted = Math.Max(minimums[index], groups[index].OpenLength ?? minimums[index]);
        var taken = Math.Min(wanted - current, spare);
        var level = Level(givers.Select(other => (places[other].Bounds.Height, minimums[other])).ToList(), taken);

        List<WindowGroup> result = [.. groups];
        foreach (var other in givers)
        {
            var length = Math.Max(minimums[other], Math.Min(places[other].Bounds.Height, level));
            result[other] = result[other] with { Share = length };
        }

        result[index] = groups[index] with { IsTitleOnly = false, Share = current + taken };
        return result;
    }

    // The length to cut everything longer down to, never below its minimum, so that the cuts add up to what's needed.
    private static double Level(List<(double Length, double Minimum)> windows, double needed)
    {
        if (windows.Count == 0 || needed <= 0)
            return double.MaxValue;

        double Cut(double level) => windows.Sum(window => window.Length - Math.Max(window.Minimum, Math.Min(window.Length, level)));

        var (low, high) = (0.0, windows.Max(window => window.Length));
        for (var step = 0; step < LevelSearchSteps; step++)
        {
            var middle = (low + high) / 2;
            if (Cut(middle) > needed)
                low = middle;
            else
                high = middle;
        }

        return high;
    }

    private static bool Holds(SideColumn column, string windowId) =>
        column.Groups.Any(group => group.WindowIds.Contains(windowId, StringComparer.Ordinal));

    private static double MinimumLength(WindowGroup group, LayoutMeasurements measurements, DockingMetrics metrics) =>
        group.WindowIds
            .Select(id => measurements.MinimumWindowLengths.GetValueOrDefault(id, metrics.DefaultMinimumWindowLength))
            .DefaultIfEmpty(metrics.DefaultMinimumWindowLength)
            .Max();
}
