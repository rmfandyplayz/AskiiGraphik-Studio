// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>
/// Fits a layout to the app window: sides first, then toolbars in what's left, then the safe area
/// (docs/DOCKING.md §2). It has no state and changes nothing, so it can run on every layout pass, and on a
/// layout that's only being previewed.
/// </summary>
public static class LayoutSolver
{
    /// <summary>Where everything in <paramref name="layout"/> goes right now.</summary>
    /// <param name="layout">What to place.</param>
    /// <param name="measurements">The sizes things want.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public static SolvedLayout Solve(DockingLayout layout, LayoutMeasurements measurements, DockingMetrics metrics)
    {
        var available = measurements.Available;
        var room = measurements.DropRoom;
        var columnWidths = FittedColumnWidths(layout, room, available.Width, metrics);
        var groups = new List<SolvedWindowGroup>();
        var taken = new Dictionary<SideEdge, double>();
        LayoutRect? dropMarker = null;

        foreach (var side in layout.Sides)
        {
            var sideRoom = room?.Edge == side.Edge ? room : null;
            var placed = PlaceSide(side, measurements, metrics, columnWidths[side.Edge], sideRoom, groups);
            taken[side.Edge] = placed.Taken;
            dropMarker ??= placed.DropMarker;
        }

        if (room is { Kind: DropRoomKind.EmptySide } && taken.GetValueOrDefault(room.Edge) == 0)
        {
            taken[room.Edge] = metrics.Margin + room.Thickness;
            dropMarker = OnEdge(room.Edge, available, metrics.Margin, room.Thickness, metrics.Margin,
                Math.Max(0, available.Height - metrics.Margin * 2));
        }

        var leftBand = taken.GetValueOrDefault(SideEdge.Left);
        var rightBand = taken.GetValueOrDefault(SideEdge.Right);
        var region = new LayoutRect(leftBand, 0, Math.Max(0, available.Width - leftBand - rightBand), available.Height);
        var solution = ToolbarSolver.Solve(layout.Toolbars, measurements.ToolbarSizes, region, metrics,
            measurements.SubToolbar);

        // Without the sub-toolbar, whatever it pushed, so that picking a tool never moves the view.
        var safeArea = SafeArea(region, available, solution.ToolbarsWithoutSubToolbar, metrics.Margin);

        var floating = layout.FloatingWindows.Select(window => PlaceFloating(window, available, metrics)).ToList();

        return new SolvedLayout(groups, solution.Toolbars, region, safeArea, solution.SubToolbar, floating, dropMarker);
    }

    /// <summary>
    /// Where a toolbar's top left would be if it had its anchor to itself, for floating it there when the anchor
    /// has been taken. <paramref name="measurements"/> must include its size.
    /// </summary>
    public static LayoutPoint WhereAloneOnItsAnchor(DockingLayout layout, string toolbarId,
        LayoutMeasurements measurements, DockingMetrics metrics)
    {
        var alone = layout.Toolbars
            .Where(toolbar => toolbar.ToolbarId == toolbarId)
            .Select(toolbar => toolbar with { IsVisible = true, FloatingAt = null });
        var solved = Solve(layout with { Toolbars = [.. alone] }, measurements, metrics);

        return solved.Toolbars.Count > 0
            ? new LayoutPoint(solved.Toolbars[0].Bounds.X, solved.Toolbars[0].Bounds.Y)
            : new LayoutPoint(solved.CanvasRegion.X + metrics.Margin, solved.CanvasRegion.Y + metrics.Margin);
    }

    /// <summary>
    /// Where floating windows stay: inside the app window, by the margin widgets keep from its edges. That also keeps their
    /// edges clear of the app window's own, which resize it.
    /// </summary>
    public static LayoutRect FloatingWindowArea(LayoutSize available, DockingMetrics metrics)
    {
        var margin = Math.Max(0, Math.Min(metrics.Margin, Math.Min(available.Width, available.Height) / 2));
        return new LayoutRect(margin, margin, available.Width - margin * 2, available.Height - margin * 2);
    }

    /// <summary>The widest a floating window can be made: as wide as a side's windows can get, with the other side empty.</summary>
    public static double MaximumFloatingWindowWidth(LayoutSize available, DockingMetrics metrics) =>
        Math.Max(metrics.MinimumWindowWidth, available.Width - metrics.MinimumCanvasRegionWidth - metrics.Margin);

    /// <summary>
    /// How wide a new column on <paramref name="edge"/> could be, with every column at the width the user wants and the
    /// canvas region at its minimum. A widget side counts as a dock, since a new column makes it one. A column fits only
    /// when this is at least <see cref="DockingMetrics.MinimumWindowWidth"/>.
    /// </summary>
    public static double RoomForANewColumn(DockingLayout layout, SideEdge edge, double availableWidth, DockingMetrics metrics)
    {
        var widths = layout.Sides.Sum(side => side.Columns.Sum(column => column.Width));
        var margins = layout.Sides.Where(side => side.Edge != edge).Sum(side => OuterMargin(side, metrics));
        return availableWidth - metrics.MinimumCanvasRegionWidth - widths - margins;
    }

