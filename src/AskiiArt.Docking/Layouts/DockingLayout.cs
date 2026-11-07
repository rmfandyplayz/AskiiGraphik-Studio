// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json.Serialization;

namespace AskiiArt.Docking.Layouts;

/// <summary>A point in logical pixels, measured from the top left of the space between the strips.</summary>
/// <param name="X">How far from the left.</param>
/// <param name="Y">How far from the top.</param>
public readonly record struct LayoutPoint(double X, double Y);

/// <summary>
/// Where every window and toolbar is meant to be (docs/DOCKING.md). This is what the user asked for, not
/// what fits right now: <see cref="Solving.LayoutSolver"/> fits it to the app window without changing it,
/// so shrinking the window and growing it again restores everything.
/// </summary>
/// <param name="Sides">The sides that hold windows. A side that's absent is empty.</param>
/// <param name="Toolbars">Every toolbar, shown or hidden.</param>
/// <param name="ClosedWindows">Windows that are closed, and where they were.</param>
public sealed record DockingLayout(
    IReadOnlyList<SideLayout> Sides,
    IReadOnlyList<ToolbarPlacement> Toolbars,
    IReadOnlyList<ClosedWindow> ClosedWindows)
{
    /// <summary>The most widgets a side can hold.</summary>
    public const int MaximumWidgetsOnASide = 4;

    private readonly IReadOnlyList<FloatingWindow> _floatingWindows = [];

    /// <summary>Windows on their own above everything, from back to front.</summary>
    public IReadOnlyList<FloatingWindow> FloatingWindows
    {
        get => _floatingWindows;

        // Reading a layout saved before there were floating windows passes null, whatever the initializer says.
        init => _floatingWindows = value ?? [];
    }

    private readonly IReadOnlyList<FloatingSize> _floatingSizes = [];

    /// <summary>The size each window last had while it floated, for floating it again.</summary>
    public IReadOnlyList<FloatingSize> FloatingSizes
    {
        get => _floatingSizes;

        // Reading a layout saved before these were kept passes null, whatever the initializer says.
        init => _floatingSizes = value ?? [];
    }

    /// <summary>The size the window has while it floats, or last had, or null if it's never floated.</summary>
    public FloatingSize? FloatingSizeOf(string windowId) =>
        FloatingWindowNamed(windowId) is { } floating
            ? new FloatingSize(windowId, floating.Width, floating.Height)
            : FloatingSizes.FirstOrDefault(size => size.WindowId == windowId);

    /// <summary>The side on <paramref name="edge"/>, or null when it's empty.</summary>
    public SideLayout? Side(SideEdge edge) => Sides.FirstOrDefault(side => side.Edge == edge);

    /// <summary>Every open window's id.</summary>
    public IEnumerable<string> OpenWindowIds() =>
        Sides.SelectMany(side => side.Columns)
            .SelectMany(column => column.Groups)
            .SelectMany(group => group.WindowIds)
            .Concat(FloatingWindows.Select(window => window.WindowId));

    /// <summary>Whether the window is open anywhere.</summary>
    public bool IsWindowOpen(string windowId) => OpenWindowIds().Contains(windowId, StringComparer.Ordinal);

    /// <summary>What kind of window it is, or null if it's closed.</summary>
    public WindowKind? KindOf(string windowId)
    {
        if (FloatingWindowNamed(windowId) is not null)
            return WindowKind.Floating;

        return PlaceOf(windowId)?.Kind switch
        {
            SideKind.Widgets => WindowKind.Widget,
            SideKind.Dock => WindowKind.Dock,
            _ => null,
        };
    }

    /// <summary>Where the window is on a side, or null if it's floating or closed.</summary>
    public WindowPlace? PlaceOf(string windowId)
    {
        foreach (var side in Sides)
        {
            for (var column = 0; column < side.Columns.Count; column++)
            {
                var groups = side.Columns[column].Groups;
                for (var position = 0; position < groups.Count; position++)
                {
                    var tab = IndexOf(groups[position].WindowIds, windowId);
                    if (tab >= 0)
                        return new WindowPlace(side.Edge, side.Kind, column, position, tab);
                }
            }
        }

        return null;
    }

    /// <summary>The group holding the window, or null if it isn't on a side.</summary>
    public WindowGroup? GroupHolding(string windowId) => PlaceOf(windowId) is { } place
        ? Side(place.Edge)!.Columns[place.Column].Groups[place.Position]
        : null;

    /// <summary>The window if it's floating, or null if it isn't.</summary>
    public FloatingWindow? FloatingWindowNamed(string windowId) =>
        FloatingWindows.FirstOrDefault(window => window.WindowId == windowId);

    /// <summary>Whether the toolbar is showing.</summary>
    public bool IsToolbarVisible(string toolbarId) =>
        Toolbars.Any(toolbar => toolbar.IsVisible && toolbar.ToolbarId == toolbarId);

    /// <summary>
    /// Whether a toolbar other than <paramref name="toolbarId"/> holds the anchor. Only a showing, anchored
    /// toolbar does: a hidden one gives way, and is floated if it's shown again.
    /// </summary>
    public bool IsAnchorTaken(ToolbarAnchor anchor, string toolbarId) => Toolbars.Any(toolbar =>
        toolbar.IsVisible && !toolbar.IsFloating && toolbar.Anchor == anchor && toolbar.ToolbarId != toolbarId);

    private static int IndexOf(IReadOnlyList<string> windowIds, string windowId)
    {
        for (var index = 0; index < windowIds.Count; index++)
        {
            if (windowIds[index] == windowId)
                return index;
        }

        return -1;
    }
}

