// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Dragging;

/// <summary>Which edges of a floating window a resize moves.</summary>
[Flags]
public enum WindowEdges
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>The left edge.</summary>
    Left = 1,

    /// <summary>The top edge.</summary>
    Top = 2,

    /// <summary>The right edge.</summary>
    Right = 4,

    /// <summary>The bottom edge.</summary>
    Bottom = 8,
}

/// <summary>One drag that resizes windows, as data. The layout only changes when it ends.</summary>
public interface IWindowResize
{
    /// <summary>The layout as it was before the drag, which cancelling goes back to.</summary>
    DockingLayout Start { get; }

    /// <summary>What the layout would be with the pointer this far from where it was pressed.</summary>
    DockingLayout LayoutAfter(LayoutPoint moved);
}

/// <summary>Dragging a side's inner edge: all its windows share the one width.</summary>
public sealed class SideWidthResize : IWindowResize
{
    private readonly SideEdge _edge;
    private readonly double _startWidth;
    private readonly double _minimum;
    private readonly double _maximum;

    /// <summary>Starts a resize.</summary>
    /// <param name="start">The layout as it was before the drag, with windows on that side.</param>
    /// <param name="edge">The side.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public SideWidthResize(DockingLayout start, SideEdge edge, LayoutMeasurements measurements, DockingMetrics metrics)
    {
        Start = start;
        _edge = edge;

        var side = start.Side(edge) ?? throw new ArgumentException("That side has no windows.", nameof(edge));
        _startWidth = side.Columns[0].Width;
        _minimum = metrics.MinimumWindowWidth;

        var otherEdge = edge == SideEdge.Left ? SideEdge.Right : SideEdge.Left;
        var otherWidth = start.Side(otherEdge)?.Columns.Sum(column => column.Width) ?? 0;
        var margins = start.Sides.Count(other => other.Kind == SideKind.Widgets) * metrics.Margin;
        var room = measurements.Available.Width - metrics.MinimumCanvasRegionWidth - otherWidth - margins;

        _maximum = Math.Max(_minimum, room);
    }

    /// <inheritdoc />
    public DockingLayout Start { get; }

    /// <inheritdoc />
    public DockingLayout LayoutAfter(LayoutPoint moved)
    {
        var wider = _edge == SideEdge.Left ? moved.X : -moved.X;
        var width = Math.Clamp(_startWidth + wider, _minimum, _maximum);

        return Start with
        {
            Sides =
            [
                .. Start.Sides.Select(side => side.Edge == _edge
                    ? side with { Columns = [side.Columns[0] with { Width = width }, .. side.Columns.Skip(1)] }
                    : side),
            ],
        };
    }
}

/// <summary>Dragging the gap between two widgets.</summary>
public sealed class StackGapResize : IWindowResize
{
    private readonly SideEdge _edge;
    private readonly int _gap;
    private readonly IReadOnlyList<StackLengths.Entry> _entries;
    private readonly double _titleLength;

    /// <summary>Starts a resize.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="edge">The side.</param>
    /// <param name="gap">Which gap: 0 is the one below the first window.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public StackGapResize(DockingLayout start, SideEdge edge, int gap, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        Start = start;
        _edge = edge;
        _gap = gap;
        _titleLength = metrics.TitleLength;

        var solved = LayoutSolver.Solve(start, measurements, metrics);
        _entries =
        [
            .. solved.WindowGroups.Where(group => group.Edge == edge).OrderBy(group => group.Bounds.Y).Select(group =>
                new StackLengths.Entry(group.Bounds.Height, group.IsTitleOnly,
                    group.WindowIds.Select(id => measurements.MinimumWindowLengths.GetValueOrDefault(id, metrics.DefaultMinimumWindowLength)).Max())),
        ];
    }

    /// <inheritdoc />
    public DockingLayout Start { get; }

    /// <inheritdoc />
    public DockingLayout LayoutAfter(LayoutPoint moved)
    {
        var entries = StackLengths.AfterMovingGap(_entries, _gap, moved.Y, _titleLength);

        return Start with
        {
            Sides =
            [
                .. Start.Sides.Select(side => side.Edge == _edge
                    ? side with { Columns = [side.Columns[0] with { Groups = WithEntries(side.Columns[0].Groups, entries) }] }
                    : side),
            ],
        };
    }

    // In logical pixels, which is fine because shares only mean something relative to each other, and the ones showing
    // their contents add up to exactly the room they have. A title-only window keeps its old share.
    private static List<WindowGroup> WithEntries(IReadOnlyList<WindowGroup> groups, IReadOnlyList<StackLengths.Entry> entries) =>
    [
        .. groups.Select((group, index) => group with
        {
            Share = entries[index].IsTitleOnly ? group.Share : entries[index].Length,
            IsTitleOnly = entries[index].IsTitleOnly,
        }),
    ];
}

/// <summary>
/// Dragging an edge or corner of a floating window. It can't get narrower than the smallest width. Below its minimum
/// height it sticks at that height, then snaps to title-only halfway to it, with no sizes in between.
/// </summary>
public sealed class FloatingWindowResize : IWindowResize
{
    private readonly string _windowId;
    private readonly WindowEdges _edges;
    private readonly LayoutRect _bounds;
    private readonly LayoutSize _available;
    private readonly double _minimumWidth;
    private readonly double _minimumHeight;
    private readonly double _titleLength;

