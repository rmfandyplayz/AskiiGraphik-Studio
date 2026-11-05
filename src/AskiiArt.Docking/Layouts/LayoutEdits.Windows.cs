// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

public static partial class LayoutEdits
{
    /// <summary>Closes a window, remembering where it was. Does nothing if it isn't open.</summary>
    public static DockingLayout CloseWindow(DockingLayout layout, string windowId)
    {
        if (layout.FloatingWindowNamed(windowId) is { } floating)
        {
            var wasFloating = new ClosedWindow(windowId, SideEdge.Right, 0, 1, floating.Width, floating);
            return WithoutWindow(layout, windowId) with
            {
                ClosedWindows = [.. layout.ClosedWindows.Where(other => other.WindowId != windowId), wasFloating],
            };
        }

        foreach (var side in layout.Sides)
        {
            foreach (var column in side.Columns)
            {
                var position = IndexOfGroupHolding(column, windowId);
                if (position < 0)
                    continue;

                var group = column.Groups[position];
                var closed = new ClosedWindow(windowId, side.Edge, position, group.Share, column.Width);
                return WithoutWindow(layout, windowId) with
                {
                    ClosedWindows = [.. layout.ClosedWindows.Where(other => other.WindowId != windowId), closed],
                };
            }
        }

        return layout;
    }

    /// <summary>
    /// Opens a window: back where it was if that's still possible, otherwise on a side with room. With no room
    /// anywhere it stays closed, until the rest of the opening rules arrive in step 2.5g.
    /// </summary>
    public static DockingLayout OpenWindow(DockingLayout layout, string windowId, double defaultSideWidth)
    {
        if (layout.IsWindowOpen(windowId))
            return layout;

        var remembered = layout.ClosedWindows.FirstOrDefault(closed => closed.WindowId == windowId);
        if (remembered?.WasFloating is { } floating)
        {
            return layout with
            {
                FloatingWindows = [.. layout.FloatingWindows, floating],
                ClosedWindows = [.. layout.ClosedWindows.Where(closed => closed.WindowId != windowId)],
            };
        }

        if (remembered is not null &&
            TryJoinStack(layout, windowId, remembered.Edge, remembered.Position, remembered.Share, remembered.SideWidth)
                is { } back)
        {
            return back;
        }

        if (SideForNewWidget(layout) is not { } edge)
            return layout;

        return TryJoinStack(layout, windowId, edge, int.MaxValue, FairShare(layout.Side(edge)), defaultSideWidth) ?? layout;
    }

    /// <summary>Floats a window, in front of any other floating one.</summary>
    public static DockingLayout FloatWindow(DockingLayout layout, string windowId, LayoutPoint position, double width,
        double height, bool isTitleOnly = false)
    {
        var floating = new FloatingWindow(windowId, position, width, height, isTitleOnly);
        var without = WithoutWindow(layout, windowId);
        return without with { FloatingWindows = [.. without.FloatingWindows, floating] };
    }

    /// <summary>
    /// Puts a window in a side's stack, at its place among the windows that would be there without it. It takes a fair
    /// share of the stack, from the largest window first, and lands with its contents showing. Does nothing if the side
    /// is missing, isn't made of widgets or is full.
    /// </summary>
    public static DockingLayout MoveWindowIntoStack(DockingLayout layout, string windowId, SideEdge edge, int position)
    {
        var without = WithoutWindow(layout, windowId);
        if (without.Side(edge) is not { Kind: SideKind.Widgets } side || side.Columns[0].Groups.Count >= DockingLayout.MaximumWidgetsOnASide)
            return layout;

        var column = side.Columns[0];
        var updated = side with { Columns = [column with { Groups = WithArrival(column.Groups, windowId, position) }] };
        return without with { Sides = [.. without.Sides.Select(other => ReferenceEquals(other, side) ? updated : other)] };
    }

    /// <summary>Makes a window the only widget on an empty side. Does nothing if something else is on that side.</summary>
    public static DockingLayout MoveWindowToEmptySide(DockingLayout layout, string windowId, SideEdge edge, double width)
    {
        var without = WithoutWindow(layout, windowId);
        if (without.Side(edge) is not null)
            return layout;

        var side = new SideLayout(edge, SideKind.Widgets, [new SideColumn(width, [new WindowGroup([windowId])])]);
        return without with { Sides = [.. without.Sides, side] };
    }

    /// <summary>Whether a window could become a widget: whether any side has room for one.</summary>
    public static bool CanTurnIntoWidget(DockingLayout layout) => SideForNewWidget(layout) is not null;

