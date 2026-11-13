// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>What dropping on a window drop zone does (docs/DOCKING.md §5.2).</summary>
public enum WindowDropKind
{
    /// <summary>
    /// Nothing changes: this is the edge strip of the side the window is alone on, which looks empty while it's held.
    /// </summary>
    OwnPlace,

    /// <summary>The window becomes the only widget on a side that's empty.</summary>
    EmptySide,

    /// <summary>
    /// The window becomes a dock column at the very edge of a side with windows. A widget side becomes a dock, its
    /// widgets one column beside the window.
    /// </summary>
    SideEdge,

    /// <summary>The window joins a side's stack of widgets.</summary>
    Stack,

    /// <summary>The window becomes a tab in a dock window.</summary>
    Tab,

    /// <summary>The window stacks above or below a dock window, in its column.</summary>
    DockStack,

    /// <summary>The window becomes a new dock column beside a dock window's column.</summary>
    DockColumn,
}

/// <summary>A place on screen where dropping a window puts it on a side.</summary>
/// <param name="Kind">What dropping there does.</param>
/// <param name="Edge">The side it's on.</param>
/// <param name="Position">
/// Where the window goes among the others in a stack, or the place in its column of the dock window it becomes a tab of.
/// </param>
/// <param name="Bounds">Its rectangle.</param>
/// <param name="Column">The dock column it goes in, or for a new column, where that goes, from the app window's edge.</param>
/// <param name="Tab">For a tab, where it goes among the others.</param>
public sealed record WindowDropZone(
    WindowDropKind Kind,
    SideEdge Edge,
    int Position,
    LayoutRect Bounds,
    int Column = 0,
    int Tab = 0);

/// <summary>What dropping a window with the pointer at some position would do, and how to show that.</summary>
/// <param name="Layout">The layout to show around the window: without it, wherever the pointer is.</param>
/// <param name="Zone">The zone the pointer is over, if any.</param>
/// <param name="Room">
/// The room to make in <paramref name="Layout"/> for the marker, over a zone that doesn't make the window a tab. The solver
/// leaves it empty (<see cref="LayoutMeasurements.DropRoom"/>).
/// </param>
/// <param name="Marker">The hatched strip showing roughly where the window would go, in that room.</param>
/// <param name="IncomingTab">Where it would go as a tab, over a zone that makes it one.</param>
/// <param name="Held">Where the window is while it's held: under the pointer, even past the app window's edge.</param>
/// <param name="Landing">
/// Where it would float, if that isn't where it's held, because floating windows stay inside. Null over a zone.
/// </param>
public sealed record WindowDragPreview(
    DockingLayout Layout,
    WindowDropZone? Zone,
    DropRoom? Room,
    LayoutRect? Marker,
    IncomingTab? IncomingTab,
    LayoutRect Held,
    LayoutRect? Landing);

/// <summary>A tab that a drop would make in a dock window, shown in that window's tab strip.</summary>
/// <param name="ShowingWindowId">The window showing in the group it would join, whose tab strip shows where it goes.</param>
/// <param name="Index">Where it would go among the group's tabs.</param>
/// <param name="GroupBounds">The group's rectangle, outlined while the tab is held there.</param>
public sealed record IncomingTab(string ShowingWindowId, int Index, LayoutRect GroupBounds);

/// <summary>
/// One drag of a window, as data (docs/DOCKING.md §5.2). The zones are worked out once, from the layout without the
/// window, and stay put while the windows move apart to make room for a marker showing roughly where it would go. The
/// window follows the pointer anywhere, like a toolbar, and a floating drop moves it back inside.
/// </summary>
/// <remarks>
/// A zone's numbers count the windows and columns there would be without the dragged one. The zone that would put it
/// back exactly where it was changes nothing, rather than redoing its arrival, which would share out the room afresh.
/// </remarks>
public sealed class WindowDrag
{
    // Less than this apart, the window lands where it's held, and there's no need to show where.
    private const double SamePlaceTolerance = 0.5;

    private readonly string _windowId;
    private readonly LayoutPoint _grabOffset;
    private readonly LayoutSize _available;
    private readonly LayoutMeasurements _measurements;
    private readonly DockingMetrics _metrics;
    private readonly LayoutSize _floatingSize;
    private readonly double _ownWidth;
    private readonly WindowPlace? _place;
    private readonly Arrival? _home;
    private readonly DockingLayout _without;
    private readonly SolvedLayout _others;
    private readonly IReadOnlyList<LayoutRect> _otherFloatingWindows;

