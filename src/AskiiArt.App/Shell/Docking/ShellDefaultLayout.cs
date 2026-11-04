// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;

namespace AskiiArt.App.Shell.Docking;

/// <summary>Where everything starts, and what Reset Layout goes back to (docs/DOCKING.md §9).</summary>
internal static class ShellDefaultLayout
{
    public static DockingLayout Create(double widgetWidth) => new(
    [
        new SideLayout(SideEdge.Right, SideKind.Widgets,
        [
            new SideColumn(widgetWidth,
            [
                new WindowGroup([ShellDockingIds.LayersPanel]),
                new WindowGroup([ShellDockingIds.HistoryPanel]),
            ]),
        ]),
    ],
    [
        new ToolbarPlacement(ShellDockingIds.FileToolbar, ToolbarAnchor.TopLeft, ToolbarOrientation.Horizontal),
        new ToolbarPlacement(ShellDockingIds.ToolsToolbar, ToolbarAnchor.BottomMiddle, ToolbarOrientation.Horizontal),
        new ToolbarPlacement(ShellDockingIds.EditToolbar, ToolbarAnchor.TopRight, ToolbarOrientation.Horizontal),
        new ToolbarPlacement(ShellDockingIds.ViewToolbar, ToolbarAnchor.BottomRight, ToolbarOrientation.Horizontal),
    ],
    []);
}
