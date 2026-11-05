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
/// <param name="DimmedZones">The zones that dim, because the copy of the toolbar overlaps them.</param>
public sealed record ToolbarDragPreview(
    DockingLayout Layout,
    ToolbarDropZone? Zone,
    LayoutRect? PushedBackTo,
    SolvedSubToolbar? SubToolbar,
    IReadOnlySet<ToolbarAnchor> DimmedZones);

/// <summary>
/// One drag of a toolbar, as data: what's a drop zone, and what a drop would do for any pointer position. A preview
/// and a drop both come from <see cref="LayoutAt"/>, so they can't disagree. The other toolbars are out of sight for
/// the whole drag, and each zone has the same place in every drag, whatever else is on its edge (docs/DOCKING.md §5.1).
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
        Zones =
        [
            .. ToolbarAnchors.All
                .Where(anchor => !start.IsAnchorTaken(anchor, toolbarId))
                .Select(anchor => new ToolbarDropZone(anchor, BoundsOfZone(anchor, inner)))
                .Where(zone => zone.Bounds.Width > 0 && zone.Bounds.Height > 0),
        ];
    }

    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    public DockingLayout Start { get; }

    /// <summary>
    /// A zone for each free anchor, and for the toolbar's own. Occupied anchors have none. A strip runs the full length
    /// between its edge's corners and is as thick as the toolbar would be there. A corner's square has room for a strip
    /// each way. None of them ever overlap.
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
        var solved = Solve(layout);
        var subToolbar = solved.SubToolbar?.ParentToolbarId == _toolbarId ? solved.SubToolbar : null;

        if (ZoneAt(position) is { } zone)
            return new ToolbarDragPreview(layout, zone, null, subToolbar, new HashSet<ToolbarAnchor>());

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

    private LayoutRect BoundsOfZone(ToolbarAnchor anchor, LayoutRect inner)
    {
        var side = CornerSide(inner);
        var inset = side + _metrics.ToolbarGap;

        // No thicker than a corner's square, so that strips can't reach each other in a very small region.
        var acrossThickness = Math.Min(_shapes.Horizontal.Height, side);
        var uprightThickness = Math.Min(_shapes.Vertical.Width, side);
        var acrossLength = Math.Max(0, inner.Width - inset * 2);
        var uprightLength = Math.Max(0, inner.Height - inset * 2);

        return anchor switch
        {
            ToolbarAnchor.TopLeft => new LayoutRect(inner.X, inner.Y, side, side),
            ToolbarAnchor.TopMiddle => new LayoutRect(inner.X + inset, inner.Y, acrossLength, acrossThickness),
            ToolbarAnchor.TopRight => new LayoutRect(inner.Right - side, inner.Y, side, side),
            ToolbarAnchor.RightMiddle => new LayoutRect(inner.Right - uprightThickness, inner.Y + inset, uprightThickness, uprightLength),
            ToolbarAnchor.BottomRight => new LayoutRect(inner.Right - side, inner.Bottom - side, side, side),
            ToolbarAnchor.BottomMiddle => new LayoutRect(inner.X + inset, inner.Bottom - acrossThickness, acrossLength, acrossThickness),
            ToolbarAnchor.BottomLeft => new LayoutRect(inner.X, inner.Bottom - side, side, side),
            ToolbarAnchor.LeftMiddle => new LayoutRect(inner.X, inner.Y + inset, uprightThickness, uprightLength),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
        };
    }

    // Room for a strip of each direction, so it's never thinner than the strips beside it. In a very small region it
    // gives way, because every strip keeps its share of its edge.
    private double CornerSide(LayoutRect inner)
    {
        var shortest = Math.Min(inner.Width, inner.Height);
        var mostThatFits = (shortest * (1 - _metrics.ToolbarZoneMinimumShare) - _metrics.ToolbarGap * 2) / 2;

        return Math.Max(0, Math.Min(_shapes.Horizontal.Height + _shapes.Vertical.Width, mostThatFits));
    }

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
