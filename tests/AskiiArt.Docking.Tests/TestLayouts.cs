// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using CsCheck;

namespace AskiiArt.Docking.Tests;

/// <summary>Shared layouts, and generators of any layout at all, valid or not.</summary>
internal static class TestLayouts
{
    public static readonly string[] WindowIds = ["w0", "w1", "w2", "w3", "w4", "w5", "w6", "w7", "w8", "w9"];

    public static readonly DockingMetrics Metrics = new(
        Margin: 8,
        WidgetGap: 8,
        TitleLength: 28,
        DefaultMinimumWindowLength: 100,
        MinimumCanvasRegionWidth: 320,
        ToolbarGap: 8,
        MinimumToolbarLength: 80,
        MiddleToolbarPriority: 1.25,
        ToolbarZoneMinimumShare: 0.3,
        SubToolbarLengthFactor: 1.5,
        MinimumWindowWidth: 160,
        DefaultWindowWidth: 240,
        DefaultFloatingWindowHeight: 280,
        WindowEdgeStripWidth: 24,
        WindowEdgePlaceholderWidth: 48);

    public static readonly DockingCatalog Catalog = new(
        WindowIds,
        [
            new ToolbarPlacement("file", ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("tools", ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("edit", ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
            new ToolbarPlacement("view", ToolbarAnchor.BottomRight, ToolbarOrientation.Horizontal),
        ],
        DefaultSideWidth: 240);

    public static readonly IReadOnlyDictionary<string, LayoutSize> ToolbarSizes =
        new Dictionary<string, LayoutSize>(StringComparer.Ordinal)
        {
            ["file"] = new(150, 40),
            ["tools"] = new(120, 40),
            ["edit"] = new(290, 40),
            ["view"] = new(370, 40),
        };

    private static readonly Gen<string> _ids =
        Gen.OneOfConst([.. WindowIds, "gone"]);

    private static readonly Gen<WindowGroup> _groups = Gen.Select(
        _ids.List[0, 3],
        Gen.Double[-1, 5],
        Gen.Bool,
        (ids, share, isTitleOnly) => new WindowGroup(ids, share, isTitleOnly));

    private static readonly Gen<SideColumn> _columns = Gen.Select(
        Gen.Double[-10, 600],
        _groups.List[0, 6],
        (width, groups) => new SideColumn(width, groups));

    private static readonly Gen<SideLayout> _sides = Gen.Select(
        Gen.Enum<SideEdge>(),
        Gen.Enum<SideKind>(),
        _columns.List[0, 3],
        (edge, kind, columns) => new SideLayout(edge, kind, columns));

    private static readonly Gen<LayoutPoint?> _floating = Gen.Select(
        Gen.Bool,
        Gen.Double[-500, 3000],
        Gen.Double[-500, 3000],
        (isFloating, x, y) => isFloating ? new LayoutPoint(x, y) : (LayoutPoint?)null);

    private static readonly Gen<ToolbarPlacement> _toolbars = Gen.Select(
        Gen.OneOfConst("file", "tools", "edit", "view", "gone"),
        Gen.Enum<ToolbarAnchor>(),
        Gen.Enum<ToolbarOrientation>(),
        Gen.Bool,
        _floating,
        (id, anchor, orientation, isVisible, floatingAt) =>
            new ToolbarPlacement(id, anchor, orientation, isVisible, floatingAt));

    private static readonly Gen<ClosedWindow> _closed = Gen.Select(
        _ids,
        Gen.Enum<SideEdge>(),
        Gen.Int[-1, 6],
        Gen.Double[-1, 5],
        Gen.Double[-10, 600],
        (id, edge, position, share, width) => new ClosedWindow(id, edge, position, share, width));

    private static readonly Gen<FloatingWindow> _floatingWindows = Gen.Select(
        _ids,
        Gen.Double[-500, 3000],
        Gen.Double[-500, 3000],
        Gen.Double[-10, 800],
        Gen.Double[-10, 800],
        Gen.Bool,
        (id, x, y, width, height, isTitleOnly) =>
            new FloatingWindow(id, new LayoutPoint(x, y), width, height, isTitleOnly));

    /// <summary>Any layout, with duplicates, unknown ids and silly numbers.</summary>
    public static readonly Gen<DockingLayout> Anything = Gen.Select(
        _sides.List[0, 3],
        _toolbars.List[0, 6],
        _closed.List[0, 4],
        _floatingWindows.List[0, 3],
        (sides, toolbars, closed, floating) =>
            new DockingLayout(sides, toolbars, closed) { FloatingWindows = floating });

    /// <summary>Any layout that keeps the rules.</summary>
    public static readonly Gen<DockingLayout> Valid = Anything.Select(layout => LayoutRepair.Repair(layout, Catalog));

    /// <summary>Any app window at least as big as the app's smallest allowed one, with some smaller screens.</summary>
    public static readonly Gen<LayoutSize> WindowSizes = Gen.Select(
        Gen.Double[800, 4000],
        Gen.Double[300, 3000],
        (width, height) => new LayoutSize(width, height));

    public static LayoutMeasurements Measure(LayoutSize available) => Measure(available, ToolbarSizes);

    public static LayoutMeasurements Measure(LayoutSize available, IReadOnlyDictionary<string, LayoutSize> toolbars) =>
        new(available, new Dictionary<string, double>(StringComparer.Ordinal), toolbars);

    /// <summary>How big each toolbar wants to be, whichever way it runs.</summary>
    public static Dictionary<string, LayoutSize> Sizes(DockingLayout layout, Func<string, double> lengths)
    {
        var sizes = new Dictionary<string, LayoutSize>(StringComparer.Ordinal);
        foreach (var toolbar in layout.Toolbars)
        {
            sizes[toolbar.ToolbarId] = toolbar.Orientation == ToolbarOrientation.Horizontal
                ? new LayoutSize(lengths(toolbar.ToolbarId), 40)
                : new LayoutSize(40, lengths(toolbar.ToolbarId));
        }

        return sizes;
    }

    public static string Text(DockingLayout layout) => LayoutJson.Write(layout);

    /// <summary>A layout with just these widgets on one side.</summary>
    public static DockingLayout Widgets(SideEdge edge, params WindowGroup[] groups) => new(
        [new SideLayout(edge, SideKind.Widgets, [new SideColumn(240, groups)])],
        [],
        []);
}