/// <summary>
/// One side of the app window. A widget side is a dock side with stricter rules: one column, one window per
/// group and at most <see cref="DockingLayout.MaximumWidgetsOnASide"/> groups.
/// </summary>
/// <param name="Edge">Which edge of the app window it's on.</param>
/// <param name="Kind">Whether it holds widgets or a dock.</param>
/// <param name="Columns">Its columns, from the app window's edge inwards.</param>
public sealed record SideLayout(SideEdge Edge, SideKind Kind, IReadOnlyList<SideColumn> Columns);

/// <summary>A column on a side: groups stacked along the side's length.</summary>
/// <param name="Width">How wide the user wants it, in logical pixels.</param>
/// <param name="Groups">Its groups, in order from the start of the side.</param>
public sealed record SideColumn(double Width, IReadOnlyList<WindowGroup> Groups);

/// <summary>Windows sharing one place in a column, as tabs. A widget's group has exactly one.</summary>
/// <param name="WindowIds">The windows, in tab order.</param>
/// <param name="Share">How much of the column's free length it wants, relative to its neighbours.</param>
/// <param name="IsTitleOnly">Whether the user squeezed it down to its title.</param>
/// <param name="OpenLength">How long it was before it was squeezed to its title, for opening it again. Null if unknown.</param>
/// <param name="SelectedWindowId">The tab that's showing. Null, or a window that isn't in the group, means the first.</param>
public sealed record WindowGroup(
    IReadOnlyList<string> WindowIds,
    double Share = 1,
    bool IsTitleOnly = false,
    double? OpenLength = null,
    string? SelectedWindowId = null)
{
    /// <summary>The tab that's showing. Empty only for a group with no windows, which a repaired layout never has.</summary>
    [JsonIgnore]
    public string Selected
    {
        get
        {
            if (SelectedWindowId is { } selected && Holds(selected))
                return selected;

            return WindowIds.Count > 0 ? WindowIds[0] : string.Empty;
        }
    }

    /// <summary>Whether the window is one of its tabs.</summary>
    public bool Holds(string windowId) => WindowIds.Contains(windowId, StringComparer.Ordinal);
}

/// <summary>A window on its own, which can be anywhere inside the app window.</summary>
/// <param name="WindowId">Which window.</param>
/// <param name="Position">Where its top left goes. The solver moves it just enough to stay inside.</param>
/// <param name="Width">How wide the user wants it.</param>
/// <param name="Height">How tall the user wants it, for whenever it isn't title-only.</param>
/// <param name="IsTitleOnly">Whether the user squeezed it down to its title.</param>
public sealed record FloatingWindow(
    string WindowId,
    LayoutPoint Position,
    double Width,
    double Height,
    bool IsTitleOnly = false);

/// <summary>The size a window last had while it floated.</summary>
/// <param name="WindowId">Which window.</param>
/// <param name="Width">How wide it was.</param>
/// <param name="Height">How tall it was, for whenever it isn't title-only.</param>
public sealed record FloatingSize(string WindowId, double Width, double Height);

/// <summary>Where a toolbar goes. A hidden toolbar keeps its place for when it's shown again.</summary>
/// <param name="ToolbarId">Which toolbar.</param>
/// <param name="Anchor">Which of the 8 places it's on. A floating toolbar doesn't hold this anchor.</param>
/// <param name="Orientation">Which way its buttons run.</param>
/// <param name="IsVisible">Whether it's showing.</param>
/// <param name="FloatingAt">Where its top left goes if it's floating, or null if it's on its anchor.</param>
/// <param name="SubToolbarSide">
/// Where its sub-toolbar goes while it's floating: the side that faced the canvas when it was last on an anchor.
/// </param>
public sealed record ToolbarPlacement(
    string ToolbarId,
    ToolbarAnchor Anchor,
    ToolbarOrientation Orientation,
    bool IsVisible = true,
    LayoutPoint? FloatingAt = null,
    SubToolbarSide SubToolbarSide = SubToolbarSide.BelowOrRight)
{
    /// <summary>Whether it's floating rather than on its anchor.</summary>
    [JsonIgnore]
    public bool IsFloating => FloatingAt is not null;

    /// <summary>Whether the user can choose its direction. An edge middle's toolbar follows the edge.</summary>
    [JsonIgnore]
    public bool CanChooseOrientation => IsFloating || ToolbarAnchors.RequiredOrientation(Anchor) is null;
}

/// <summary>A closed window, and enough about where it was to put it back.</summary>
/// <param name="WindowId">Which window.</param>
/// <param name="Edge">The side it was on.</param>
/// <param name="Position">Its place in its column's stack.</param>
/// <param name="Share">The group's share of the stack.</param>
/// <param name="SideWidth">The column's width, in case the side has since emptied.</param>
/// <param name="WasFloating">Where it was if it was floating, in which case the rest doesn't apply.</param>
/// <param name="Column">Which of a dock's columns it was in, from the app window's edge. A widget's is 0.</param>
/// <param name="TabmateId">A window that was a tab beside it in a dock, for joining it again. Null if it had none.</param>
public sealed record ClosedWindow(
    string WindowId,
    SideEdge Edge,
    int Position,
    double Share,
    double SideWidth,
    FloatingWindow? WasFloating = null,
    int Column = 0,
    string? TabmateId = null);
