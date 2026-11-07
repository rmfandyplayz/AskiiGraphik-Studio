// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>
/// The size a window gets when it comes off a side to float, by a drag or from its menu (docs/DOCKING.md §5.2). A window
/// alone in its column fills the app window's height, which would make a clumsy floating window, so that one goes back to
/// the size it last floated at, or half its height if it's never floated.
/// </summary>
public static class WindowFloating
{
    /// <summary>Whether its group is the only one in its column, so that it fills the side's height.</summary>
    public static bool FillsItsColumn(DockingLayout layout, string windowId) =>
        layout.PlaceOf(windowId) is { } place && layout.Side(place.Edge)!.Columns[place.Column].Groups.Count == 1;

    /// <summary>The size it floats at, for a window that's open on a side or already floating.</summary>
    /// <param name="layout">The layout it's in.</param>
    /// <param name="windowId">The window.</param>
    /// <param name="bounds">Where it is now.</param>
    /// <param name="isTitleOnly">Whether it shows only its title now.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public static LayoutSize SizeWhenFloated(DockingLayout layout, string windowId, LayoutRect bounds, bool isTitleOnly,
        LayoutMeasurements measurements, DockingMetrics metrics)
    {
        if (layout.FloatingWindowNamed(windowId) is { } floating)
            return new LayoutSize(floating.Width, floating.Height);

        var minimum = measurements.MinimumWindowLengths.GetValueOrDefault(windowId, metrics.DefaultMinimumWindowLength);

        if (FillsItsColumn(layout, windowId))
        {
            return layout.FloatingSizeOf(windowId) is { } remembered
                ? new LayoutSize(remembered.Width, remembered.Height)
                : new LayoutSize(bounds.Width, Math.Max(minimum, bounds.Height / 2));
        }

        // A title-only window that lands gets a normal size.
        return isTitleOnly
            ? new LayoutSize(bounds.Width, Math.Max(metrics.DefaultFloatingWindowHeight, minimum))
            : new LayoutSize(bounds.Width, bounds.Height);
    }

    /// <summary>The width the user asked for: a floating window's own, or its side's. Null if it isn't open.</summary>
    public static double? WantedWidth(DockingLayout layout, string windowId)
    {
        if (layout.FloatingWindowNamed(windowId) is { } floating)
            return floating.Width;

        return layout.Sides.SelectMany(side => side.Columns)
            .FirstOrDefault(column => column.Groups.Any(group => group.WindowIds.Contains(windowId, StringComparer.Ordinal)))
            ?.Width;
    }

    /// <summary>
    /// Floats a window from its menu. It moves a little way in from where it was and down, so that it visibly comes off,
    /// keeping the edge it had nearest the app window's. Does nothing if it's floating already, or isn't open.
    /// </summary>
    /// <param name="layout">The layout it's in.</param>
    /// <param name="windowId">The window.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    /// <param name="offset">How far in and down it moves.</param>
    public static DockingLayout FromMenu(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics, double offset)
    {
        if (layout.FloatingWindowNamed(windowId) is not null ||
            LayoutSolver.Solve(layout, measurements, metrics).PlaceOfWindow(windowId) is not { } place)
        {
            return layout;
        }

        var size = SizeWhenFloated(layout, windowId, place.Bounds, place.IsTitleOnly, measurements, metrics);
        var isOnTheLeft = layout.Side(SideEdge.Left)?.Columns.SelectMany(column => column.Groups)
            .Any(group => group.WindowIds.Contains(windowId, StringComparer.Ordinal)) == true;
        var wanted = new LayoutPoint(
            isOnTheLeft ? place.Bounds.X + offset : place.Bounds.Right - offset - size.Width,
            place.Bounds.Y + offset);

        var at = LayoutSolver.KeptInside(wanted, size, LayoutSolver.FloatingWindowArea(measurements.Available, metrics));
        return LayoutEdits.FloatWindow(layout, windowId, at, size.Width, size.Height);
    }
}
