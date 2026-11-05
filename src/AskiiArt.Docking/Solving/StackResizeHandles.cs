// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Names a handle that resizes a side's windows.</summary>
/// <param name="Edge">The side it's on.</param>
/// <param name="Gap">Which gap between windows it lies in, or null for the handle along the side's inner edge.</param>
public readonly record struct StackResizeHandleId(SideEdge Edge, int? Gap);

/// <summary>A place along a side that catches the pointer, because dragging there resizes windows.</summary>
/// <param name="Id">Which handle.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record StackResizeHandle(StackResizeHandleId Id, LayoutRect Bounds);

/// <summary>The handles in the gaps between widgets and along a side's inner edge (docs/DOCKING.md §3.1).</summary>
public static class StackResizeHandles
{
    /// <summary>The handles a layout has, whatever size the app window is.</summary>
    public static IEnumerable<StackResizeHandleId> IdsIn(DockingLayout layout)
    {
        foreach (var side in layout.Sides.Where(side => side.Kind == SideKind.Widgets))
        {
            yield return new StackResizeHandleId(side.Edge, null);

            var windows = side.Columns.Sum(column => column.Groups.Count);
            for (var gap = 0; gap < windows - 1; gap++)
                yield return new StackResizeHandleId(side.Edge, gap);
        }
    }

    /// <summary>Where the handles go. An inner edge's lies in the margin beside the windows, clear of any toolbar.</summary>
    public static IReadOnlyList<StackResizeHandle> For(SolvedLayout solved, DockingMetrics metrics)
    {
        var handles = new List<StackResizeHandle>();

        foreach (var edge in new[] { SideEdge.Left, SideEdge.Right })
        {
            var groups = solved.WindowGroups
                .Where(group => group.Edge == edge && group.Kind == SideKind.Widgets)
                .OrderBy(group => group.Bounds.Y)
                .ToList();

            if (groups.Count == 0)
                continue;

            var first = groups[0].Bounds;
            var x = edge == SideEdge.Left ? first.Right : first.X - metrics.Margin;
            var length = groups[^1].Bounds.Bottom - first.Y;
            handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, null),
                new LayoutRect(x, first.Y, metrics.Margin, length)));

            for (var gap = 0; gap < groups.Count - 1; gap++)
            {
                var above = groups[gap].Bounds;
                var bounds = new LayoutRect(above.X, above.Bottom, above.Width, groups[gap + 1].Bounds.Y - above.Bottom);
                handles.Add(new StackResizeHandle(new StackResizeHandleId(edge, gap), bounds));
            }
        }

        return handles;
    }
}
