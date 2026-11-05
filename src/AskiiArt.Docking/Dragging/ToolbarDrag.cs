// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>A place on screen where dropping a toolbar puts it on an anchor.</summary>
/// <param name="Anchor">The anchor it stands for.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record ToolbarDropZone(ToolbarAnchor Anchor, LayoutRect Bounds);

/// <summary>The size a toolbar wants, each way round. A drag can turn it, so it needs to know both.</summary>
/// <param name="Horizontal">Its size when it runs across.</param>
/// <param name="Vertical">Its size when it runs up and down.</param>
public readonly record struct ToolbarShapes(LayoutSize Horizontal, LayoutSize Vertical)
{
    /// <summary>Its size when it runs this way.</summary>
    public LayoutSize For(ToolbarOrientation orientation) =>
        orientation == ToolbarOrientation.Horizontal ? Horizontal : Vertical;
}

/// <summary>What dropping with the pointer at some position would do, and how to show that.</summary>
/// <param name="Layout">The layout a drop would give.</param>
/// <param name="Landing">Where the toolbar would land, if that's worth showing: not if it's right under the pointer's grip.</param>
/// <param name="CoveredZones">The zones under the landing place, which get out of its way.</param>
/// <param name="ReservedSpots">What the other toolbars have to keep clear of while this shows.</param>
public sealed record ToolbarDragPreview(
    DockingLayout Layout,
    LayoutRect? Landing,
    IReadOnlySet<ToolbarAnchor> CoveredZones,
    IReadOnlyList<ReservedToolbarSpot> ReservedSpots);

/// <summary>
/// One drag of a toolbar, as data: what's a drop zone, and what a drop would do for any pointer position. A preview
/// and a drop both come from <see cref="LayoutAt"/>, so they can't disagree. The zones are worked out once, when the
/// drag starts, so they don't move as the layout shifts under the pointer (docs/DOCKING.md §5).
/// </summary>
public sealed class ToolbarDrag
{
    // Closer than this, the landing place is just the toolbar's own shadow.
    private const double SameSpotTolerance = 0.5;

    private readonly string _toolbarId;
    private readonly LayoutPoint _grab;
    private readonly LayoutSize _heldSize;
    private readonly ToolbarShapes _shapes;
    private readonly LayoutMeasurements _measurements;
    private readonly DockingMetrics _metrics;
    private readonly List<ReservedToolbarSpot> _reserved;

