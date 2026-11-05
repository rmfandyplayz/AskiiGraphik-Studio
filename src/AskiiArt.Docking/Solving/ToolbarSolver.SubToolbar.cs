// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>
/// Where a sub-toolbar goes (docs/DOCKING.md §4.4). An anchored parent's sits on the canvas side, and the toolbars of
/// the neighbouring edges move out of its way. A floating parent's sits on the side it remembers, flips if there's no
/// room there, and moves nothing, as a floating toolbar moves nothing.
/// </summary>
internal static partial class ToolbarSolver
{
    // Closer than this to the length it wants, a toolbar isn't fighting for space.
    private const double FightingTolerance = 0.5;

    private sealed record SubToolbarPush(LayoutRect Bounds, Lane ParentLane);

    private static (SolvedSubToolbar? SubToolbar, Lanes Lanes) AddSubToolbar(SubToolbarRequest request, Lanes lanes,
        ILookup<Edge, Item> byEdge, List<ToolbarPlacement> showing, List<SolvedToolbar> floating,
        IReadOnlyDictionary<string, LayoutSize> sizes, LayoutRect inner, DockingMetrics metrics)
    {
        var parentId = request.ParentToolbarId;

        if (floating.FirstOrDefault(toolbar => toolbar.ToolbarId == parentId) is { } floatingParent)
        {
            var side = showing.First(toolbar => toolbar.ToolbarId == parentId).SubToolbarSide;
            return (BesideFloating(request, floatingParent, side, sizes[parentId], inner, metrics), lanes);
        }

        foreach (var lane in lanes.All)
        {
            if (lane.Toolbars.FirstOrDefault(toolbar => toolbar.Item.ToolbarId == parentId) is not { } parent)
                continue;

            if (BesideAnchored(request, lane, parent, inner, metrics) is not { } subToolbar)
                return (null, lanes);

            var pushed = SolveLanes(byEdge, inner, metrics, new SubToolbarPush(subToolbar.Bounds, lane));

            // Pushed as far as they go, and something's still in the way. That's a window too small to have one.
            var isInTheWay = pushed.All.SelectMany(other => other.Toolbars)
                .Any(other => other.Item.ToolbarId != parentId && other.Bounds.Intersects(subToolbar.Bounds));

            return isInTheWay ? (null, lanes) : (subToolbar, pushed);
        }

        return (null, lanes);
    }

    private static SolvedSubToolbar? BesideAnchored(SubToolbarRequest request, Lane lane, Placed parent,
        LayoutRect inner, DockingMetrics metrics)
    {
        var edge = lane.Edge;
        var isHorizontal = IsHorizontalLane(edge);
        var wanted = request.Shapes.For(parent.Orientation);

        var parentStart = AlongStart(parent, edge, inner);
        var parentLength = AlongLength(parent, edge);
        var length = Math.Min(LengthFor(wanted, parent.Orientation, parentLength, parent.Item.Length, metrics),
            lane.To - lane.From);
        if (length <= 0)
            return null;

        var start = Math.Max(lane.From, Math.Min(AlignedStart(parent.Item, edge, parentStart, parentLength, length),
            lane.To - length));

        // Normally just past its parent, but a thicker neighbour along its way would be overlapped there.
        var deepest = lane.Toolbars
            .Where(other => AlongStart(other, edge, inner) < start + length &&
                            AlongStart(other, edge, inner) + AlongLength(other, edge) > start)
            .Select(other => other.Item.Thickness)
            .Append(parent.Item.Thickness)
            .Max();

        var depth = deepest + metrics.ToolbarGap;
        var thickness = isHorizontal ? wanted.Height : wanted.Width;
        if (depth + thickness > (isHorizontal ? inner.Height : inner.Width))
            return null;

        var side = edge is Edge.Top or Edge.Left ? SubToolbarSide.BelowOrRight : SubToolbarSide.AboveOrLeft;
        return new SolvedSubToolbar(request.ParentToolbarId, parent.Orientation, side,
            AtDepth(edge, inner, start, length, depth, thickness));
    }

    private static SolvedSubToolbar? BesideFloating(SubToolbarRequest request, SolvedToolbar parent,
        SubToolbarSide rememberedSide, LayoutSize parentWants, LayoutRect inner, DockingMetrics metrics)
    {
        var isHorizontal = parent.Orientation == ToolbarOrientation.Horizontal;
        var wanted = request.Shapes.For(parent.Orientation);
        var parentLength = isHorizontal ? parent.Bounds.Width : parent.Bounds.Height;
        var parentNatural = isHorizontal ? parentWants.Width : parentWants.Height;

        var length = Math.Min(LengthFor(wanted, parent.Orientation, parentLength, parentNatural, metrics),
            isHorizontal ? inner.Width : inner.Height);
        var size = isHorizontal ? new LayoutSize(length, wanted.Height) : new LayoutSize(wanted.Width, length);

        var otherSide = rememberedSide == SubToolbarSide.BelowOrRight
            ? SubToolbarSide.AboveOrLeft
            : SubToolbarSide.BelowOrRight;

        foreach (var side in new[] { rememberedSide, otherSide })
        {
            var bounds = SubToolbarGeometry.Beside(parent.Bounds, size, parent.Orientation, side, metrics.ToolbarGap);
            if (FitsAcross(bounds, inner, isHorizontal))
                return new SolvedSubToolbar(request.ParentToolbarId, parent.Orientation, side, KeptInsideAlong(bounds, inner, isHorizontal));
        }

        return null;
    }

