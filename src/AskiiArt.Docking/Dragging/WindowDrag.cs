// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>What dropping on a window drop zone does.</summary>
public enum WindowDropKind
{
    /// <summary>Nothing changes: this is where the window already is, alone on its side.</summary>
    OwnPlace,

    /// <summary>The window becomes the only widget on a side that's empty.</summary>
    EmptySide,

    /// <summary>The window joins a side's stack of widgets.</summary>
    Stack,
}

/// <summary>A place on screen where dropping a window puts it on a side.</summary>
/// <param name="Kind">What dropping there does.</param>
/// <param name="Edge">The side it's on.</param>
/// <param name="Position">For a stack, where the window goes among the others.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record WindowDropZone(WindowDropKind Kind, SideEdge Edge, int Position, LayoutRect Bounds);

/// <summary>What dropping a window with the pointer at some position would do, and how to show that.</summary>
/// <param name="Layout">The layout to show around the window.</param>
/// <param name="Zone">The zone the pointer is over, if any.</param>
/// <param name="Placeholder">Where the hatched placeholder goes, if the window would land on a side.</param>
/// <param name="Held">Where the window is while it's held: under the pointer, even past the app window's edge.</param>
/// <param name="Landing">
/// Where it would float, if that isn't where it's held, because floating windows stay inside. Null over a zone.
/// </param>
public sealed record WindowDragPreview(
    DockingLayout Layout,
    WindowDropZone? Zone,
    LayoutRect? Placeholder,
    LayoutRect Held,
    LayoutRect? Landing);

/// <summary>
/// One drag of a window, as data (docs/DOCKING.md §5.2). The zones are worked out once, from the layout without the
/// window, which is what's on screen whenever it isn't over one. So they don't move as the windows make room for it.
/// The window follows the pointer anywhere, like a toolbar, and a floating drop moves it back inside.
/// </summary>
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
    private readonly (SideEdge Edge, int Position)? _home;
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

        _home = HomeOf(start, windowId);
        _floatingSize = WindowFloating.SizeWhenFloated(start, windowId, place.Bounds, place.IsTitleOnly, measurements, metrics);
        _ownWidth = WindowFloating.WantedWidth(start, windowId) ?? place.Bounds.Width;

        // A window alone on its side takes the size it would float at straight away, so that it can follow the pointer.
        HeldSize = WindowFloating.IsAloneOnItsSide(start, windowId)
            ? _floatingSize
            : new LayoutSize(place.Bounds.Width, place.Bounds.Height);

        // As far along the title as before, and as far down it in pixels, so the pointer stays on the title.
        _grabOffset = new LayoutPoint(grab.X * HeldSize.Width, Math.Min(grab.Y * place.Bounds.Height, HeldSize.Height));

        var others = LayoutSolver.Solve(LayoutEdits.WithoutWindow(start, windowId), measurements, metrics);
        _otherFloatingWindows = [.. others.FloatingWindows.Select(window => window.Bounds)];

        Zones = [.. ZonesFor(SideEdge.Left, others, place.Bounds), .. ZonesFor(SideEdge.Right, others, place.Bounds)];
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

        switch (zone.Kind)
        {
            case WindowDropKind.EmptySide:
                return LayoutEdits.MoveWindowToEmptySide(Start, _windowId, zone.Edge, _ownWidth);
            case WindowDropKind.Stack when _home != (zone.Edge, zone.Position):
                return LayoutEdits.MoveWindowIntoStack(Start, _windowId, zone.Edge, zone.Position);
            default:
                return Start;
        }
    }

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do, and how to show that.</summary>
    public WindowDragPreview PreviewAt(LayoutPoint position)
    {
        var held = HeldAt(position);

        if (ZoneAt(position) is not { } zone)
        {
            var floated = Floated(position);
            var landing = LayoutSolver.Solve(floated, _measurements, _metrics).PlaceOfWindow(_windowId)?.Bounds;
            return new WindowDragPreview(floated, null, null, held, landing is { } bounds && !IsSamePlace(bounds, held) ? bounds : null);
        }

        // An empty side only opens a little until the drop, as the owner's picture shows it.
        var layout = zone.Kind == WindowDropKind.EmptySide
            ? LayoutEdits.MoveWindowToEmptySide(Start, _windowId, zone.Edge, _metrics.WindowEdgePlaceholderWidth)
            : LayoutAt(position);

        var place = LayoutSolver.Solve(layout, _measurements, _metrics).PlaceOfWindow(_windowId);
        return new WindowDragPreview(layout, zone, place?.Bounds, held, null);
    }

    private static bool IsAnEdge(WindowDropZone zone) => zone.Kind is WindowDropKind.EmptySide or WindowDropKind.OwnPlace;

    private static bool IsSamePlace(LayoutRect first, LayoutRect second) =>
        Math.Abs(first.X - second.X) < SamePlaceTolerance && Math.Abs(first.Y - second.Y) < SamePlaceTolerance &&
        Math.Abs(first.Width - second.Width) < SamePlaceTolerance &&
        Math.Abs(first.Height - second.Height) < SamePlaceTolerance;

    private static (SideEdge Edge, int Position)? HomeOf(DockingLayout layout, string windowId)
    {
        foreach (var side in layout.Sides)
        {
            var groups = side.Columns.SelectMany(column => column.Groups).ToList();
            var position = groups.FindIndex(group => group.WindowIds.Contains(windowId, StringComparer.Ordinal));
            if (position >= 0)
                return (side.Edge, position);
        }

        return null;
    }

    private List<WindowDropZone> ZonesFor(SideEdge edge, SolvedLayout others, LayoutRect held)
    {
        var groups = others.WindowGroups.Where(group => group.Edge == edge).OrderBy(group => group.Bounds.Y).ToList();

        if (groups.Count == 0)
            return _home?.Edge == edge ? [OwnPlaceZone(edge, held)] : [EmptySideZone(edge)];

        // Docks arrive in step 2.5f.
        if (groups[0].Kind != SideKind.Widgets || groups.Count >= DockingLayout.MaximumWidgetsOnASide)
            return [];

        return StackZones(edge, groups);
    }

    private WindowDropZone OwnPlaceZone(SideEdge edge, LayoutRect held) =>
        new(WindowDropKind.OwnPlace, edge, 0, Across(edge, held, 0, _available.Height));

    private WindowDropZone EmptySideZone(SideEdge edge)
    {
        var strip = _metrics.WindowEdgeStripWidth;
        var x = edge == SideEdge.Left ? 0 : _available.Width - strip;
        return new WindowDropZone(WindowDropKind.EmptySide, edge, 0, new LayoutRect(x, 0, strip, _available.Height));
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
}
