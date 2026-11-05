// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>
/// Fits a layout to the app window: sides first, then toolbars in what's left, then the safe area
/// (docs/DOCKING.md §2). It has no state and changes nothing, so it can run on every layout pass, and on a
/// layout that's only being previewed.
/// </summary>
public static class LayoutSolver
{
    /// <summary>Where everything in <paramref name="layout"/> goes right now.</summary>
    /// <param name="layout">What to place.</param>
    /// <param name="measurements">The sizes things want.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public static SolvedLayout Solve(DockingLayout layout, LayoutMeasurements measurements, DockingMetrics metrics)
    {
        var available = measurements.Available;
        var widthScale = SideWidthScale(layout, available.Width, metrics);
        var groups = new List<SolvedWindowGroup>();
        var taken = new Dictionary<SideEdge, double>();

        foreach (var side in layout.Sides)
            taken[side.Edge] = PlaceSide(side, measurements, metrics, widthScale, groups);

        var leftBand = taken.GetValueOrDefault(SideEdge.Left);
        var rightBand = taken.GetValueOrDefault(SideEdge.Right);
        var region = new LayoutRect(leftBand, 0, Math.Max(0, available.Width - leftBand - rightBand), available.Height);
        var solution = ToolbarSolver.Solve(layout.Toolbars, measurements.ToolbarSizes, region, metrics,
            measurements.SubToolbar);

        // Without the sub-toolbar, whatever it pushed, so that picking a tool never moves the view.
        var safeArea = SafeArea(region, available, solution.ToolbarsWithoutSubToolbar, metrics.Margin);

        var floating = layout.FloatingWindows.Select(window => PlaceFloating(window, available, metrics)).ToList();

        return new SolvedLayout(groups, solution.Toolbars, region, safeArea, solution.SubToolbar, floating);
    }

    /// <summary>
    /// Where a toolbar's top left would be if it had its anchor to itself, for floating it there when the anchor
    /// has been taken. <paramref name="measurements"/> must include its size.
    /// </summary>
    public static LayoutPoint WhereAloneOnItsAnchor(DockingLayout layout, string toolbarId,
        LayoutMeasurements measurements, DockingMetrics metrics)
    {
        var alone = layout.Toolbars
            .Where(toolbar => toolbar.ToolbarId == toolbarId)
            .Select(toolbar => toolbar with { IsVisible = true, FloatingAt = null });
        var solved = Solve(layout with { Toolbars = [.. alone] }, measurements, metrics);

        return solved.Toolbars.Count > 0
            ? new LayoutPoint(solved.Toolbars[0].Bounds.X, solved.Toolbars[0].Bounds.Y)
            : new LayoutPoint(solved.CanvasRegion.X + metrics.Margin, solved.CanvasRegion.Y + metrics.Margin);
    }

    // Moved just enough to stay inside, without changing what's stored, so regrowing the window puts it back.
    private static SolvedFloatingWindow PlaceFloating(FloatingWindow window, LayoutSize available, DockingMetrics metrics)
    {
        var width = Math.Min(window.Width, available.Width);
        var height = Math.Min(window.IsTitleOnly ? metrics.TitleLength : window.Height, available.Height);

        // Max last, so rounding can't put it outside when it exactly fills the room.
        var bounds = new LayoutRect(
            Math.Max(0, Math.Min(window.Position.X, available.Width - width)),
            Math.Max(0, Math.Min(window.Position.Y, available.Height - height)),
            width,
            height);

        return new SolvedFloatingWindow(window.WindowId, bounds, window.IsTitleOnly);
    }

    // Sides shrink together when they'd leave the canvas region too narrow. Wanted widths stay in the layout.
    private static double SideWidthScale(DockingLayout layout, double availableWidth, DockingMetrics metrics)
    {
        var widths = layout.Sides.Sum(side => side.Columns.Sum(column => column.Width));
        var margins = layout.Sides.Sum(side => OuterMargin(side, metrics));
        var room = Math.Max(0, availableWidth - metrics.MinimumCanvasRegionWidth - margins);

        return widths > room && widths > 0 ? room / widths : 1;
    }

