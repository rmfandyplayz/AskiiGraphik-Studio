// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

/// <summary>
/// Changes to a layout, each a pure function from one layout to the next. Every drop and menu command goes
/// through here, so a preview can run the same edit as the real thing.
/// </summary>
public static partial class LayoutEdits
{
    /// <summary>Hides a toolbar. It keeps its place for when it's shown again.</summary>
    public static DockingLayout HideToolbar(DockingLayout layout, string toolbarId) =>
        WithToolbar(layout, toolbarId, toolbar => toolbar with { IsVisible = false });

    /// <summary>
    /// Shows a hidden toolbar where it was. An anchor holds one toolbar, so if its anchor has since been taken
    /// it floats at <paramref name="whereToFloatIfTaken"/> instead.
    /// </summary>
    public static DockingLayout ShowToolbar(DockingLayout layout, string toolbarId, LayoutPoint whereToFloatIfTaken) =>
        WithToolbar(layout, toolbarId, toolbar =>
        {
            var mustFloat = !toolbar.IsFloating && layout.IsAnchorTaken(toolbar.Anchor, toolbarId);
            return mustFloat
                ? Floated(toolbar, whereToFloatIfTaken)
                : toolbar with { IsVisible = true };
        });

    /// <summary>
    /// Puts a toolbar on an anchor. An edge middle sets its direction, and a corner keeps the one it had. Does
    /// nothing if another toolbar holds the anchor.
    /// </summary>
    public static DockingLayout MoveToolbarToAnchor(DockingLayout layout, string toolbarId, ToolbarAnchor anchor) =>
        layout.IsAnchorTaken(anchor, toolbarId)
            ? layout
            : WithToolbar(layout, toolbarId, toolbar => Placed(toolbar, anchor));

    /// <summary>
    /// Floats a toolbar with its top left at <paramref name="position"/>, keeping its direction. It goes last
    /// among the toolbars, so it's drawn over any other floating one it lands on.
    /// </summary>
    public static DockingLayout FloatToolbar(DockingLayout layout, string toolbarId, LayoutPoint position)
    {
        var toolbar = layout.Toolbars.FirstOrDefault(other => other.ToolbarId == toolbarId);
        if (toolbar is null)
            return layout;

        return layout with
        {
            Toolbars =
            [
                .. layout.Toolbars.Where(other => other.ToolbarId != toolbarId),
                Floated(toolbar, position),
            ],
        };
    }

    /// <summary>Changes which way a toolbar runs. Does nothing on an edge middle, whose toolbar follows the edge.</summary>
    public static DockingLayout SetToolbarOrientation(DockingLayout layout, string toolbarId,
        ToolbarOrientation orientation) =>
        WithToolbar(layout, toolbarId, toolbar =>
            toolbar.CanChooseOrientation ? toolbar with { Orientation = orientation } : toolbar);

    /// <summary>A toolbar moved to <paramref name="anchor"/>, following the direction it requires.</summary>
    internal static ToolbarPlacement Placed(ToolbarPlacement toolbar, ToolbarAnchor anchor) => toolbar with
    {
        Anchor = anchor,
        Orientation = ToolbarAnchors.RequiredOrientation(anchor) ?? toolbar.Orientation,
        IsVisible = true,
        FloatingAt = null,
    };

    // Leaving an anchor, it remembers which side faced the canvas, so its sub-toolbar stays on that side.
    private static ToolbarPlacement Floated(ToolbarPlacement toolbar, LayoutPoint position) => toolbar with
    {
        IsVisible = true,
        FloatingAt = position,
        SubToolbarSide = toolbar.IsFloating
            ? toolbar.SubToolbarSide
            : ToolbarAnchors.CanvasSide(toolbar.Anchor, toolbar.Orientation),
    };

    private static DockingLayout WithToolbar(DockingLayout layout, string toolbarId,
        Func<ToolbarPlacement, ToolbarPlacement> change) => layout with
        {
            Toolbars = [.. layout.Toolbars.Select(toolbar => toolbar.ToolbarId == toolbarId ? change(toolbar) : toolbar)],
        };
}