    /// <summary>Starts a drag.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="windowId">The window being dragged, which must be open.</param>
    /// <param name="grab">Where on the window the pointer holds it, from 0 to 1 along each axis of its size now.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public WindowDrag(DockingLayout start, string windowId, LayoutPoint grab, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        Start = start;
        _windowId = windowId;
        _available = measurements.Available;
        _measurements = measurements;
        _metrics = metrics;

        var solved = LayoutSolver.Solve(start, measurements, metrics);
        var place = solved.PlaceOfWindow(windowId)
            ?? throw new ArgumentException("Only an open window can be dragged.", nameof(windowId));

        _place = start.PlaceOf(windowId);
        _home = HomeOf(start, _place);
        _floatingSize = WindowFloating.SizeWhenFloated(start, windowId, place.Bounds, place.IsTitleOnly, measurements, metrics);
        _ownWidth = WindowFloating.WantedWidth(start, windowId) ?? place.Bounds.Width;

        // A window alone on its side takes the size it would float at straight away, so that it can follow the pointer.
        HeldSize = WindowFloating.FillsItsColumn(start, windowId)
            ? _floatingSize
            : new LayoutSize(place.Bounds.Width, place.Bounds.Height);

        // As far along the title as before, and as far down it in pixels, so the pointer stays on the title.
        _grabOffset = new LayoutPoint(grab.X * HeldSize.Width, Math.Min(grab.Y * place.Bounds.Height, HeldSize.Height));

        _without = LayoutEdits.WithoutWindow(start, windowId);
        _others = LayoutSolver.Solve(_without, measurements, metrics);
        _otherFloatingWindows = [.. _others.FloatingWindows.Select(window => window.Bounds)];