    // Returns how much of the app window's width the side takes.
    private static double PlaceSide(SideLayout side, LayoutMeasurements measurements, DockingMetrics metrics,
        double widthScale, List<SolvedWindowGroup> groups)
    {
        var margin = OuterMargin(side, metrics);
        var gap = side.Kind == SideKind.Widgets ? metrics.WidgetGap : 0;
        var stackLength = Math.Max(0, measurements.Available.Height - margin * 2);
        var across = margin;

        foreach (var column in side.Columns)
        {
            var width = column.Width * widthScale;
            var entries = column.Groups
                .Select(group => new StackSolver.Entry(group.Share, group.IsTitleOnly, MinimumLength(group, measurements, metrics)))
                .ToList();
            var slots = StackSolver.Solve(entries, stackLength, gap, metrics.TitleLength);

            for (var index = 0; index < slots.Count; index++)
            {
                var bounds = OnEdge(side.Edge, measurements.Available, across, width, margin + slots[index].Start, slots[index].Length);
                groups.Add(new SolvedWindowGroup(column.Groups[index].WindowIds, side.Edge, side.Kind, bounds, slots[index].IsTitleOnly));
            }

            across += width;
        }

        return across;
    }

    private static double OuterMargin(SideLayout side, DockingMetrics metrics) =>
        side.Kind == SideKind.Widgets ? metrics.Margin : 0;

    private static double MinimumLength(WindowGroup group, LayoutMeasurements measurements, DockingMetrics metrics) =>
        group.WindowIds
            .Select(id => measurements.MinimumWindowLengths.GetValueOrDefault(id, metrics.DefaultMinimumWindowLength))
            .DefaultIfEmpty(metrics.DefaultMinimumWindowLength)
            .Max();

    // The one place that knows which way a side runs. A horizontal dock would only have to add its cases here,
    // and to the canvas region above.
    private static LayoutRect OnEdge(SideEdge edge, LayoutSize whole, double acrossStart, double acrossLength,
        double alongStart, double alongLength) => edge switch
        {
            SideEdge.Left => new LayoutRect(acrossStart, alongStart, acrossLength, alongLength),
            SideEdge.Right => new LayoutRect(whole.Width - acrossStart - acrossLength, alongStart, acrossLength, alongLength),
            _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null),
        };

    // A toolbar only takes a band from the edge it lies along. Sub-toolbars and floating items never count.
    private static LayoutInsets SafeArea(LayoutRect region, LayoutSize available, IReadOnlyList<SolvedToolbar> toolbars,
        double margin)
    {
        var left = region.X;
        var right = available.Width - region.Right;
        double top = 0, bottom = 0, leftBand = 0, rightBand = 0;

        foreach (var toolbar in toolbars)
        {
            var bounds = toolbar.Bounds;
            if (toolbar.Orientation == ToolbarOrientation.Horizontal)
            {
                if (toolbar.Anchor is ToolbarAnchor.TopLeft or ToolbarAnchor.TopMiddle or ToolbarAnchor.TopRight)
                    top = Math.Max(top, margin + bounds.Height);
                else if (toolbar.Anchor is ToolbarAnchor.BottomLeft or ToolbarAnchor.BottomMiddle or ToolbarAnchor.BottomRight)
                    bottom = Math.Max(bottom, margin + bounds.Height);
            }
            else if (toolbar.Anchor is ToolbarAnchor.TopLeft or ToolbarAnchor.LeftMiddle or ToolbarAnchor.BottomLeft)
            {
                leftBand = Math.Max(leftBand, margin + bounds.Width);
            }
            else if (toolbar.Anchor is ToolbarAnchor.TopRight or ToolbarAnchor.RightMiddle or ToolbarAnchor.BottomRight)
            {
                rightBand = Math.Max(rightBand, margin + bounds.Width);
            }
        }

        return new LayoutInsets(left + leftBand, top, right + rightBand, bottom);
    }
}