    /// <summary>Starts a drag.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="toolbarId">The toolbar being dragged.</param>
    /// <param name="grab">Where on the toolbar the pointer holds it, from 0 to 1 along each axis.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="shapes">The size the dragged toolbar wants, each way round.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public ToolbarDrag(DockingLayout start, string toolbarId, LayoutPoint grab, LayoutMeasurements measurements,
        ToolbarShapes shapes, DockingMetrics metrics)
    {
        Start = start;
        _toolbarId = toolbarId;
        _grab = grab;
        _shapes = shapes;
        _measurements = measurements;
        _metrics = metrics;

        var orientation = OrientationIn(start);
        _heldSize = shapes.For(orientation);

        var region = LayoutSolver.Solve(start, MeasurementsFor(orientation), metrics).CanvasRegion;
        var inner = ToolbarSolver.InnerRegion(region, metrics);
        var spots = ToolbarAnchors.All
            .Where(anchor => !start.IsAnchorTaken(anchor, toolbarId))
            .Select(anchor => SpotFor(anchor, orientation, inner))
            .ToList();

        // The dragged toolbar has left, so it holds nothing.
        var others = start with { Toolbars = [.. start.Toolbars.Where(toolbar => toolbar.ToolbarId != toolbarId)] };
        var solved = LayoutSolver.Solve(others, measurements, metrics, spots).ReservedSpots;

        _reserved = [.. spots.Where(spot => IsThereRoomFor(spot, solved))];
        Zones = [.. _reserved.Select(spot => new ToolbarDropZone(spot.Anchor, solved[spot.Anchor]))];
    }

    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    public DockingLayout Start { get; }

    /// <summary>
    /// A zone for each free anchor, and for the toolbar's own. Occupied anchors have none. Each is as thick as the
    /// toolbar would be there, and they lie among the other toolbars like toolbars, so they never overlap one.
    /// </summary>
    public IReadOnlyList<ToolbarDropZone> Zones { get; }

    /// <summary>The zone at <paramref name="position"/>, if any.</summary>
    public ToolbarDropZone? ZoneAt(LayoutPoint position) =>
        Zones.FirstOrDefault(zone => zone.Bounds.Contains(position));

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do: go on a zone's anchor, or float.</summary>
    public DockingLayout LayoutAt(LayoutPoint position)
    {
        if (ZoneAt(position) is { } zone)
            return LayoutEdits.MoveToolbarToAnchor(Start, _toolbarId, zone.Anchor);

        var floated = LayoutEdits.FloatToolbar(Start, _toolbarId, HeldTopLeft(position));

        // It saves where it lands, not where the pointer was. A toolbar dropped over a window sits beside that
        // window, and shouldn't jump under the pointer's old spot if the window closes later.
        return WhereItLands(floated) is { } landed
            ? LayoutEdits.FloatToolbar(Start, _toolbarId, new LayoutPoint(landed.X, landed.Y))
            : floated;
    }

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do, and how to show that.</summary>
    public ToolbarDragPreview PreviewAt(LayoutPoint position)
    {
        var layout = LayoutAt(position);
        var landing = WhereItLands(layout);

        if (ZoneAt(position) is not null)
        {
            // What a drop takes up, nothing has to keep clear of. So its neighbours along that edge get what they'd
            // have after the drop, and the preview's truthful about them too.
            var taken = ZonesUnder(landing);
            return new ToolbarDragPreview(layout, landing, taken,
                [.. _reserved.Where(spot => !taken.Contains(spot.Anchor))]);
        }

        // Only worth showing if something pushed it away from where the pointer holds it.
        if (landing is { } bounds && IsAt(bounds, HeldTopLeft(position)))
            landing = null;

        return new ToolbarDragPreview(layout, landing, ZonesUnder(landing), _reserved);
    }

    private ToolbarOrientation OrientationIn(DockingLayout layout) =>
        layout.Toolbars.FirstOrDefault(toolbar => toolbar.ToolbarId == _toolbarId)?.Orientation
        ?? ToolbarOrientation.Horizontal;

    private LayoutMeasurements MeasurementsFor(ToolbarOrientation orientation) => _measurements with
    {
        ToolbarSizes = new Dictionary<string, LayoutSize>(_measurements.ToolbarSizes, StringComparer.Ordinal)
        {
            [_toolbarId] = _shapes.For(orientation),
        },
    };

    // A zone is as thick as the toolbar would be on it. A corner keeps the toolbar's direction, and a middle sets it.
    private ReservedToolbarSpot SpotFor(ToolbarAnchor anchor, ToolbarOrientation current, LayoutRect inner)
    {
        var direction = ToolbarAnchors.RequiredOrientation(anchor);
        var thickness = (direction ?? current) == ToolbarOrientation.Horizontal
            ? _shapes.Horizontal.Height
            : _shapes.Vertical.Width;

        var length = direction switch
        {
            ToolbarOrientation.Horizontal => inner.Width * _metrics.ToolbarZoneMinimumShare,
            ToolbarOrientation.Vertical => inner.Height * _metrics.ToolbarZoneMinimumShare,
            _ => thickness,
        };

        return new ReservedToolbarSpot(anchor, length, thickness);
    }

    private static bool IsThereRoomFor(ReservedToolbarSpot spot,
        IReadOnlyDictionary<ToolbarAnchor, LayoutRect> solved) =>
        solved.TryGetValue(spot.Anchor, out var bounds) && bounds.Width > 0 && bounds.Height > 0;

    private LayoutPoint HeldTopLeft(LayoutPoint position) =>
        new(position.X - _grab.X * _heldSize.Width, position.Y - _grab.Y * _heldSize.Height);

    private LayoutRect? WhereItLands(DockingLayout layout) =>
        LayoutSolver.Solve(layout, MeasurementsFor(OrientationIn(layout)), _metrics).Toolbars
            .FirstOrDefault(toolbar => toolbar.ToolbarId == _toolbarId)?.Bounds;

    private HashSet<ToolbarAnchor> ZonesUnder(LayoutRect? landing) =>
        landing is { } bounds
            ? [.. Zones.Where(zone => zone.Bounds.Intersects(bounds)).Select(zone => zone.Anchor)]
            : [];

    private static bool IsAt(LayoutRect bounds, LayoutPoint topLeft) =>
        Math.Abs(bounds.X - topLeft.X) < SameSpotTolerance && Math.Abs(bounds.Y - topLeft.Y) < SameSpotTolerance;
}
