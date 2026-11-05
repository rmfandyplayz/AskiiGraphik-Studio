// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Where the solver put the toolbars.</summary>
/// <param name="Toolbars">Every toolbar that's showing.</param>
/// <param name="ReservedSpots">Where each reserved spot went.</param>
/// <param name="SubToolbar">Where the sub-toolbar went, if one was asked for and it fits.</param>
/// <param name="ToolbarsWithoutSubToolbar">Where the toolbars would be with no sub-toolbar, for the safe area.</param>
internal sealed record ToolbarSolution(
    IReadOnlyList<SolvedToolbar> Toolbars,
    IReadOnlyDictionary<ToolbarAnchor, LayoutRect> ReservedSpots,
    SolvedSubToolbar? SubToolbar,
    IReadOnlyList<SolvedToolbar> ToolbarsWithoutSubToolbar);

/// <summary>
/// Puts each toolbar on its anchor, inside the canvas region less a margin. The margin equals the widgets',
/// so a toolbar never passes a window's top or bottom edge. Toolbars along one edge share its length, and a
/// toolbar on a neighbouring edge starts after a corner toolbar (docs/DOCKING.md §4.3). A toolbar with no room
/// for even its minimum length is left out, corners first, until there's room again. A floating toolbar goes
/// where it was put, moved just enough to stay inside, and nothing moves for it. A reserved spot goes through
/// all this like a toolbar, except that it gets its length before any toolbar does.
/// </summary>
internal static partial class ToolbarSolver
{
    private enum Edge
    {
        Top,
        Bottom,
        Left,
        Right,
    }

    // With no toolbar, it's a reserved spot.
    private sealed record Item(string? ToolbarId, ToolbarAnchor Anchor, double Length, double Thickness)
    {
        public bool IsReserved => ToolbarId is null;
    }

    private sealed record Placed(Item Item, ToolbarOrientation Orientation, LayoutRect Bounds);

    // One edge's toolbars, and the stretch of the edge they had to stay within, measured from its start.
    private sealed record Lane(Edge Edge, double From, double To, List<Placed> Toolbars);

    private sealed record Lanes(Lane Top, Lane Bottom, Lane Left, Lane Right)
    {
        public IEnumerable<Lane> All => [Top, Bottom, Left, Right];
    }

    public static ToolbarSolution Solve(IReadOnlyList<ToolbarPlacement> toolbars,
        IReadOnlyDictionary<string, LayoutSize> sizes, LayoutRect region, DockingMetrics metrics,
        IReadOnlyList<ReservedToolbarSpot> reserved, SubToolbarRequest? subToolbar)
    {
        var inner = InnerRegion(region, metrics);
        var showing = toolbars.Where(toolbar => toolbar.IsVisible && sizes.ContainsKey(toolbar.ToolbarId)).ToList();

        var anchored = showing
            .Where(toolbar => !toolbar.IsFloating)
            .Select(toolbar => (Edge: EdgeOf(toolbar.Anchor, toolbar.Orientation),
                Item: ToItem(toolbar, sizes[toolbar.ToolbarId], inner)));

        // A corner's spot is square, so it could lie along either edge. It lies along the horizontal one, and the
        // vertical one starts after it, as it would after a horizontal corner toolbar.
        var spots = reserved.Select(spot => (Edge: EdgeOf(spot.Anchor, ToolbarOrientation.Horizontal),
            Item: ToItem(spot, inner)));

        var byEdge = anchored.Concat(spots).ToLookup(entry => entry.Edge, entry => entry.Item);
        var lanes = SolveLanes(byEdge, inner, metrics);

        var floating = showing
            .Where(toolbar => toolbar.IsFloating)
            .Select(toolbar => PlaceFloating(toolbar, sizes[toolbar.ToolbarId], inner, metrics))
            .OfType<SolvedToolbar>()
            .ToList();

        var withoutSubToolbar = ToSolved(lanes, floating);
        var (solvedSubToolbar, finalLanes) = subToolbar is null
            ? (null, lanes)
            : AddSubToolbar(subToolbar, lanes, byEdge, showing, floating, sizes, inner, metrics);

        var reservedSpots = new Dictionary<ToolbarAnchor, LayoutRect>();
        foreach (var placed in finalLanes.All.SelectMany(lane => lane.Toolbars).Where(placed => placed.Item.IsReserved))
            reservedSpots[placed.Item.Anchor] = placed.Bounds;

        return new ToolbarSolution(ToSolved(finalLanes, floating), reservedSpots, solvedSubToolbar, withoutSubToolbar);
    }