    /// <summary>Makes a floating window a widget, at the end of a stack on whichever side the opening rules choose.</summary>
    public static DockingLayout TurnIntoWidget(DockingLayout layout, string windowId, double defaultSideWidth)
    {
        if (SideForNewWidget(layout) is not { } edge)
            return layout;

        return layout.Side(edge) is { } side
            ? MoveWindowIntoStack(layout, windowId, edge, side.Columns[0].Groups.Count)
            : MoveWindowToEmptySide(layout, windowId, edge, defaultSideWidth);
    }

    /// <summary>Puts a floating window in front of the others. Does nothing if it's there already, or isn't floating.</summary>
    public static DockingLayout BringFloatingWindowToFront(DockingLayout layout, string windowId)
    {
        if (layout.FloatingWindowNamed(windowId) is not { } floating || layout.FloatingWindows[^1] == floating)
            return layout;

        return layout with
        {
            FloatingWindows = [.. layout.FloatingWindows.Where(window => window.WindowId != windowId), floating],
        };
    }

    /// <summary>A layout with the window nowhere, and not counted as closed either, for moving it somewhere else.</summary>
    internal static DockingLayout WithoutWindow(DockingLayout layout, string windowId)
    {
        var floating = layout.FloatingWindows.Where(window => window.WindowId != windowId).ToList();

        foreach (var side in layout.Sides)
        {
            foreach (var column in side.Columns)
            {
                var position = IndexOfGroupHolding(column, windowId);
                if (position >= 0)
                    return layout with { Sides = SidesWithout(layout, side, column, position, windowId), FloatingWindows = floating };
            }
        }

        return layout with { FloatingWindows = floating };
    }

    /// <summary>A stack always keeps at least one window's contents showing.</summary>
    internal static IReadOnlyList<WindowGroup> WithOneGroupOpen(IReadOnlyList<WindowGroup> groups)
    {
        if (groups.Count == 0 || groups.Any(group => !group.IsTitleOnly))
            return groups;

        return [.. groups.Take(groups.Count - 1), groups[^1] with { IsTitleOnly = false }];
    }

    // docs/DOCKING.md §6: a side that alone has room, then the one with no toolbar at its middle, then the one with
    // fewer windows, then the right.
    private static SideEdge? SideForNewWidget(DockingLayout layout)
    {
        var leftHasRoom = HasRoomForAWidget(layout.Side(SideEdge.Left));
        var rightHasRoom = HasRoomForAWidget(layout.Side(SideEdge.Right));

        if (!leftHasRoom && !rightHasRoom)
            return null;

        if (leftHasRoom != rightHasRoom)
            return leftHasRoom ? SideEdge.Left : SideEdge.Right;

        var toolbarOnLeft = layout.IsAnchorTaken(ToolbarAnchor.LeftMiddle, string.Empty);
        var toolbarOnRight = layout.IsAnchorTaken(ToolbarAnchor.RightMiddle, string.Empty);
        if (toolbarOnLeft != toolbarOnRight)
            return toolbarOnLeft ? SideEdge.Right : SideEdge.Left;

        return WindowCount(layout.Side(SideEdge.Left)) < WindowCount(layout.Side(SideEdge.Right))
            ? SideEdge.Left
            : SideEdge.Right;
    }

    private static bool HasRoomForAWidget(SideLayout? side) =>
        side is null || (side.Kind == SideKind.Widgets && WindowCount(side) < DockingLayout.MaximumWidgetsOnASide);

    private static int WindowCount(SideLayout? side) =>
        side?.Columns.Sum(column => column.Groups.Count) ?? 0;

    // Title-only windows stay as they are, and the fair share comes from the windows showing their contents, levelling
    // the largest down first.
    private static List<WindowGroup> WithArrival(IReadOnlyList<WindowGroup> groups, string windowId, int position)
    {
        var shares = groups.Where(group => !group.IsTitleOnly).Select(group => group.Share).ToList();
        var total = shares.Sum();
        var fairShare = shares.Count > 0 ? total / (shares.Count + 1) : 1;
        var level = LevelLeaving(shares, total - fairShare);

        List<WindowGroup> result =
        [
            .. groups.Select(group => group.IsTitleOnly ? group : group with { Share = Math.Min(group.Share, level) }),
        ];

        result.Insert(Math.Clamp(position, 0, result.Count), new WindowGroup([windowId], fairShare));
        return result;
    }

    // The height to cut everything taller down to, so that what's left adds up to the room.
    private static double LevelLeaving(List<double> shares, double room)
    {
        var sorted = shares.Order().ToList();
        for (var index = 0; index < sorted.Count; index++)
        {
            var level = room / (sorted.Count - index);
            if (level <= sorted[index])
                return level;

            room -= sorted[index];
        }

        return sorted.Count > 0 ? sorted[^1] : 0;
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

    private static List<SideLayout> SidesWithout(DockingLayout layout, SideLayout side, SideColumn column,
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
