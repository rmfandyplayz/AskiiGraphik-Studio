// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>The sizes the layout rules use, from the theme's tokens.</summary>
/// <param name="Margin">Around widgets, and around toolbars in the canvas region, so the two line up.</param>
/// <param name="WidgetGap">Between widgets in a stack.</param>
/// <param name="TitleLength">How long a title-only window is.</param>
/// <param name="DefaultMinimumWindowLength">The smallest size for a window that hasn't stated one.</param>
/// <param name="MinimumCanvasRegionWidth">Sides shrink before the canvas region gets narrower than this.</param>
/// <param name="ToolbarGap">Between toolbars sharing an edge.</param>
/// <param name="MinimumToolbarLength">A toolbar shorter than this is left out until there's room.</param>
/// <param name="MiddleToolbarPriority">How much more of a shared edge a middle toolbar gets than a corner one.</param>
/// <param name="ToolbarZoneMinimumShare">How much of its edge a middle's drop zone always gets, from 0 to 1.</param>
public sealed record DockingMetrics(
    double Margin,
    double WidgetGap,
    double TitleLength,
    double DefaultMinimumWindowLength,
    double MinimumCanvasRegionWidth,
    double ToolbarGap,
    double MinimumToolbarLength,
    double MiddleToolbarPriority,
    double ToolbarZoneMinimumShare);

/// <summary>
/// Room kept on an anchor as if a toolbar were there, and ahead of any toolbar. A drag holds its drop zones this
/// way, so that they're never covered. A corner's is square, and a middle's takes whatever its edge has free.
/// </summary>
/// <param name="Anchor">The anchor. Nothing should be on it.</param>
/// <param name="Length">How long it is along its edge, at the least.</param>
/// <param name="Thickness">How thick it is.</param>
public sealed record ReservedToolbarSpot(ToolbarAnchor Anchor, double Length, double Thickness);

/// <summary>What the solver needs to know about right now.</summary>
/// <param name="Available">The space between the menu and status strips.</param>
/// <param name="MinimumWindowLengths">The smallest size each window can show its contents at, by id.</param>
/// <param name="ToolbarSizes">How big each toolbar wants to be, by id. One with no size isn't shown.</param>
public sealed record LayoutMeasurements(
    LayoutSize Available,
    IReadOnlyDictionary<string, double> MinimumWindowLengths,
    IReadOnlyDictionary<string, LayoutSize> ToolbarSizes);
