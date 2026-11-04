// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Where everything goes right now.</summary>
/// <param name="WindowGroups">Every window group on a side.</param>
/// <param name="Toolbars">Every toolbar that's showing.</param>
/// <param name="CanvasRegion">What the sides leave free. Toolbars live here.</param>
/// <param name="SafeArea">How much of each edge is taken from the canvas, for centring and scroll limits.</param>
public sealed record SolvedLayout(
    IReadOnlyList<SolvedWindowGroup> WindowGroups,
    IReadOnlyList<SolvedToolbar> Toolbars,
    LayoutRect CanvasRegion,
    LayoutInsets SafeArea);

/// <summary>A window group's place.</summary>
/// <param name="WindowIds">The windows in it, in tab order.</param>
/// <param name="Edge">The side it's on.</param>
/// <param name="Kind">What that side holds.</param>
/// <param name="Bounds">Its rectangle.</param>
/// <param name="IsTitleOnly">Whether it shows only its title right now, by choice or for lack of room.</param>
public sealed record SolvedWindowGroup(
    IReadOnlyList<string> WindowIds,
    SideEdge Edge,
    SideKind Kind,
    LayoutRect Bounds,
    bool IsTitleOnly);

/// <summary>A toolbar's place.</summary>
/// <param name="ToolbarId">Which toolbar.</param>
/// <param name="Anchor">The anchor it's on, or null if it's floating.</param>
/// <param name="Orientation">Which way its buttons run.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record SolvedToolbar(
    string ToolbarId,
    ToolbarAnchor? Anchor,
    ToolbarOrientation Orientation,
    LayoutRect Bounds);