    // At most its parent's length if the parent is fighting for space, otherwise a bit longer.
    private static double LengthFor(LayoutSize wanted, ToolbarOrientation orientation, double parentLength,
        double parentNatural, DockingMetrics metrics)
    {
        var isFighting = parentLength < parentNatural - FightingTolerance;
        var limit = isFighting ? parentLength : parentLength * metrics.SubToolbarLengthFactor;
        var natural = orientation == ToolbarOrientation.Horizontal ? wanted.Width : wanted.Height;

        return Math.Min(natural, limit);
    }

    // Aligned to its parent's corner, or centred on any other parent.
    private static double AlignedStart(Item parent, Edge edge, double parentStart, double parentLength, double length)
    {
        if (IsStartCorner(parent, edge))
            return parentStart;

        if (IsEndCorner(parent, edge))
            return parentStart + parentLength - length;

        return parentStart + (parentLength - length) / 2;
    }

    private static double AlongStart(Placed toolbar, Edge edge, LayoutRect inner) =>
        IsHorizontalLane(edge) ? toolbar.Bounds.X - inner.X : toolbar.Bounds.Y - inner.Y;

    private static double AlongLength(Placed toolbar, Edge edge) =>
        IsHorizontalLane(edge) ? toolbar.Bounds.Width : toolbar.Bounds.Height;

    private static bool FitsAcross(LayoutRect bounds, LayoutRect inner, bool isHorizontal) =>
        isHorizontal
            ? bounds.Y >= inner.Y && bounds.Bottom <= inner.Bottom
            : bounds.X >= inner.X && bounds.Right <= inner.Right;

    // Max last, so rounding can't put it outside when it exactly fills the room.
    private static LayoutRect KeptInsideAlong(LayoutRect bounds, LayoutRect inner, bool isHorizontal) =>
        isHorizontal
            ? bounds with { X = Math.Max(inner.X, Math.Min(bounds.X, inner.Right - bounds.Width)) }
            : bounds with { Y = Math.Max(inner.Y, Math.Min(bounds.Y, inner.Bottom - bounds.Height)) };

    // The part of the region that a lane's toolbars lie in.
    private static LayoutRect BandOf(IEnumerable<Item> items, Edge edge, LayoutRect inner)
    {
        var thickness = LaneThickness(items);
        return AtDepth(edge, inner, 0, IsHorizontalLane(edge) ? inner.Width : inner.Height, 0, thickness);
    }

    // How much a sub-toolbar takes from the start and end of another lane, if it reaches into that lane's band. Only
    // lanes running away from its parent's edge can make room: one opposite it would have to move across.
    private static (double Lead, double Trail) TakenBy(SubToolbarPush? push, Edge edge, LayoutRect band, LayoutRect inner,
        double gap)
    {
        if (push is null || IsHorizontalLane(edge) == IsHorizontalLane(push.ParentLane.Edge) || !push.Bounds.Intersects(band))
            return default;

        return push.ParentLane.Edge switch
        {
            Edge.Top => (push.Bounds.Bottom - inner.Y + gap, 0),
            Edge.Bottom => (0, inner.Bottom - push.Bounds.Y + gap),
            Edge.Left => (push.Bounds.Right - inner.X + gap, 0),
            Edge.Right => (0, inner.Right - push.Bounds.X + gap),
            _ => throw new ArgumentOutOfRangeException(nameof(push), push.ParentLane.Edge, null),
        };
    }
}

/// <summary>Where a sub-toolbar goes relative to its parent.</summary>
public static class SubToolbarGeometry
{
    /// <summary>A sub-toolbar a gap away from its parent's side, and centred on it.</summary>
    /// <param name="parent">The parent's rectangle.</param>
    /// <param name="size">The sub-toolbar's size.</param>
    /// <param name="orientation">Which way both of them run.</param>
    /// <param name="side">Which side of the parent it's on.</param>
    /// <param name="gap">How far from the parent it is.</param>
    public static LayoutRect Beside(LayoutRect parent, LayoutSize size, ToolbarOrientation orientation,
        SubToolbarSide side, double gap)
    {
        if (orientation == ToolbarOrientation.Horizontal)
        {
            var y = side == SubToolbarSide.BelowOrRight ? parent.Bottom + gap : parent.Y - gap - size.Height;
            return new LayoutRect(parent.X + (parent.Width - size.Width) / 2, y, size.Width, size.Height);
        }

        var x = side == SubToolbarSide.BelowOrRight ? parent.Right + gap : parent.X - gap - size.Width;
        return new LayoutRect(x, parent.Y + (parent.Height - size.Height) / 2, size.Width, size.Height);
    }
}