    /// <summary>Where toolbars can go: the canvas region less a margin.</summary>
    internal static LayoutRect InnerRegion(LayoutRect region, DockingMetrics metrics) => new(
        region.X + metrics.Margin,
        region.Y + metrics.Margin,
        Math.Max(0, region.Width - metrics.Margin * 2),
        Math.Max(0, region.Height - metrics.Margin * 2));

    private static List<SolvedToolbar> ToSolved(Lanes lanes, List<SolvedToolbar> floating)
    {
        var solved = lanes.All.SelectMany(lane => lane.Toolbars)
            .Where(placed => !placed.Item.IsReserved)
            .Select(placed =>
                new SolvedToolbar(placed.Item.ToolbarId!, placed.Item.Anchor, placed.Orientation, placed.Bounds))
            .ToList();

        solved.AddRange(floating);
        return solved;
    }

    private static SolvedToolbar? PlaceFloating(ToolbarPlacement toolbar, LayoutSize size, LayoutRect inner,
        DockingMetrics metrics)
    {
        var isHorizontal = toolbar.Orientation == ToolbarOrientation.Horizontal;
        var length = isHorizontal ? size.Width : size.Height;
        var room = isHorizontal ? inner.Width : inner.Height;
        if (room < Math.Min(length, metrics.MinimumToolbarLength))
            return null;

        var width = Math.Min(size.Width, inner.Width);
        var height = Math.Min(size.Height, inner.Height);
        var wanted = toolbar.FloatingAt ?? default;

        // Max last, so rounding can't put it outside when it exactly fills the room.
        var bounds = new LayoutRect(
            Math.Max(inner.X, Math.Min(wanted.X, inner.Right - width)),
            Math.Max(inner.Y, Math.Min(wanted.Y, inner.Bottom - height)),
            width,
            height);

        return new SolvedToolbar(toolbar.ToolbarId, null, toolbar.Orientation, bounds);
    }

