// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

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
public sealed record DockingMetrics(
    double Margin,
    double WidgetGap,
    double TitleLength,
    double DefaultMinimumWindowLength,
    double MinimumCanvasRegionWidth,
    double ToolbarGap,
    double MinimumToolbarLength,
    double MiddleToolbarPriority);

/// <summary>What the solver needs to know about right now.</summary>
/// <param name="Available">The space between the menu and status strips.</param>
/// <param name="MinimumWindowLengths">The smallest size each window can show its contents at, by id.</param>
/// <param name="ToolbarSizes">How big each toolbar wants to be, by id. One with no size isn't shown.</param>
public sealed record LayoutMeasurements(
    LayoutSize Available,
    IReadOnlyDictionary<string, double> MinimumWindowLengths,
    IReadOnlyDictionary<string, LayoutSize> ToolbarSizes);