    /// <summary>Starts a resize.</summary>
    /// <param name="start">The layout as it was before the drag.</param>
    /// <param name="windowId">The window, which must be floating.</param>
    /// <param name="edges">Which edges move.</param>
    /// <param name="measurements">The current sizes of everything.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public FloatingWindowResize(DockingLayout start, string windowId, WindowEdges edges, LayoutMeasurements measurements,
        DockingMetrics metrics)
    {
        Start = start;
        _windowId = windowId;
        _edges = edges;
        _available = measurements.Available;
        _minimumWidth = metrics.MinimumWindowWidth;
        _minimumHeight = measurements.MinimumWindowLengths.GetValueOrDefault(windowId, metrics.DefaultMinimumWindowLength);
        _titleLength = metrics.TitleLength;

        var solved = LayoutSolver.Solve(start, measurements, metrics);
        _bounds = solved.FloatingWindows.First(window => window.WindowId == windowId).Bounds;
    }

    /// <inheritdoc />
    public DockingLayout Start { get; }

    /// <inheritdoc />
    public DockingLayout LayoutAfter(LayoutPoint moved)
    {
        var (left, right) = Moved(_bounds.X, _bounds.Right, moved.X, WindowEdges.Left, WindowEdges.Right, _available.Width, _minimumWidth);
        var (top, bottom, isTitleOnly) = MovedUpAndDown(moved.Y);

        return Start with
        {
            FloatingWindows =
            [
                .. Start.FloatingWindows.Select(window => window.WindowId != _windowId
                    ? window
                    : window with
                    {
                        Position = new LayoutPoint(left, top),
                        Width = right - left,
                        Height = isTitleOnly ? window.Height : bottom - top,
                        IsTitleOnly = isTitleOnly,
                    }),
            ],
        };
    }

    private (double Start, double End) Moved(double start, double end, double moved, WindowEdges startEdge,
        WindowEdges endEdge, double room, double minimum)
    {
        if (_edges.HasFlag(startEdge))
            return (Math.Max(0, Math.Min(start + moved, end - minimum)), end);

        return _edges.HasFlag(endEdge) ? (start, Math.Min(room, Math.Max(end + moved, start + minimum))) : (start, end);
    }

    private (double Top, double Bottom, bool IsTitleOnly) MovedUpAndDown(double moved)
    {
        var movesTop = _edges.HasFlag(WindowEdges.Top);
        if (!movesTop && !_edges.HasFlag(WindowEdges.Bottom))
            return (_bounds.Y, _bounds.Bottom, Start.FloatingWindowNamed(_windowId)!.IsTitleOnly);

        var wanted = movesTop
            ? _bounds.Bottom - Math.Max(0, _bounds.Y + moved)
            : Math.Min(_available.Height, _bounds.Bottom + moved) - _bounds.Y;

        var (height, isTitleOnly) = StackLengths.Snapped(wanted, _minimumHeight, _titleLength);
        return movesTop
            ? (_bounds.Bottom - height, _bounds.Bottom, isTitleOnly)
            : (_bounds.Y, _bounds.Y + height, isTitleOnly);
    }
}

/// <summary>
/// How a stack of windows shares its length when the user drags a gap. The window on one side of the gap grows, and
/// the ones on the other give way, nearest first. A window at its minimum sticks there until the drag has gone halfway
/// to its title, then snaps to title-only and the next one gives way. There are no sizes in between.
/// </summary>
internal static class StackLengths
{
    public readonly record struct Entry(double Length, bool IsTitleOnly, double MinimumLength);

    /// <summary>What a window this long is shown as: its title, its minimum or as asked.</summary>
    public static (double Length, bool IsTitleOnly) Snapped(double wanted, double minimum, double titleLength)
    {
        if (wanted >= minimum)
            return (wanted, false);

        return wanted < (minimum + titleLength) / 2 ? (titleLength, true) : (minimum, false);
    }

    /// <summary>The stack after the gap has moved this far: positive is down, negative is up.</summary>
    public static IReadOnlyList<Entry> AfterMovingGap(IReadOnlyList<Entry> entries, int gap, double moved, double titleLength)
    {
        if (moved == 0 || gap < 0 || gap >= entries.Count - 1)
            return entries;

        var grower = moved > 0 ? gap : gap + 1;
        var givingWay = moved > 0
            ? Enumerable.Range(gap + 1, entries.Count - gap - 1)
            : Enumerable.Range(0, gap + 1).Reverse();

        var result = entries.ToArray();
        var grown = entries[grower];
        var distance = Math.Abs(moved);
        var wanted = grown.IsTitleOnly
            ? Snapped(titleLength + distance, grown.MinimumLength, titleLength).Length
            : grown.Length + distance;

        // Still title-only: it hasn't been dragged far enough to open, so nothing moves.
        if (wanted <= grown.Length)
            return entries;

        var given = TakeFrom(result, givingWay, wanted - grown.Length, titleLength);

        // Opening takes its whole minimum, and there may not be that much to take.
        if (grown.IsTitleOnly && grown.Length + given < grown.MinimumLength)
            return entries;

        result[grower] = grown with { Length = grown.Length + given, IsTitleOnly = false };
        return result;
    }

    // Returns how much it took, which can be more than needed, because a window snapping to its title frees all of it.
    private static double TakeFrom(Entry[] entries, IEnumerable<int> givingWay, double needed, double titleLength)
    {
        var taken = 0.0;
        foreach (var index in givingWay)
        {
            var entry = entries[index];
            if (entry.IsTitleOnly)
                continue;

            var share = Math.Min(needed - taken, Math.Max(0, entry.Length - entry.MinimumLength));
            taken += share;
            entries[index] = entry with { Length = entry.Length - share };

            var stillNeeded = needed - taken;
            if (stillNeeded <= 0)
                break;

            var freed = entries[index].Length - titleLength;
            if (stillNeeded < freed / 2)
                break;

            entries[index] = entry with { Length = titleLength, IsTitleOnly = true };
            taken += freed;
        }

        return taken;
    }
}
