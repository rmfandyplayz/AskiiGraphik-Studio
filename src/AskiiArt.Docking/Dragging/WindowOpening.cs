// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>
/// Opening a window from the Window menu (docs/DOCKING.md §6). Rules 1 to 4 are <see cref="LayoutEdits.OpenWindow"/>.
/// Rule 5, floating when nothing else can take it, is here, because where it floats depends on the app window's size.
/// </summary>
public static class WindowOpening
{
    /// <summary>
    /// Opens a window by the opening rules. With nowhere else to go it floats, centred on the canvas region, at the size
    /// it last floated at, or else a side's default width and half the app window's height. If another floating window
    /// is already exactly there, it moves down and right by <paramref name="cascadeOffset"/>, so that it doesn't hide
    /// that one.
    /// </summary>
    /// <param name="layout">The layout to open it in.</param>
    /// <param name="windowId">The window.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    /// <param name="cascadeOffset">How far down and right it moves to clear another floating window.</param>
    public static DockingLayout Open(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics, double cascadeOffset)
    {
        var opened = LayoutEdits.OpenWindow(layout, windowId, metrics.DefaultWindowWidth);
        if (opened.IsWindowOpen(windowId))
            return opened;

        var size = FloatingSize(layout, windowId, measurements, metrics);
        var area = LayoutSolver.FloatingWindowArea(measurements.Available, metrics);
        var region = LayoutSolver.Solve(layout, measurements, metrics).CanvasRegion;
        var centred = new LayoutPoint(
            region.X + (region.Width - size.Width) / 2,
            region.Y + (region.Height - size.Height) / 2);

        var at = LayoutSolver.KeptInside(centred, size, area);
        for (var tries = 0; tries < layout.FloatingWindows.Count && IsTaken(layout, at); tries++)
            at = LayoutSolver.KeptInside(new LayoutPoint(at.X + cascadeOffset, at.Y + cascadeOffset), size, area);

        var floated = LayoutEdits.FloatWindow(layout, windowId, at, size.Width, size.Height);
        return floated with { ClosedWindows = [.. floated.ClosedWindows.Where(closed => closed.WindowId != windowId)] };
    }

    private static LayoutSize FloatingSize(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        var maximumWidth = LayoutSolver.MaximumFloatingWindowWidth(measurements.Available, metrics);
        if (layout.FloatingSizeOf(windowId) is { } remembered)
            return new LayoutSize(Math.Min(remembered.Width, maximumWidth), remembered.Height);

        var minimum = measurements.MinimumWindowLengths.GetValueOrDefault(windowId, metrics.DefaultMinimumWindowLength);
        return new LayoutSize(
            Math.Min(metrics.DefaultWindowWidth, maximumWidth),
            Math.Max(minimum, measurements.Available.Height / 2));
    }

    // Positions are compared to within half a pixel, since a window put there by the same sum can be off by rounding.
    private static bool IsTaken(DockingLayout layout, LayoutPoint at) => layout.FloatingWindows.Any(window =>
        Math.Abs(window.Position.X - at.X) < 0.5 && Math.Abs(window.Position.Y - at.Y) < 0.5);
}
