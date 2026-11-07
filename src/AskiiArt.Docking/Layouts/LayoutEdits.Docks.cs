// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

public static partial class LayoutEdits
{
    /// <summary>Shows one of a dock group's tabs. Does nothing if the window isn't in a dock.</summary>
    public static DockingLayout SelectTab(DockingLayout layout, string windowId)
    {
        if (layout.PlaceOf(windowId) is not { Kind: SideKind.Dock } place)
            return layout;

        var side = layout.Side(place.Edge)!;
        var column = side.Columns[place.Column];
        if (column.Groups[place.Position].Selected == windowId)
            return layout;

        List<WindowGroup> groups = [.. column.Groups];
        groups[place.Position] = groups[place.Position] with { SelectedWindowId = windowId };
        return WithColumn(layout, side, place.Column, column with { Groups = groups });
    }

    /// <summary>
    /// Puts a window in a new dock column, <paramref name="column"/> columns in from the app window's edge among the
    /// columns there would be without it. A widget side becomes a dock first, keeping its order and heights, and an empty
    /// side becomes a dock of one.
    /// </summary>
    public static DockingLayout MoveWindowToNewColumn(DockingLayout layout, string windowId, SideEdge edge, int column,
        double width)
    {
        var without = WithoutWindow(layout, windowId);
        var arriving = new SideColumn(width, [new WindowGroup([windowId])]);

        if (without.Side(edge) is not { } side)
            return WithSide(without, new SideLayout(edge, SideKind.Dock, [arriving]));

        List<SideColumn> columns = [.. side.Columns];
        columns.Insert(Math.Clamp(column, 0, columns.Count), arriving);
        return WithSide(without, AsDock(side) with { Columns = columns });
    }

    /// <summary>
    /// Puts a window in a dock column's stack, at <paramref name="position"/> among the groups there would be without it.
    /// It takes a fair share of the column, as a widget arriving in a stack does. Does nothing if there's no such column.
    /// </summary>
    public static DockingLayout MoveWindowIntoColumn(DockingLayout layout, string windowId, SideEdge edge, int column,
        int position)
    {
        var without = WithoutWindow(layout, windowId);
        if (without.Side(edge) is not { Kind: SideKind.Dock } side || column < 0 || column >= side.Columns.Count)
            return layout;

        var target = side.Columns[column];
        return WithColumn(without, side, column, target with { Groups = WithArrival(target.Groups, windowId, position) });
    }

    /// <summary>
    /// Makes a window a tab in a dock's group, at <paramref name="tab"/> among the tabs there would be without it, and
    /// shows it. Does nothing if there's no such group.
    /// </summary>
    public static DockingLayout MoveWindowIntoTabs(DockingLayout layout, string windowId, SideEdge edge, int column,
        int position, int tab)
    {
        var without = WithoutWindow(layout, windowId);
        if (without.Side(edge) is not { Kind: SideKind.Dock } side || column < 0 || column >= side.Columns.Count ||
            position < 0 || position >= side.Columns[column].Groups.Count)
        {
            return layout;
        }

        var target = side.Columns[column];
        var group = target.Groups[position];
        List<string> windowIds = [.. group.WindowIds];
        windowIds.Insert(Math.Clamp(tab, 0, windowIds.Count), windowId);

        List<WindowGroup> groups = [.. target.Groups];
        groups[position] = group with { WindowIds = windowIds, SelectedWindowId = windowId };
        return WithColumn(without, side, column, target with { Groups = groups });
    }

    /// <summary>
    /// Whether a side could hold widgets as it is: one column of at most
    /// <see cref="DockingLayout.MaximumWidgetsOnASide"/> groups, with no tabs.
    /// </summary>
    public static bool CanBecomeWidgets(SideLayout side) =>
        side.Columns is [var only] &&
        only.Groups.Count <= DockingLayout.MaximumWidgetsOnASide &&
        only.Groups.All(group => group.WindowIds.Count == 1);

    /// <summary>Makes a widget side a dock, keeping its order and heights. Does nothing to any other side.</summary>
    public static DockingLayout TurnSideIntoDock(DockingLayout layout, SideEdge edge) =>
        layout.Side(edge) is { Kind: SideKind.Widgets } side ? WithSide(layout, AsDock(side)) : layout;

    /// <summary>
    /// Makes a dock side widgets, keeping its order and heights. Does nothing unless it's a dock that
    /// <see cref="CanBecomeWidgets"/>.
    /// </summary>
    public static DockingLayout TurnSideIntoWidgets(DockingLayout layout, SideEdge edge)
    {
        if (layout.Side(edge) is not { Kind: SideKind.Dock } side || !CanBecomeWidgets(side))
            return layout;

        var column = side.Columns[0];
        var widgets = column with { Groups = [.. column.Groups.Select(group => group with { SelectedWindowId = null })] };
        return WithSide(layout, side with { Kind = SideKind.Widgets, Columns = [widgets] });
    }

    /// <summary>
    /// The side a floating window turned into a dock goes to (docs/DOCKING.md §3.2), of those with room for a column: a
    /// dock, then the side the widget opening rules choose, then the side with no toolbar at its middle, fewer windows,
    /// and the right. Null if neither side has room.
    /// </summary>
    internal static SideEdge? SideForNewDockColumn(DockingLayout layout, Func<SideEdge, bool> hasRoomForAColumn)
    {
        var leftHasRoom = hasRoomForAColumn(SideEdge.Left);
        var rightHasRoom = hasRoomForAColumn(SideEdge.Right);

        if (!leftHasRoom && !rightHasRoom)
            return null;

        if (leftHasRoom != rightHasRoom)
            return leftHasRoom ? SideEdge.Left : SideEdge.Right;

        var leftIsDock = layout.Side(SideEdge.Left)?.Kind == SideKind.Dock;
        var rightIsDock = layout.Side(SideEdge.Right)?.Kind == SideKind.Dock;
        if (leftIsDock != rightIsDock)
            return leftIsDock ? SideEdge.Left : SideEdge.Right;

        if (!leftIsDock && SideForNewWidget(layout) is { } edge)
            return edge;

        return PreferredSide(layout);
    }

    private static SideLayout AsDock(SideLayout side) => side with { Kind = SideKind.Dock };

    private static DockingLayout WithSide(DockingLayout layout, SideLayout side)
    {
        if (layout.Side(side.Edge) is null)
            return layout with { Sides = [.. layout.Sides, side] };

        return layout with { Sides = [.. layout.Sides.Select(other => other.Edge == side.Edge ? side : other)] };
    }

    private static DockingLayout WithColumn(DockingLayout layout, SideLayout side, int index, SideColumn column)
    {
        List<SideColumn> columns = [.. side.Columns];
        columns[index] = column;
        return WithSide(layout, side with { Columns = columns });
    }
}