        Zones = [.. ZonesFor(SideEdge.Left), .. ZonesFor(SideEdge.Right)];
    }

    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    public DockingLayout Start { get; }

    /// <summary>The size the window has while it's held.</summary>
    public LayoutSize HeldSize { get; }

    /// <summary>Everywhere a drop puts the window on a side. Dropping anywhere else floats it.</summary>
    public IReadOnlyList<WindowDropZone> Zones { get; }

    /// <summary>
    /// The zone at <paramref name="position"/>, if any. A pointer over another floating window has no zone, because a drop
    /// there floats the window on top. Past the app window's edge only a side that's empty without the window has one,
    /// because its zone is the edge itself. Anywhere else out there, a drop floats the window back inside.
    /// </summary>
    public WindowDropZone? ZoneAt(LayoutPoint position)
    {
        if (_otherFloatingWindows.Any(window => window.Contains(position)))
            return null;

        var atTheEdge = new LayoutPoint(Math.Clamp(position.X, 0, Math.Max(0, _available.Width - 1)), position.Y);
        return Zones.FirstOrDefault(zone => zone.Bounds.Contains(IsAnEdge(zone) ? atTheEdge : position));
    }

    /// <summary>Where the window is while it's held with the pointer at <paramref name="position"/>.</summary>
    public LayoutRect HeldAt(LayoutPoint position) =>
        new(position.X - _grabOffset.X, position.Y - _grabOffset.Y, HeldSize.Width, HeldSize.Height);

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do.</summary>
    public DockingLayout LayoutAt(LayoutPoint position)
    {
        if (ZoneAt(position) is not { } zone)
            return Floated(position);

        if (ArrivalAt(zone) == _home)
            return Start;

        return zone.Kind switch
        {
            WindowDropKind.EmptySide => LayoutEdits.MoveWindowToEmptySide(Start, _windowId, zone.Edge, _ownWidth),
            WindowDropKind.SideEdge or WindowDropKind.DockColumn =>
                LayoutEdits.MoveWindowToNewColumn(Start, _windowId, zone.Edge, zone.Column, NewColumnWidth(zone.Edge)),
            WindowDropKind.Stack => LayoutEdits.MoveWindowIntoStack(Start, _windowId, zone.Edge, zone.Position),
            WindowDropKind.DockStack =>
                LayoutEdits.MoveWindowIntoColumn(Start, _windowId, zone.Edge, zone.Column, zone.Position),
            WindowDropKind.Tab =>
                LayoutEdits.MoveWindowIntoTabs(Start, _windowId, zone.Edge, zone.Column, zone.Position, zone.Tab),
            _ => Start,
        };
    }

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do, and how to show that.</summary>
    public WindowDragPreview PreviewAt(LayoutPoint position)
    {
        var held = HeldAt(position);
        var floated = Floated(position);

        if (ZoneAt(position) is not { } zone)
        {
            var landing = LayoutSolver.Solve(floated, _measurements, _metrics).PlaceOfWindow(_windowId)?.Bounds;
            var outside = landing is { } bounds && !IsSamePlace(bounds, held) ? landing : null;
            return new WindowDragPreview(floated, null, null, null, null, held, outside);
        }

        if (zone.Kind == WindowDropKind.Tab)
            return new WindowDragPreview(floated, zone, null, null, IncomingTabAt(zone), held, null);

        var room = RoomFor(zone);
        var marker = LayoutSolver.Solve(floated, _measurements with { DropRoom = room }, _metrics).DropMarker;
        return new WindowDragPreview(floated, zone, room, marker, null, held, null);
    }

    private static bool IsAnEdge(WindowDropZone zone) => zone.Kind is WindowDropKind.EmptySide or WindowDropKind.OwnPlace;

    private static bool IsSamePlace(LayoutRect first, LayoutRect second) =>
        Math.Abs(first.X - second.X) < SamePlaceTolerance && Math.Abs(first.Y - second.Y) < SamePlaceTolerance &&
        Math.Abs(first.Width - second.Width) < SamePlaceTolerance &&
        Math.Abs(first.Height - second.Height) < SamePlaceTolerance;

    // The arrival that would put the window back where it was. A window alone on its side has none, because without it
    // the side is empty, and its own place is a zone of its own.
    private static Arrival? HomeOf(DockingLayout layout, WindowPlace? place)
    {
        if (place is not { } at)
            return null;

        if (at.Kind == SideKind.Widgets)
            return new Arrival(WindowDropKind.Stack, at.Edge, 0, at.Position, 0);

        var column = layout.Side(at.Edge)!.Columns[at.Column];
        if (column.Groups[at.Position].WindowIds.Count > 1)
            return new Arrival(WindowDropKind.Tab, at.Edge, at.Column, at.Position, at.Tab);

        return column.Groups.Count > 1
            ? new Arrival(WindowDropKind.DockStack, at.Edge, at.Column, at.Position, 0)
            : new Arrival(WindowDropKind.DockColumn, at.Edge, at.Column, 0, 0);
    }

    // A new column at the edge is the same arrival however it's reached.
    private static Arrival ArrivalAt(WindowDropZone zone) => zone.Kind == WindowDropKind.SideEdge
        ? new Arrival(WindowDropKind.DockColumn, zone.Edge, 0, 0, 0)
        : new Arrival(zone.Kind, zone.Edge, zone.Column, zone.Position, zone.Tab);

    // In the order of docs/DOCKING.md §5.2's table, since the first zone the pointer is over wins.
    private List<WindowDropZone> ZonesFor(SideEdge edge)
    {
        var groups = GroupsOn(edge);

        if (groups.Count == 0 && _place?.Edge != edge)
            return [EdgeStripZone(WindowDropKind.EmptySide, edge)];

        if (groups.Count == 0)
            return [EdgeStripZone(WindowDropKind.OwnPlace, edge)];

        var zones = new List<WindowDropZone>();
        var columnFits = LayoutSolver.HasRoomForANewColumn(_without, edge, _available.Width, _metrics);
        if (columnFits)
            zones.Add(EdgeStripZone(WindowDropKind.SideEdge, edge));

        if (groups[0].Kind == SideKind.Widgets)
        {
            if (groups.Count < DockingLayout.MaximumWidgetsOnASide)
                zones.AddRange(StackZones(edge, groups));

            return zones;
        }

        if (columnFits)
            zones.AddRange(ColumnBorderZones(edge));

        foreach (var group in groups)
            zones.AddRange(DockGroupZones(group));

        return zones;
    }

    private List<SolvedWindowGroup> GroupsOn(SideEdge edge) =>
        [.. _others.WindowGroups.Where(group => group.Edge == edge).OrderBy(group => group.Bounds.Y)];

    // A dock column's groups, from the top.
    private List<SolvedWindowGroup> GroupsIn(SideEdge edge, int column) =>
        [.. _others.WindowGroups.Where(group => group.Edge == edge && group.Column == column).OrderBy(group => group.Position)];

    // Each dock column's rectangle, from the app window's edge.
    private List<LayoutRect> ColumnsOn(SideEdge edge) =>
    [
        .. _others.WindowGroups
            .Where(group => group.Edge == edge)
            .GroupBy(group => group.Column)
            .OrderBy(column => column.Key)
            .Select(column => new LayoutRect(column.First().Bounds.X, 0, column.First().Bounds.Width, _available.Height)),
    ];

    // The edge of a column on the canvas side.
    private static double InnerEdge(SideEdge edge, LayoutRect column) => edge == SideEdge.Left ? column.Right : column.X;

    // A zone straddling each border between columns, and the dock's inner edge, the full height. A drop there makes a
    // column there. The outer edge has the side's edge strip instead.
    private IEnumerable<WindowDropZone> ColumnBorderZones(SideEdge edge)
    {
        var columns = ColumnsOn(edge);
        for (var column = 1; column <= columns.Count; column++)
        {
            var border = InnerEdge(edge, columns[column - 1]);
            yield return new WindowDropZone(WindowDropKind.DockColumn, edge, 0,
                VerticalStrip(border, _metrics.DockBorderZoneThickness), column);
        }
    }

    private WindowDropZone EdgeStripZone(WindowDropKind kind, SideEdge edge)
    {
        var strip = _metrics.WindowEdgeStripWidth;
        var x = edge == SideEdge.Left ? 0 : _available.Width - strip;
        return new WindowDropZone(kind, edge, 0, new LayoutRect(x, 0, strip, _available.Height));
    }

    // Along the tab strip, a slot for each place a tab could go. Below it, a band along the top of the column's first
    // window and the bottom of its last stacks the window there, and a thinner one along the bottom of any other puts it
    // between that one and the next. That one stays above the border, so the next window's tab strip keeps its whole
    // height, unless this window shows only its tab strip. The rest makes it a tab at the end, or back where it was if
    // it came from here.
    private List<WindowDropZone> DockGroupZones(SolvedWindowGroup group)
    {
        var bounds = group.Bounds;
        var zones = TabZones(group, new LayoutRect(bounds.X, bounds.Y, bounds.Width, StripHeight(group)));
        var body = BodyOf(group);
        if (body.Height <= 0)
            return zones;

        var edge = group.Edge;
        var column = GroupsIn(edge, group.Column);
        var isFirst = group.Position == 0;
        var isLast = group.Position == column.Count - 1;
        var band = Math.Min(_metrics.DockBandLength, body.Height / 4);
        var between = Math.Min(_metrics.DockBorderZoneThickness, body.Height / 4);

        if (isFirst)
        {
            zones.Add(new WindowDropZone(WindowDropKind.DockStack, edge, 0,
                new LayoutRect(body.X, body.Y, body.Width, band), group.Column));
        }
        else if (BodyOf(column[group.Position - 1]).Height <= 0)
        {
            zones.Add(new WindowDropZone(WindowDropKind.DockStack, edge, group.Position,
                new LayoutRect(body.X, body.Y, body.Width, between), group.Column));
        }

        var below = isLast ? band : between;
        zones.Add(new WindowDropZone(WindowDropKind.DockStack, edge, group.Position + 1,
            new LayoutRect(body.X, body.Bottom - below, body.Width, below), group.Column));

        var tab = _home is { Kind: WindowDropKind.Tab } home && home.Edge == edge && home.Column == group.Column &&
            home.Position == group.Position
            ? home.Tab
            : group.WindowIds.Count;
        zones.Add(new WindowDropZone(WindowDropKind.Tab, edge, group.Position, body, group.Column, tab));
        return zones;
    }

    private double StripHeight(SolvedWindowGroup group) => Math.Min(_metrics.TitleLength, group.Bounds.Height);

    // Everything below the tab strip.
    private LayoutRect BodyOf(SolvedWindowGroup group)
    {
        var bounds = group.Bounds;
        var strip = StripHeight(group);
        return new LayoutRect(bounds.X, bounds.Y + strip, bounds.Width, bounds.Height - strip);
    }

    private IncomingTab IncomingTabAt(WindowDropZone zone)
    {
        var group = GroupsIn(zone.Edge, zone.Column)[zone.Position];
        return new IncomingTab(group.Selected, zone.Tab, group.Bounds);
    }

    // docs/DOCKING.md §5.2: the windows make room for a marker where the window would go. It's thin on a border in a dock,
    // and elsewhere as deep as a dock's bands, so it shows how big its zone is. Among a dock's windows, or where a column
    // would go, the room is wider by the padding each way, so the marker keeps the same distance from everything.
    private DropRoom? RoomFor(WindowDropZone zone)
    {
        var standard = _metrics.DockBandLength;
        var padding = _metrics.DropMarkerPadding * 2;
        var thin = _metrics.ThinDropMarkerThickness + padding;
        return zone.Kind switch
        {
            WindowDropKind.EmptySide or WindowDropKind.OwnPlace => new DropRoom(DropRoomKind.EmptySide, zone.Edge, standard),
            WindowDropKind.SideEdge or WindowDropKind.DockColumn =>
                new DropRoom(DropRoomKind.Column, zone.Edge, thin, zone.Column),
            WindowDropKind.Stack => new DropRoom(DropRoomKind.Stack, zone.Edge, standard, 0, zone.Position),
            WindowDropKind.DockStack => new DropRoom(DropRoomKind.Stack, zone.Edge,
                IsAtAColumnsEnd(zone) ? standard + padding : thin, zone.Column, zone.Position),
            _ => null,
        };
    }

    private bool IsAtAColumnsEnd(WindowDropZone zone) =>
        zone.Position == 0 || zone.Position == GroupsIn(zone.Edge, zone.Column).Count;

    // The full height, centred on a line down the app window, kept inside it.
    private LayoutRect VerticalStrip(double middle, double thickness) =>
        new(Math.Clamp(middle - thickness / 2, 0, Math.Max(0, _available.Width - thickness)), 0, thickness,
            _available.Height);

    // Each slot reaches from the middle of one tab to the middle of the next, so a drop goes between the two tabs nearest
    // the pointer. The last reaches to the end of the strip.
    private List<WindowDropZone> TabZones(SolvedWindowGroup group, LayoutRect strip)
    {
        var spans = DockTabs.Spans(group.WindowIds, strip, _measurements);
        var zones = new List<WindowDropZone>();
        var from = strip.X;

        for (var tab = 0; tab <= spans.Count; tab++)
        {
            var to = tab < spans.Count ? Math.Clamp(spans[tab].Middle, strip.X, strip.Right) : strip.Right;
            if (to > from)
            {
                zones.Add(new WindowDropZone(WindowDropKind.Tab, group.Edge, group.Position,
                    new LayoutRect(from, strip.Y, to - from, strip.Height), group.Column, tab));
            }

            from = Math.Max(from, to);
        }

        return zones;
    }

    // Kept within what fits, and never narrower than a window can be, since the zone is only offered if that fits.
    private double NewColumnWidth(SideEdge edge)
    {
        var room = LayoutSolver.RoomForANewColumn(_without, edge, _available.Width, _metrics);
        return Math.Clamp(_ownWidth, _metrics.MinimumWindowWidth, Math.Max(_metrics.MinimumWindowWidth, room));
    }

    // The upper and lower half of each window, reaching to the middle of the gaps and the ends of the side, so there's
    // nowhere along the stack that a drop would miss.
    private List<WindowDropZone> StackZones(SideEdge edge, List<SolvedWindowGroup> groups)
    {
        var zones = new List<WindowDropZone>();
        for (var index = 0; index < groups.Count; index++)
        {
            var bounds = groups[index].Bounds;
            var top = index == 0 ? 0 : (groups[index - 1].Bounds.Bottom + bounds.Y) / 2;
            var bottom = index == groups.Count - 1 ? _available.Height : (bounds.Bottom + groups[index + 1].Bounds.Y) / 2;
            var middle = bounds.Y + bounds.Height / 2;

            zones.Add(new WindowDropZone(WindowDropKind.Stack, edge, index, Across(edge, bounds, top, middle)));
            zones.Add(new WindowDropZone(WindowDropKind.Stack, edge, index + 1, Across(edge, bounds, middle, bottom)));
        }

        return zones;
    }

    // From the very edge of the app window to the inner edge of the window on that side.
    private LayoutRect Across(SideEdge edge, LayoutRect window, double top, double bottom) =>
        edge == SideEdge.Left
            ? new LayoutRect(0, top, window.Right, bottom - top)
            : new LayoutRect(window.X, top, _available.Width - window.X, bottom - top);

    // Kept inside where it's stored, as well as where it's shown, so that growing the app window doesn't move it to
    // where it was let go outside.
    private DockingLayout Floated(LayoutPoint position)
    {
        var held = HeldAt(position);
        var area = LayoutSolver.FloatingWindowArea(_available, _metrics);

        // A floating window that's only being moved is left as it was.
        if (Start.FloatingWindowNamed(_windowId) is { } floating)
        {
            var at = LayoutSolver.KeptInside(new LayoutPoint(held.X, held.Y), HeldSize, area);
            return LayoutEdits.FloatWindow(Start, _windowId, at, floating.Width, floating.Height, floating.IsTitleOnly);
        }

        var landsAt = LayoutSolver.KeptInside(new LayoutPoint(held.X, held.Y), _floatingSize, area);
        return LayoutEdits.FloatWindow(Start, _windowId, landsAt, _floatingSize.Width, _floatingSize.Height);
    }

    private readonly record struct Arrival(WindowDropKind Kind, SideEdge Edge, int Column, int Position, int Tab);
}