    /// <summary>Whether a new column on <paramref name="edge"/> fits (<see cref="RoomForANewColumn"/>).</summary>
    public static bool HasRoomForANewColumn(DockingLayout layout, SideEdge edge, double availableWidth,
        DockingMetrics metrics) =>
        RoomForANewColumn(layout, edge, availableWidth, metrics) >= metrics.MinimumWindowWidth;

    /// <summary>Where a rectangle's top left goes to keep it inside <paramref name="area"/>, moved as little as it can be.</summary>
    public static LayoutPoint KeptInside(LayoutPoint position, LayoutSize size, LayoutRect area) => new(
        // Max last, so rounding can't put it outside when it exactly fills the room.
        Math.Max(area.X, Math.Min(position.X, area.Right - size.Width)),
        Math.Max(area.Y, Math.Min(position.Y, area.Bottom - size.Height)));

    // Moved just enough to stay inside, without changing what's stored, so regrowing the window puts it back.
    private static SolvedFloatingWindow PlaceFloating(FloatingWindow window, LayoutSize available, DockingMetrics metrics)
    {
        var area = FloatingWindowArea(available, metrics);
        var size = new LayoutSize(
            Math.Min(window.Width, area.Width),
            Math.Min(window.IsTitleOnly ? metrics.TitleLength : window.Height, area.Height));
        var at = KeptInside(window.Position, size, area);

        return new SolvedFloatingWindow(window.WindowId, new LayoutRect(at.X, at.Y, size.Width, size.Height), window.IsTitleOnly);
    }

    // Columns shrink when they'd leave the canvas region too narrow (ColumnWidths). Wanted widths stay in the layout.
    // A drop room that's a new column is one of them, at its place, and keeps its width, which is below the floor.
    private static Dictionary<SideEdge, double[]> FittedColumnWidths(DockingLayout layout, DropRoom? dropRoom,
        double availableWidth, DockingMetrics metrics)
    {
        var margins = layout.Sides.Sum(side => OuterMargin(side, metrics));
        var room = Math.Max(0, availableWidth - metrics.MinimumCanvasRegionWidth - margins);
        var wantedBySide = layout.Sides.Select(side => WantedWidths(side, dropRoom, metrics)).ToList();
        var fitted = ColumnWidths.Fit([.. wantedBySide.SelectMany(widths => widths)], room, metrics.MinimumWindowWidth);

        var widths = new Dictionary<SideEdge, double[]>();
        var next = 0;
        for (var index = 0; index < layout.Sides.Count; index++)
        {
            widths[layout.Sides[index].Edge] = fitted[next..(next + wantedBySide[index].Count)];
            next += wantedBySide[index].Count;
        }

        return widths;
    }

    // With a new column's room among them. Between two columns, those two give up what they can, down to the narrowest a
    // window can be, and the dock grows by the rest. At either edge of the dock, it grows by all of it.
    private static List<double> WantedWidths(SideLayout side, DropRoom? room, DockingMetrics metrics)
    {
        var widths = side.Columns.Select(column => column.Width).ToArray();
        if (room is not { Kind: DropRoomKind.Column } || room.Edge != side.Edge)
            return [.. widths];

        var at = Math.Clamp(room.Column, 0, widths.Length);
        if (at > 0 && at < widths.Length)
        {
            var floors = widths.Select(width => Math.Min(width, metrics.MinimumWindowWidth)).ToArray();
            RoomTaking.Take(widths, floors, at, room.Thickness, othersToo: false);
        }

        return [.. widths[..at], room.Thickness, .. widths[at..]];
    }

