// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

/// <summary>
/// Changes to a layout, each a pure function from one layout to the next. Every drop and menu command goes
/// through here, so a preview can run the same edit as the real thing.
/// </summary>
public static class LayoutEdits
{
    /// <summary>Closes a window, remembering where it was. Does nothing if it isn't open.</summary>
    public static DockingLayout CloseWindow(DockingLayout layout, string windowId)
    {
        foreach (var side in layout.Sides)
        {
            foreach (var column in side.Columns)
            {
                var position = IndexOfGroupHolding(column, windowId);
                if (position < 0)
                    continue;

                var group = column.Groups[position];
                var closed = new ClosedWindow(windowId, side.Edge, position, group.Share, column.Width);
                return layout with
                {
                    Sides = WithoutWindow(layout, side, column, position, windowId),
                    ClosedWindows = [.. layout.ClosedWindows.Where(other => other.WindowId != windowId), closed],
                };
            }
        }

        return layout;
    }

    /// <summary>
    /// Opens a window: back where it was if that's still possible, otherwise on a side with room. With no
    /// room anywhere it stays closed, until floating (step 2.5e) and the opening rules (step 2.5g) exist.
    /// </summary>
    public static DockingLayout OpenWindow(DockingLayout layout, string windowId, double defaultSideWidth)
    {
        if (layout.IsWindowOpen(windowId))
            return layout;

        var remembered = layout.ClosedWindows.FirstOrDefault(closed => closed.WindowId == windowId);
        if (remembered is not null &&
            TryJoinStack(layout, windowId, remembered.Edge, remembered.Position, remembered.Share, remembered.SideWidth)
                is { } back)
        {
            return back;
        }

        foreach (var edge in new[] { SideEdge.Right, SideEdge.Left })
        {
            var share = FairShare(layout.Side(edge));
            if (TryJoinStack(layout, windowId, edge, int.MaxValue, share, defaultSideWidth) is { } placed)
                return placed;
        }

        return layout;
    }

    /// <summary>
    /// Shows or hides a toolbar. An anchor holds one toolbar, so showing one whose anchor has since been
    /// taken moves it to a free anchor (step 2.5c floats it instead).
    /// </summary>
    public static DockingLayout SetToolbarVisible(DockingLayout layout, string toolbarId, bool isVisible)
    {
        return layout with { Toolbars = [.. layout.Toolbars.Select(Updated)] };

        ToolbarPlacement Updated(ToolbarPlacement toolbar)
        {
            if (toolbar.ToolbarId != toolbarId)
                return toolbar;

            if (!isVisible)
                return toolbar with { IsVisible = false };

            var taken = layout.Toolbars
                .Where(other => other.IsVisible && other.ToolbarId != toolbarId)
                .Select(other => other.Anchor)
                .ToHashSet();

            if (!taken.Contains(toolbar.Anchor))
                return toolbar with { IsVisible = true };

            var free = ToolbarAnchors.All.Where(anchor => !taken.Contains(anchor)).ToList();
            return free.Count > 0 ? Placed(toolbar, free[0]) : toolbar;
        }
    }

    /// <summary>A toolbar moved to <paramref name="anchor"/>, following the direction it requires.</summary>
    internal static ToolbarPlacement Placed(ToolbarPlacement toolbar, ToolbarAnchor anchor) => toolbar with
    {
        Anchor = anchor,
        Orientation = ToolbarAnchors.RequiredOrientation(anchor) ?? toolbar.Orientation,
        IsVisible = true,
    };

    /// <summary>A stack always keeps at least one window's contents showing.</summary>
    internal static IReadOnlyList<WindowGroup> WithOneGroupOpen(IReadOnlyList<WindowGroup> groups)
    {
        if (groups.Count == 0 || groups.Any(group => !group.IsTitleOnly))
            return groups;

        return [.. groups.Take(groups.Count - 1), groups[^1] with { IsTitleOnly = false }];
    }

    private static int IndexOfGroupHolding(SideColumn column, string windowId)
    {
        for (var index = 0; index < column.Groups.Count; index++)
        {
            if (column.Groups[index].WindowIds.Contains(windowId, StringComparer.Ordinal))
                return index;
        }

        return -1;
    }

    private static List<SideLayout> WithoutWindow(DockingLayout layout, SideLayout side, SideColumn column,
        int position, string windowId)
    {
        var group = column.Groups[position];
        var remaining = group.WindowIds.Where(id => id != windowId).ToList();

        List<WindowGroup> groups = [.. column.Groups];
        if (remaining.Count == 0)
            groups.RemoveAt(position);
        else
            groups[position] = group with { WindowIds = remaining };

        var columns = side.Columns
            .Select(other => ReferenceEquals(other, column) ? column with { Groups = WithOneGroupOpen(groups) } : other)
            .Where(other => other.Groups.Count > 0)
            .ToList();

        return
        [
            .. layout.Sides
                .Select(other => ReferenceEquals(other, side) ? side with { Columns = columns } : other)
                .Where(other => other.Columns.Count > 0),
        ];
    }

    private static DockingLayout? TryJoinStack(DockingLayout layout, string windowId, SideEdge edge, int position,
        double share, double width)
    {
        var joining = new WindowGroup([windowId], share);
        var opened = layout.ClosedWindows.Where(closed => closed.WindowId != windowId).ToList();

        if (layout.Side(edge) is not { } side)
        {
            var newSide = new SideLayout(edge, SideKind.Widgets, [new SideColumn(width, [joining])]);
            return layout with { Sides = [.. layout.Sides, newSide], ClosedWindows = opened };
        }

        if (side.Kind != SideKind.Widgets || side.Columns[0].Groups.Count >= DockingLayout.MaximumWidgetsOnASide)
            return null;

        var column = side.Columns[0];
        List<WindowGroup> groups = [.. column.Groups];
        groups.Insert(Math.Clamp(position, 0, groups.Count), joining);

        var updated = side with { Columns = [column with { Groups = groups }] };
        return layout with
        {
            Sides = [.. layout.Sides.Select(other => ReferenceEquals(other, side) ? updated : other)],
            ClosedWindows = opened,
        };
    }

    private static double FairShare(SideLayout? side)
    {
        var groups = side?.Columns.SelectMany(column => column.Groups).ToList();
        return groups is { Count: > 0 } ? groups.Average(group => group.Share) : 1;
    }
}
