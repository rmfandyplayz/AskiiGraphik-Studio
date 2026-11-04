// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>A place on screen where dropping a toolbar puts it on an anchor.</summary>
/// <param name="Anchor">The anchor it stands for.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record ToolbarDropZone(ToolbarAnchor Anchor, LayoutRect Bounds);

/// <summary>
/// One drag of a toolbar, as data: what's a drop zone, and what the layout would be for any pointer position.
/// A preview and a drop both ask <see cref="LayoutAt"/>, so they can't disagree. The zones are worked out once,
/// when the drag starts, so they don't move as the layout shifts under the pointer (docs/DOCKING.md §5).
/// </summary>
public sealed class ToolbarDrag
{
    private readonly string _toolbarId;
    private readonly LayoutPoint _grab;
    private readonly LayoutSize _size;
    private readonly LayoutMeasurements _measurements;
    private readonly DockingMetrics _metrics;

    /// <summary>Starts a drag.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="toolbarId">The toolbar being dragged.</param>
    /// <param name="grab">Where on the toolbar the pointer holds it, from 0 to 1 along each axis.</param>
    /// <param name="measurements">The current sizes, with the toolbar's size for its current direction.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public ToolbarDrag(DockingLayout start, string toolbarId, LayoutPoint grab, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        Start = start;
        _toolbarId = toolbarId;
        _grab = grab;
        _size = measurements.ToolbarSizes.GetValueOrDefault(toolbarId);
        _measurements = measurements;
        _metrics = metrics;

        var region = LayoutSolver.Solve(start, measurements, metrics).CanvasRegion;
        Zones = [.. ZonesFor(start, toolbarId, ToolbarSolver.InnerRegion(region, metrics), metrics)];
    }

    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    public DockingLayout Start { get; }

    /// <summary>A zone for each free anchor, and for the toolbar's own. Occupied anchors have none.</summary>
    public IReadOnlyList<ToolbarDropZone> Zones { get; }

    /// <summary>The zone at <paramref name="position"/>, if any.</summary>
    public ToolbarDropZone? ZoneAt(LayoutPoint position) => Zones.FirstOrDefault(zone => zone.Bounds.Contains(position));

    /// <summary>What dropping with the pointer at <paramref name="position"/> would do: go on a zone's anchor, or float.</summary>
    public DockingLayout LayoutAt(LayoutPoint position)
    {
        if (ZoneAt(position) is { } zone)
            return LayoutEdits.MoveToolbarToAnchor(Start, _toolbarId, zone.Anchor);

        var topLeft = new LayoutPoint(position.X - _grab.X * _size.Width, position.Y - _grab.Y * _size.Height);
        var floated = LayoutEdits.FloatToolbar(Start, _toolbarId, topLeft);

        // It saves where it lands, not where the pointer was. A toolbar dropped over a window sits beside that
        // window, and shouldn't jump under the pointer's old spot if the window closes later.
        var landed = LayoutSolver.Solve(floated, _measurements, _metrics).Toolbars
            .FirstOrDefault(toolbar => toolbar.ToolbarId == _toolbarId);

        return landed is null
            ? floated
            : LayoutEdits.FloatToolbar(Start, _toolbarId, new LayoutPoint(landed.Bounds.X, landed.Bounds.Y));
    }

    private static IEnumerable<ToolbarDropZone> ZonesFor(DockingLayout layout, string toolbarId, LayoutRect inner,
        DockingMetrics metrics)
    {
        foreach (var anchor in ToolbarAnchors.All)
        {
            if (layout.IsAnchorTaken(anchor, toolbarId))
                continue;

            var bounds = Bounds(anchor, inner, metrics);
            if (bounds.Width > 0 && bounds.Height > 0)
                yield return new ToolbarDropZone(anchor, bounds);
        }
    }

    private static LayoutRect Bounds(ToolbarAnchor anchor, LayoutRect inner, DockingMetrics metrics)
    {
        var corner = metrics.ToolbarZoneCornerSize;
        var thickness = metrics.ToolbarZoneEdgeThickness;
        var clear = corner + metrics.ToolbarGap;
        var along = Math.Max(0, inner.Width - clear * 2);
        var down = Math.Max(0, inner.Height - clear * 2);

        return anchor switch
        {
            ToolbarAnchor.TopLeft => new LayoutRect(inner.X, inner.Y, corner, corner),
            ToolbarAnchor.TopRight => new LayoutRect(inner.Right - corner, inner.Y, corner, corner),
            ToolbarAnchor.BottomLeft => new LayoutRect(inner.X, inner.Bottom - corner, corner, corner),
            ToolbarAnchor.BottomRight => new LayoutRect(inner.Right - corner, inner.Bottom - corner, corner, corner),
            ToolbarAnchor.TopMiddle => new LayoutRect(inner.X + clear, inner.Y, along, thickness),
            ToolbarAnchor.BottomMiddle => new LayoutRect(inner.X + clear, inner.Bottom - thickness, along, thickness),
            ToolbarAnchor.LeftMiddle => new LayoutRect(inner.X, inner.Y + clear, thickness, down),
            ToolbarAnchor.RightMiddle => new LayoutRect(inner.Right - thickness, inner.Y + clear, thickness, down),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
        };
    }
}
