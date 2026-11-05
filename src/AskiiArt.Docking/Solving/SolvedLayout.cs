// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.Docking.Solving;

/// <summary>Where everything goes right now.</summary>
/// <param name="WindowGroups">Every window group on a side.</param>
/// <param name="Toolbars">Every toolbar that's showing.</param>
/// <param name="CanvasRegion">What the sides leave free. Toolbars live here.</param>
/// <param name="SafeArea">How much of each edge is taken from the canvas, for centring and scroll limits.</param>
/// <param name="SubToolbar">Where the sub-toolbar goes. Null if there's none, or no room for it.</param>
/// <param name="FloatingWindows">Every floating window, from back to front.</param>
public sealed record SolvedLayout(
    IReadOnlyList<SolvedWindowGroup> WindowGroups,
    IReadOnlyList<SolvedToolbar> Toolbars,
    LayoutRect CanvasRegion,
    LayoutInsets SafeArea,
    SolvedSubToolbar? SubToolbar,
    IReadOnlyList<SolvedFloatingWindow> FloatingWindows)
{
    /// <summary>Where a window is and whether it's title-only, if it's open.</summary>
    public (LayoutRect Bounds, bool IsTitleOnly)? PlaceOfWindow(string windowId)
    {
        if (FloatingWindows.FirstOrDefault(window => window.WindowId == windowId) is { } floating)
            return (floating.Bounds, floating.IsTitleOnly);

        var group = WindowGroups.FirstOrDefault(other => other.WindowIds.Contains(windowId, StringComparer.Ordinal));
        return group is null ? null : (group.Bounds, group.IsTitleOnly);
    }
}

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

/// <summary>A floating window's place, kept inside the app window.</summary>
/// <param name="WindowId">Which window.</param>
/// <param name="Bounds">Its rectangle.</param>
/// <param name="IsTitleOnly">Whether it shows only its title.</param>
public sealed record SolvedFloatingWindow(string WindowId, LayoutRect Bounds, bool IsTitleOnly);

/// <summary>A sub-toolbar's place.</summary>
/// <param name="ParentToolbarId">The toolbar it belongs to.</param>
/// <param name="Orientation">Which way its buttons run, which is the way its parent's do.</param>
/// <param name="Side">Which side of its parent it's on.</param>
/// <param name="Bounds">Its rectangle.</param>
public sealed record SolvedSubToolbar(
    string ParentToolbarId,
    ToolbarOrientation Orientation,
    SubToolbarSide Side,
    LayoutRect Bounds);

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
