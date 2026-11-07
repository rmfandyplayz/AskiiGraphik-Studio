// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>
/// Turning a window into a dock window from its menu (docs/DOCKING.md §3.2). Unlike the other conversions this needs the
/// app window's size, because a floating window becomes a new column, and a column only goes where it fits.
/// </summary>
public static class WindowConversions
{
    /// <summary>
    /// Whether the window could become a dock window: a widget's side always can, and a floating window needs a side
    /// with room for a new column. A dock window already is one.
    /// </summary>
    public static bool CanTurnIntoDock(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics) => layout.KindOf(windowId) switch
        {
            WindowKind.Widget => true,
            WindowKind.Floating => SideForFloatingWindow(layout, windowId, measurements, metrics) is not null,
            _ => false,
        };

    /// <summary>
    /// Makes the window a dock window. A widget's whole side converts, keeping its order and heights. A floating window
    /// becomes a new column at the outer edge of a dock, or of whichever side the opening rules choose, which converts.
    /// It keeps its width as far as the room allows. Does nothing if it can't.
    /// </summary>
    public static DockingLayout TurnIntoDock(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        if (layout.PlaceOf(windowId) is { Kind: SideKind.Widgets } place)
            return LayoutEdits.TurnSideIntoDock(layout, place.Edge);

        if (layout.FloatingWindowNamed(windowId) is not { } floating ||
            SideForFloatingWindow(layout, windowId, measurements, metrics) is not { } edge)
        {
            return layout;
        }

        var without = LayoutEdits.WithoutWindow(layout, windowId);
        var room = LayoutSolver.RoomForANewColumn(without, edge, measurements.Available.Width, metrics);
        var width = Math.Clamp(floating.Width, metrics.MinimumWindowWidth, Math.Max(metrics.MinimumWindowWidth, room));
        return LayoutEdits.MoveWindowToNewColumn(layout, windowId, edge, 0, width);
    }

    private static SideEdge? SideForFloatingWindow(DockingLayout layout, string windowId, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        var without = LayoutEdits.WithoutWindow(layout, windowId);
        return LayoutEdits.SideForNewDockColumn(without, edge =>
            LayoutSolver.HasRoomForANewColumn(without, edge, measurements.Available.Width, metrics));
    }
}
