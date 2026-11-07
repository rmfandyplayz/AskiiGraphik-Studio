// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Names a handle that resizes a side's windows.</summary>
/// <param name="Edge">The side it's on.</param>
/// <param name="Column">The column it resizes, from the app window's edge. A widget side's is 0.</param>
/// <param name="Gap">
/// Which gap between the column's windows it lies in, or null for the handle along the column's inner edge.
/// </param>
public readonly record struct StackResizeHandleId(SideEdge Edge, int Column, int? Gap);

/// <summary>A place along a side that catches the pointer, because dragging there resizes windows.</summary>
/// <param name="Id">Which handle.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record StackResizeHandle(StackResizeHandleId Id, LayoutRect Bounds);

/// <summary>
/// The handles that resize a side's windows: in the gaps between widgets and along a widget side's inner edge
/// (docs/DOCKING.md §3.1), and on every border of a dock (§3.3). The border between two dock columns belongs to the outer
/// one, so dragging it only resizes that.
/// </summary>
public static class StackResizeHandles
{
    /// <summary>The handles a layout has, whatever size the app window is.</summary>
    public static IEnumerable<StackResizeHandleId> IdsIn(DockingLayout layout)
    {
        foreach (var side in layout.Sides)
        {
            for (var column = 0; column < side.Columns.Count; column++)
            {
                yield return new StackResizeHandleId(side.Edge, column, null);

                for (var gap = 0; gap < side.Columns[column].Groups.Count - 1; gap++)
                    yield return new StackResizeHandleId(side.Edge, column, gap);
            }
        }
    }

    /// <summary>
    /// Where the handles go. A widget side's inner edge has its handle in the margin beside the windows, clear of any
    /// toolbar, and its gaps' handles fill the gaps. A dock has no margins or gaps, so its handles straddle its borders.
    /// </summary>
    public static IReadOnlyList<StackResizeHandle> For(SolvedLayout solved, DockingMetrics metrics)
    {
        var handles = new List<StackResizeHandle>();

        foreach (var columnGroups in solved.WindowGroups.GroupBy(group => (group.Edge, group.Column)))
        {
            var groups = columnGroups.OrderBy(group => group.Bounds.Y).ToList();
            var (edge, column) = columnGroups.Key;

            if (groups[0].Kind == SideKind.Widgets)
                AddWidgetHandles(handles, edge, groups, metrics);
            else
                AddDockHandles(handles, edge, column, groups, metrics);
        }

        return handles;
    }

    private static void AddWidgetHandles(List<StackResizeHandle> handles, SideEdge edge, List<SolvedWindowGroup> groups,
        DockingMetrics metrics)
    {
        var first = groups[0].Bounds;
        var x = edge == SideEdge.Left ? first.Right : first.X - metrics.Margin;
        var length = groups[^1].Bounds.Bottom - first.Y;
        handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, 0, null),
            new LayoutRect(x, first.Y, metrics.Margin, length)));

        for (var gap = 0; gap < groups.Count - 1; gap++)
        {
            var above = groups[gap].Bounds;
            var bounds = new LayoutRect(above.X, above.Bottom, above.Width, groups[gap + 1].Bounds.Y - above.Bottom);
            handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, 0, gap), bounds));
        }
    }

    private static void AddDockHandles(List<StackResizeHandle> handles, SideEdge edge, int column,
        List<SolvedWindowGroup> groups, DockingMetrics metrics)
    {
        var thickness = metrics.DockHandleThickness;
        var first = groups[0].Bounds;
        var innerEdge = edge == SideEdge.Left ? first.Right : first.X;
        var length = groups[^1].Bounds.Bottom - first.Y;
        handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, column, null),
            new LayoutRect(innerEdge - thickness / 2, first.Y, thickness, length)));

        for (var gap = 0; gap < groups.Count - 1; gap++)
        {
            var above = groups[gap].Bounds;
            handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, column, gap),
                new LayoutRect(above.X, above.Bottom - thickness / 2, above.Width, thickness)));
        }
    }
}