    // Places the side's windows, leaving its drop room empty, if it has one.
    private static PlacedSide PlaceSide(SideLayout side, LayoutMeasurements measurements, DockingMetrics metrics,
        double[] columnWidths, DropRoom? room, List<SolvedWindowGroup> groups)
    {
        var margin = OuterMargin(side, metrics);
        var gap = side.Kind == SideKind.Widgets ? metrics.WidgetGap : 0;
        var stackLength = Math.Max(0, measurements.Available.Height - margin * 2);
        var roomColumn = room is { Kind: DropRoomKind.Column } ? Math.Clamp(room.Column, 0, side.Columns.Count) : -1;
        var across = margin;
        LayoutRect? marker = null;

        var columnIndex = 0;
        foreach (var width in columnWidths)
        {
            if (columnIndex == roomColumn && marker is null)
            {
                var bounds = OnEdge(side.Edge, measurements.Available, across, width, margin, stackLength);
                marker = Padded(bounds, metrics);
                across += width;
                continue;
            }

            var column = side.Columns[columnIndex];
            var entries = column.Groups
                .Select(group => new StackSolver.Entry(group.Share, group.IsTitleOnly, MinimumLength(group, measurements, metrics)))
                .ToList();

            IReadOnlyList<StackSolver.Slot> slots;
            if (room is { Kind: DropRoomKind.Stack } && room.Column == columnIndex)
            {
                (slots, var roomSlot) = StackSolver.SolveWithRoom(entries, stackLength, gap, metrics.TitleLength,
                    room.Position, room.Thickness);
                var bounds = OnEdge(side.Edge, measurements.Available, across, width, margin + roomSlot.Start, roomSlot.Length);
                marker = side.Kind == SideKind.Dock ? Padded(bounds, metrics) : bounds;
            }
            else
            {
                slots = StackSolver.Solve(entries, stackLength, gap, metrics.TitleLength);
            }

            for (var index = 0; index < slots.Count; index++)
            {
                var bounds = OnEdge(side.Edge, measurements.Available, across, width, margin + slots[index].Start, slots[index].Length);
                var group = column.Groups[index];
                groups.Add(new SolvedWindowGroup(group.WindowIds, side.Edge, side.Kind, bounds, slots[index].IsTitleOnly,
                    columnIndex, index, group.Selected));
            }

            across += width;
            columnIndex++;
        }

        return new PlacedSide(across, marker);
    }

    // The room less the padding the owner asked for, the same on every side, which keeps the marker apart from the
    // windows around it and centred between them.
    private static LayoutRect Padded(LayoutRect room, DockingMetrics metrics)
    {
        var across = Math.Min(metrics.DropMarkerPadding, room.Width / 2);
        var down = Math.Min(metrics.DropMarkerPadding, room.Height / 2);
        return new LayoutRect(room.X + across, room.Y + down, room.Width - across * 2, room.Height - down * 2);
    }

    // How much of the app window's width the side takes, and where its drop marker goes, if it has one.
    private readonly record struct PlacedSide(double Taken, LayoutRect? DropMarker);

    private static double OuterMargin(SideLayout side, DockingMetrics metrics) =>
        side.Kind == SideKind.Widgets ? metrics.Margin : 0;

    private static double MinimumLength(WindowGroup group, LayoutMeasurements measurements, DockingMetrics metrics) =>
        group.WindowIds
            .Select(id => measurements.MinimumWindowLengths.GetValueOrDefault(id, metrics.DefaultMinimumWindowLength))
            .DefaultIfEmpty(metrics.DefaultMinimumWindowLength)
            .Max();

    // The one place that knows which way a side runs. A horizontal dock would only have to add its cases here,
    // and to the canvas region above.
    private static LayoutRect OnEdge(SideEdge edge, LayoutSize whole, double acrossStart, double acrossLength,
        double alongStart, double alongLength) => edge switch
        {
            SideEdge.Left => new LayoutRect(acrossStart, alongStart, acrossLength, alongLength),
            SideEdge.Right => new LayoutRect(whole.Width - acrossStart - acrossLength, alongStart, acrossLength, alongLength),
            _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null),
        };

    // A toolbar only takes a band from the edge it lies along. Sub-toolbars and floating items never count.
    private static LayoutInsets SafeArea(LayoutRect region, LayoutSize available, IReadOnlyList<SolvedToolbar> toolbars,
        double margin)
    {
        var left = region.X;
        var right = available.Width - region.Right;
        double top = 0, bottom = 0, leftBand = 0, rightBand = 0;

        foreach (var toolbar in toolbars)
        {
            var bounds = toolbar.Bounds;
            if (toolbar.Orientation == ToolbarOrientation.Horizontal)
            {
                if (toolbar.Anchor is ToolbarAnchor.TopLeft or ToolbarAnchor.TopMiddle or ToolbarAnchor.TopRight)
                    top = Math.Max(top, margin + bounds.Height);
                else if (toolbar.Anchor is ToolbarAnchor.BottomLeft or ToolbarAnchor.BottomMiddle or ToolbarAnchor.BottomRight)
                    bottom = Math.Max(bottom, margin + bounds.Height);
            }
            else if (toolbar.Anchor is ToolbarAnchor.TopLeft or ToolbarAnchor.LeftMiddle or ToolbarAnchor.BottomLeft)
            {
                leftBand = Math.Max(leftBand, margin + bounds.Width);
            }
            else if (toolbar.Anchor is ToolbarAnchor.TopRight or ToolbarAnchor.RightMiddle or ToolbarAnchor.BottomRight)
            {
                rightBand = Math.Max(rightBand, margin + bounds.Width);
            }
        }

        return new LayoutInsets(left + leftBand, top, right + rightBand, bottom);
    }
}