    private static Edge EdgeOf(ToolbarAnchor anchor, ToolbarOrientation orientation) => anchor switch
    {
        ToolbarAnchor.TopMiddle => Edge.Top,
        ToolbarAnchor.BottomMiddle => Edge.Bottom,
        ToolbarAnchor.LeftMiddle => Edge.Left,
        ToolbarAnchor.RightMiddle => Edge.Right,
        ToolbarAnchor.TopLeft => orientation == ToolbarOrientation.Horizontal ? Edge.Top : Edge.Left,
        ToolbarAnchor.TopRight => orientation == ToolbarOrientation.Horizontal ? Edge.Top : Edge.Right,
        ToolbarAnchor.BottomLeft => orientation == ToolbarOrientation.Horizontal ? Edge.Bottom : Edge.Left,
        ToolbarAnchor.BottomRight => orientation == ToolbarOrientation.Horizontal ? Edge.Bottom : Edge.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null),
    };

    private static bool IsHorizontalLane(Edge edge) => edge is Edge.Top or Edge.Bottom;

    private static Item ToItem(ToolbarPlacement toolbar, LayoutSize size, LayoutRect inner) =>
        IsHorizontalLane(EdgeOf(toolbar.Anchor, toolbar.Orientation))
            ? new Item(toolbar.ToolbarId, toolbar.Anchor, size.Width, Math.Min(size.Height, inner.Height))
            : new Item(toolbar.ToolbarId, toolbar.Anchor, size.Height, Math.Min(size.Width, inner.Width));

    private static Item ToItem(ReservedToolbarSpot spot, LayoutRect inner)
    {
        var room = IsHorizontalLane(EdgeOf(spot.Anchor, ToolbarOrientation.Horizontal)) ? inner.Height : inner.Width;
        return new Item(null, spot.Anchor, spot.Length, Math.Min(spot.Thickness, room));
    }

    // Horizontal edges first: where a vertical toolbar and a horizontal one could both reach a corner with no toolbar
    // anchored on it, the horizontal one gets it. A sub-toolbar's parent keeps its lane, and the sub-toolbar takes
    // room from the lanes running away from it.
    private static Lanes SolveLanes(ILookup<Edge, Item> byEdge, LayoutRect inner, DockingMetrics metrics,
        SubToolbarPush? push = null)
    {
        var gap = metrics.ToolbarGap;

        Lane SolveOrKeep(Edge edge, double lead, double trail)
        {
            if (push?.ParentLane.Edge == edge)
                return push.ParentLane;

            var (pushedLead, pushedTrail) = TakenBy(push, edge, BandOf(byEdge[edge], edge, inner), inner, gap);
            return SolveLane(byEdge[edge], edge, inner, metrics, Math.Max(lead, pushedLead),
                Math.Max(trail, pushedTrail));
        }

        var top = SolveOrKeep(Edge.Top,
            lead: BandFor(byEdge[Edge.Left], ToolbarAnchor.TopLeft, gap),
            trail: BandFor(byEdge[Edge.Right], ToolbarAnchor.TopRight, gap));
        var bottom = SolveOrKeep(Edge.Bottom,
            lead: BandFor(byEdge[Edge.Left], ToolbarAnchor.BottomLeft, gap),
            trail: BandFor(byEdge[Edge.Right], ToolbarAnchor.BottomRight, gap));

        var leftColumn = LaneThickness(byEdge[Edge.Left]);
        var rightColumn = LaneThickness(byEdge[Edge.Right]);
        var left = SolveOrKeep(Edge.Left,
            lead: BandAcross(top.Toolbars, inner.X, inner.X + leftColumn, gap),
            trail: BandAcross(bottom.Toolbars, inner.X, inner.X + leftColumn, gap));
        var right = SolveOrKeep(Edge.Right,
            lead: BandAcross(top.Toolbars, inner.Right - rightColumn, inner.Right, gap),
            trail: BandAcross(bottom.Toolbars, inner.Right - rightColumn, inner.Right, gap));

        return new Lanes(top, bottom, left, right);
    }

    // What a corner toolbar lying along the neighbouring edge takes from the start or end of this one.
    private static double BandFor(IEnumerable<Item> neighbours, ToolbarAnchor corner, double gap) =>
        neighbours.Where(item => item.Anchor == corner)
            .Select(item => item.Thickness + gap)
            .FirstOrDefault();

    private static double LaneThickness(IEnumerable<Item> items) =>
        items.Select(item => item.Thickness).DefaultIfEmpty(0).Max();

    // What toolbars already placed along a horizontal edge take from a vertical edge's column.
    private static double BandAcross(IEnumerable<Placed> toolbars, double from, double to, double gap) =>
        toolbars.Where(toolbar => toolbar.Bounds.X < to && toolbar.Bounds.Right > from)
            .Select(toolbar => toolbar.Bounds.Height + gap)
            .DefaultIfEmpty(0)
            .Max();

    private static Lane SolveLane(IEnumerable<Item> lane, Edge edge, LayoutRect inner,
        DockingMetrics metrics, double lead, double trail)
    {
        var laneLength = IsHorizontalLane(edge) ? inner.Width : inner.Height;
        var from = lead;
        var to = Math.Max(from, laneLength - trail);

        var items = lane.ToList();
        LeaveOutWhatCantFit(items, to - from, metrics);

        var room = Math.Max(0, to - from - metrics.ToolbarGap * Math.Max(0, items.Count - 1));
        var lengths = ShareOut(items, room, metrics);

        var solved = new List<Placed>();
        var startEnd = from;
        var endStart = to;

        for (var index = 0; index < items.Count; index++)
        {
            if (IsStartCorner(items[index], edge))
            {
                solved.Add(Place(items[index], edge, inner, from, lengths[index]));
                startEnd = from + lengths[index] + metrics.ToolbarGap;
            }
            else if (IsEndCorner(items[index], edge))
            {
                solved.Add(Place(items[index], edge, inner, to - lengths[index], lengths[index]));
                endStart = to - lengths[index] - metrics.ToolbarGap;
            }
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (IsStartCorner(items[index], edge) || IsEndCorner(items[index], edge))
                continue;

            if (items[index].IsReserved)
            {
                solved.Add(Place(items[index], edge, inner, startEnd, Math.Max(0, endStart - startEnd)));
                continue;
            }

            // Centred on the whole edge, until a corner toolbar reaches it.
            var length = lengths[index];
            var centred = (laneLength - length) / 2;
            var latest = Math.Max(startEnd, endStart - length);
            solved.Add(Place(items[index], edge, inner, Math.Clamp(centred, startEnd, latest), length));
        }

        return new Lane(edge, from, to, solved);
    }

    private static void LeaveOutWhatCantFit(List<Item> items, double available, DockingMetrics metrics)
    {
        while (items.Count > 0)
        {
            var room = available - metrics.ToolbarGap * (items.Count - 1);
            if (items.Sum(item => SmallestLength(item, metrics)) <= room)
                return;

            // Reserved spots go after every toolbar. Corners go first, and the end corner before the start one.
            var toolbars = items.Where(item => !item.IsReserved).ToList();
            var candidates = toolbars.Count > 0 ? toolbars : items;
            var corners = candidates.Where(IsCorner).ToList();
            items.Remove(corners.Count > 0 ? corners.MaxBy(EndFirst)! : candidates[0]);
        }
    }

    private static bool IsCorner(Item item) => ToolbarAnchors.RequiredOrientation(item.Anchor) is null;

    private static int EndFirst(Item item) =>
        item.Anchor is ToolbarAnchor.TopRight or ToolbarAnchor.BottomRight or ToolbarAnchor.BottomLeft
            ? 1
            : 0;

    private static bool IsStartCorner(Item item, Edge edge) => item.Anchor switch
    {
        ToolbarAnchor.TopLeft => true,
        ToolbarAnchor.TopRight => edge == Edge.Right,
        ToolbarAnchor.BottomLeft => edge == Edge.Bottom,
        _ => false,
    };

    private static bool IsEndCorner(Item item, Edge edge) => item.Anchor switch
    {
        ToolbarAnchor.BottomRight => true,
        ToolbarAnchor.TopRight => edge == Edge.Top,
        ToolbarAnchor.BottomLeft => edge == Edge.Left,
        _ => false,
    };

    private static double SmallestLength(Item item, DockingMetrics metrics) =>
        item.IsReserved ? item.Length : Math.Min(item.Length, metrics.MinimumToolbarLength);

    // Reserved spots take their length first. Then a toolbar that fits its fair share keeps its natural size, and
    // the rest share what remains. The middle one's fair share is a little bigger.
    private static double[] ShareOut(List<Item> items, double room, DockingMetrics metrics)
    {
        var lengths = new double[items.Count];
        var free = new List<int>();

        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].IsReserved)
            {
                lengths[index] = items[index].Length;
                room -= items[index].Length;
            }
            else
            {
                free.Add(index);
            }
        }

        while (free.Count > 0)
        {
            var shares = free.Sum(index => Weight(items[index], metrics));
            var currentRoom = room;
            double Share(int index) => currentRoom * Weight(items[index], metrics) / shares;

            var settled = free.FindIndex(index => items[index].Length <= Share(index));
            if (settled < 0)
                settled = free.FindIndex(index => Share(index) < SmallestLength(items[index], metrics));

            if (settled < 0)
            {
                foreach (var index in free)
                    lengths[index] = Share(index);

                break;
            }

            var chosen = free[settled];
            lengths[chosen] = items[chosen].Length <= Share(chosen)
                ? items[chosen].Length
                : SmallestLength(items[chosen], metrics);
            room -= lengths[chosen];
            free.RemoveAt(settled);
        }

        return lengths;
    }

    private static double Weight(Item item, DockingMetrics metrics) =>
        IsCorner(item) ? 1 : metrics.MiddleToolbarPriority;

    private static Placed Place(Item item, Edge edge, LayoutRect inner, double start, double length)
    {
        var orientation = IsHorizontalLane(edge) ? ToolbarOrientation.Horizontal : ToolbarOrientation.Vertical;
        return new Placed(item, orientation, AtDepth(edge, inner, start, length, depth: 0, item.Thickness));
    }

    // A rectangle lying along an edge: how far along it, how long, how far in from the edge, and how thick.
    private static LayoutRect AtDepth(Edge edge, LayoutRect inner, double start, double length, double depth,
        double thickness)
    {
        return edge switch
        {
            Edge.Top => new LayoutRect(inner.X + start, inner.Y + depth, length, thickness),
            Edge.Bottom => new LayoutRect(inner.X + start, inner.Bottom - depth - thickness, length, thickness),
            Edge.Left => new LayoutRect(inner.X + depth, inner.Y + start, thickness, length),
            Edge.Right => new LayoutRect(inner.Right - depth - thickness, inner.Y + start, thickness, length),
            _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null),
        };
    }
}
