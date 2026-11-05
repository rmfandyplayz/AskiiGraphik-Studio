// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>A place on screen where dropping a toolbar puts it on an anchor.</summary>
/// <param name="Anchor">The anchor it stands for.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record ToolbarDropZone(ToolbarAnchor Anchor, LayoutRect Bounds);

/// <summary>What dropping with the pointer at some position would do, and how to show that.</summary>
/// <param name="Layout">The layout a drop would give.</param>
/// <param name="Zone">The zone the pointer is over, if any, which a dashed outline shows. A drop goes on its anchor.</param>
/// <param name="PushedBackTo">
/// Where a floating toolbar would land if that isn't where it's held, because it would have to stay inside the canvas
/// region. That shows as a copy of the toolbar in a dashed outline.
/// </param>
/// <param name="SubToolbar">Where the dragged toolbar's sub-toolbar would land, if it has one.</param>
/// <param name="CoveredZones">The zones that get out of the way of what's shown.</param>
public sealed record ToolbarDragPreview(
    DockingLayout Layout,
    ToolbarDropZone? Zone,
    LayoutRect? PushedBackTo,
    SolvedSubToolbar? SubToolbar,
    IReadOnlySet<ToolbarAnchor> CoveredZones);

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
            .Select(anchor => SpotFor(anchor, inner))
            .ToList();

        // The dragged toolbar has left, so it holds nothing.
        Others = start with { Toolbars = [.. start.Toolbars.Where(toolbar => toolbar.ToolbarId != toolbarId)] };
        var solved = LayoutSolver.Solve(Others, measurements, metrics, spots).ReservedSpots;

        ReservedSpots = [.. spots.Where(spot => IsThereRoomFor(spot, solved))];
        Zones = [.. ReservedSpots.Select(spot => new ToolbarDropZone(spot.Anchor, solved[spot.Anchor]))];
    }

    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    public DockingLayout Start { get; }

    /// <summary>
    /// A zone for each free anchor, and for the toolbar's own. Occupied anchors have none. A strip is as thick as the
    /// toolbar would be there, and a corner's square has room for a strip each way. They lie among the other toolbars
    /// like toolbars, so they never overlap one.
    /// </summary>
    public IReadOnlyList<ToolbarDropZone> Zones { get; }

    /// <summary>The layout without the dragged toolbar, which is what shows around it during the drag.</summary>
    public DockingLayout Others { get; }

    /// <summary>
    /// What the other toolbars keep clear of during the drag. They get their length back when it ends, and until then
    /// they stay put, so that nothing shifts as the pointer moves.
    /// </summary>
    public IReadOnlyList<ReservedToolbarSpot> ReservedSpots { get; }

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
        var solved = Solve(layout);
        var subToolbar = solved.SubToolbar?.ParentToolbarId == _toolbarId ? solved.SubToolbar : null;

        if (ZoneAt(position) is { } zone)
            return new ToolbarDragPreview(layout, zone, null, subToolbar, new HashSet<ToolbarAnchor> { zone.Anchor });

        // Only worth showing if something pushed it away from where the pointer holds it.
        var landing = solved.Toolbars.FirstOrDefault(toolbar => toolbar.ToolbarId == _toolbarId)?.Bounds;
        if (landing is not { } bounds || IsAt(bounds, HeldTopLeft(position)))
            return new ToolbarDragPreview(layout, null, null, subToolbar, new HashSet<ToolbarAnchor>());

        return new ToolbarDragPreview(layout, null, bounds, subToolbar, ZonesUnder(bounds, subToolbar?.Bounds));
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

    // A strip is as thick as the toolbar would be on it. A corner's square has room for a strip of each direction, so
    // it's never thinner than the strips beside it, unless the region's so small that two of them would meet.
    private ReservedToolbarSpot SpotFor(ToolbarAnchor anchor, LayoutRect inner)
    {
        var horizontalThickness = _shapes.Horizontal.Height;
        var verticalThickness = _shapes.Vertical.Width;

        switch (ToolbarAnchors.RequiredOrientation(anchor))
        {
            case ToolbarOrientation.Horizontal:
                return new ReservedToolbarSpot(anchor, inner.Width * _metrics.ToolbarZoneMinimumShare, horizontalThickness);
            case ToolbarOrientation.Vertical:
                return new ReservedToolbarSpot(anchor, inner.Height * _metrics.ToolbarZoneMinimumShare, verticalThickness);
            default:
                var mostThatFits = Math.Max(0, (Math.Min(inner.Width, inner.Height) - _metrics.ToolbarGap) / 2);
                var side = Math.Min(horizontalThickness + verticalThickness, mostThatFits);
                return new ReservedToolbarSpot(anchor, side, side);
        }
    }

    private static bool IsThereRoomFor(ReservedToolbarSpot spot,
        IReadOnlyDictionary<ToolbarAnchor, LayoutRect> solved) =>
        solved.TryGetValue(spot.Anchor, out var bounds) && bounds.Width > 0 && bounds.Height > 0;

    private LayoutPoint HeldTopLeft(LayoutPoint position) =>
        new(position.X - _grab.X * _heldSize.Width, position.Y - _grab.Y * _heldSize.Height);

    private SolvedLayout Solve(DockingLayout layout) =>
        LayoutSolver.Solve(layout, MeasurementsFor(OrientationIn(layout)), _metrics);

    private LayoutRect? WhereItLands(DockingLayout layout) =>
        Solve(layout).Toolbars.FirstOrDefault(toolbar => toolbar.ToolbarId == _toolbarId)?.Bounds;

    private HashSet<ToolbarAnchor> ZonesUnder(LayoutRect toolbar, LayoutRect? subToolbar) =>
    [
        .. Zones
            .Where(zone => zone.Bounds.Intersects(toolbar) || (subToolbar is { } sub && zone.Bounds.Intersects(sub)))
            .Select(zone => zone.Anchor),
    ];

    private static bool IsAt(LayoutRect bounds, LayoutPoint topLeft) =>
        Math.Abs(bounds.X - topLeft.X) < SameSpotTolerance && Math.Abs(bounds.Y - topLeft.Y) < SameSpotTolerance;
}
