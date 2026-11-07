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

        if (layout.PlaceOf(windowId) is not { } place)
            return layout;

        var column = layout.Side(place.Edge)!.Columns[place.Column];
        var group = column.Groups[place.Position];
        var closed = new ClosedWindow(windowId, place.Edge, place.Position, group.Share, column.Width,
            Column: place.Column, TabmateId: TabmateOf(group, windowId));
        return WithoutWindow(layout, windowId) with
        {
            ClosedWindows = [.. layout.ClosedWindows.Where(other => other.WindowId != windowId), closed],
        };
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

        if (remembered is not null && TryReturnToDock(layout, remembered) is { } docked)
            return docked;

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

    /// <summary>
    /// Whether a window could become a widget: a floating one if any side has room for one, and a dock window if its whole
    /// side can become widgets (<see cref="CanBecomeWidgets"/>). A widget already is one.
    /// </summary>
    public static bool CanTurnIntoWidget(DockingLayout layout, string windowId) => layout.KindOf(windowId) switch
    {
        WindowKind.Floating => SideForNewWidget(layout) is not null,
        WindowKind.Dock => CanBecomeWidgets(layout.Side(layout.PlaceOf(windowId)!.Value.Edge)!),
        _ => false,
    };

    /// <summary>
    /// Makes a window a widget. A floating one goes at the end of a stack on whichever side the opening rules choose, and
    /// keeps its width on an empty side. A dock window's whole side converts, if it can. Does nothing otherwise.
    /// </summary>
    public static DockingLayout TurnIntoWidget(DockingLayout layout, string windowId, double defaultSideWidth)
    {
        if (layout.PlaceOf(windowId) is { Kind: SideKind.Dock } place)
            return TurnSideIntoWidgets(layout, place.Edge);

        if (layout.FloatingWindowNamed(windowId) is null || SideForNewWidget(layout) is not { } edge)
            return layout;

        var width = layout.FloatingWindowNamed(windowId)?.Width ?? defaultSideWidth;
        return layout.Side(edge) is { } side
            ? MoveWindowIntoStack(layout, windowId, edge, side.Columns[0].Groups.Count)
            : MoveWindowToEmptySide(layout, windowId, edge, width);
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

    /// <summary>
    /// A layout with the window nowhere, and not counted as closed either, for moving it somewhere else. A floating window's
    /// size is remembered, for when it floats again.
    /// </summary>
    internal static DockingLayout WithoutWindow(DockingLayout layout, string windowId)
    {
        var floating = layout.FloatingWindows.Where(window => window.WindowId != windowId).ToList();
        var sizes = layout.FloatingWindowNamed(windowId) is { } wasFloating
            ? [.. layout.FloatingSizes.Where(size => size.WindowId != windowId), new FloatingSize(windowId, wasFloating.Width, wasFloating.Height)]
            : layout.FloatingSizes;

        foreach (var side in layout.Sides)
        {
            foreach (var column in side.Columns)
            {
                var position = IndexOfGroupHolding(column, windowId);
                if (position >= 0)
                {
                    return layout with
                    {
                        Sides = SidesWithout(layout, side, column, position, windowId),
                        FloatingWindows = floating,
                        FloatingSizes = sizes,
                    };
                }
            }
        }

        return layout with { FloatingWindows = floating, FloatingSizes = sizes };
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

        return PreferredSide(layout);
    }

    // docs/DOCKING.md §6, rules 2 and 3, for when both sides could take a window: the one with no toolbar at its
    // middle, then the one with fewer windows, then the right.
    private static SideEdge PreferredSide(DockingLayout layout)
    {
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
        side?.Columns.Sum(column => column.Groups.Sum(group => group.WindowIds.Count)) ?? 0;

    // The tab to rejoin when it's reopened: the one showing, or if that's this one, the first other.
    private static string? TabmateOf(WindowGroup group, string windowId)
    {
        if (group.Selected != windowId)
            return group.Selected;

        return group.WindowIds.FirstOrDefault(other => other != windowId);
    }

    // Back as a tab beside the window it was with, if that's in a dock now, or else in its old column of its old side
    // if that's still a dock. A column that has gone comes back as a new one, as near to where it was as there is.
    private static DockingLayout? TryReturnToDock(DockingLayout layout, ClosedWindow remembered)
    {
        var opened = layout with
        {
            ClosedWindows = [.. layout.ClosedWindows.Where(closed => closed.WindowId != remembered.WindowId)],
        };

        if (remembered.TabmateId is { } tabmate && opened.PlaceOf(tabmate) is { Kind: SideKind.Dock } beside)
            return MoveWindowIntoTabs(opened, remembered.WindowId, beside.Edge, beside.Column, beside.Position, int.MaxValue);

        if (opened.Side(remembered.Edge) is not { Kind: SideKind.Dock } side)
            return null;

        if (remembered.Column >= side.Columns.Count)
            return MoveWindowToNewColumn(opened, remembered.WindowId, side.Edge, side.Columns.Count, remembered.SideWidth);

        var column = side.Columns[remembered.Column];
        List<WindowGroup> groups = [.. column.Groups];
        groups.Insert(Math.Min(remembered.Position, groups.Count), new WindowGroup([remembered.WindowId], remembered.Share));
        return WithColumn(opened, side, remembered.Column, column with { Groups = groups });
    }

    // Title-only windows stay as they are, and the fair share comes from the windows showing their contents, levelling
    // the largest down first.
    internal static List<WindowGroup> WithArrival(IReadOnlyList<WindowGroup> groups, string windowId, int position)
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
            groups[position] = group with { WindowIds = remaining, SelectedWindowId = SelectedAfterLeaving(group, windowId) };

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

    // When the tab showing leaves, the one that took its place shows, or the one before it if it was the last.
    private static string? SelectedAfterLeaving(WindowGroup group, string windowId)
    {
        if (group.Selected != windowId)
            return group.SelectedWindowId;

        var index = group.WindowIds.ToList().IndexOf(windowId);
        return index + 1 < group.WindowIds.Count ? group.WindowIds[index + 1] : group.WindowIds[index - 1];
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
