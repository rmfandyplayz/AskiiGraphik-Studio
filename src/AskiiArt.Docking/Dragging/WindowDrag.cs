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
public sealed record WindowDragPreview(DockingLayout Layout, WindowDropZone? Zone, LayoutRect? Placeholder);

/// <summary>
/// One drag of a window, as data (docs/DOCKING.md §5.2). The zones are worked out once, from the layout without the
/// window, which is what's on screen whenever it isn't over one. So they don't move as the windows make room for it.
/// </summary>
public sealed class WindowDrag
{
    private readonly string _windowId;
    private readonly LayoutPoint _grab;
    private readonly LayoutSize _available;
    private readonly LayoutMeasurements _measurements;
    private readonly DockingMetrics _metrics;
    private readonly bool _wasTitleOnly;
    private readonly (SideEdge Edge, int Position)? _home;
    private readonly IReadOnlyList<LayoutRect> _otherFloatingWindows;

    /// <summary>Starts a drag.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="windowId">The window being dragged, which must be open.</param>
    /// <param name="grab">Where on the window the pointer holds it, from 0 to 1 along each axis.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public WindowDrag(DockingLayout start, string windowId, LayoutPoint grab, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        Start = start;
        _windowId = windowId;
        _grab = grab;
        _available = measurements.Available;
        _measurements = measurements;
        _metrics = metrics;

        var solved = LayoutSolver.Solve(start, measurements, metrics);
        var place = solved.PlaceOfWindow(windowId)
            ?? throw new ArgumentException("Only an open window can be dragged.", nameof(windowId));

        HeldSize = new LayoutSize(place.Bounds.Width, place.Bounds.Height);
        _wasTitleOnly = place.IsTitleOnly;
        _home = HomeOf(start, windowId);

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
    /// The zone at <paramref name="position"/>, if any. A pointer past the app window's edge counts as at that edge, and
    /// one over another floating window has no zone, because a drop there floats the window on top.
    /// </summary>
    public WindowDropZone? ZoneAt(LayoutPoint position)
    {
        var inside = new LayoutPoint(
            Math.Clamp(position.X, 0, Math.Max(0, _available.Width - 1)),
            Math.Clamp(position.Y, 0, Math.Max(0, _available.Height - 1)));

        return _otherFloatingWindows.Any(window => window.Contains(inside))
            ? null
            : Zones.FirstOrDefault(zone => zone.Bounds.Contains(inside));
    }

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do.</summary>
    public DockingLayout LayoutAt(LayoutPoint position)
    {
        if (ZoneAt(position) is not { } zone)
            return Floated(position);

        switch (zone.Kind)
        {
            case WindowDropKind.EmptySide:
                return LayoutEdits.MoveWindowToEmptySide(Start, _windowId, zone.Edge, _metrics.DefaultWindowWidth);
            case WindowDropKind.Stack when _home != (zone.Edge, zone.Position):
                return LayoutEdits.MoveWindowIntoStack(Start, _windowId, zone.Edge, zone.Position);
            default:
                return Start;
        }
    }

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do, and how to show that.</summary>
    public WindowDragPreview PreviewAt(LayoutPoint position)
    {
        if (ZoneAt(position) is not { } zone)
            return new WindowDragPreview(Floated(position), null, null);

        // An empty side only opens a little until the drop, as the owner's picture shows it.
        var layout = zone.Kind == WindowDropKind.EmptySide
            ? LayoutEdits.MoveWindowToEmptySide(Start, _windowId, zone.Edge, _metrics.WindowEdgePlaceholderWidth)
            : LayoutAt(position);

        var place = LayoutSolver.Solve(layout, _measurements, _metrics).PlaceOfWindow(_windowId);
        return new WindowDragPreview(layout, zone, place?.Bounds);
    }

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

    private DockingLayout Floated(LayoutPoint position)
    {
        var x = position.X - _grab.X * HeldSize.Width;
        var y = position.Y - _grab.Y * HeldSize.Height;
        var at = new LayoutPoint(
            Math.Max(0, Math.Min(x, _available.Width - HeldSize.Width)),
            Math.Max(0, Math.Min(y, _available.Height - HeldSize.Height)));

        // A floating window that's only being moved is left as it was.
        if (Start.FloatingWindowNamed(_windowId) is { } floating)
            return LayoutEdits.FloatWindow(Start, _windowId, at, floating.Width, floating.Height, floating.IsTitleOnly);

        var height = _wasTitleOnly ? NormalHeight() : HeldSize.Height;
        return LayoutEdits.FloatWindow(Start, _windowId, at, HeldSize.Width, height);
    }

    private double NormalHeight() => Math.Max(_metrics.DefaultFloatingWindowHeight,
        _measurements.MinimumWindowLengths.GetValueOrDefault(_windowId, _metrics.DefaultMinimumWindowLength));
}
