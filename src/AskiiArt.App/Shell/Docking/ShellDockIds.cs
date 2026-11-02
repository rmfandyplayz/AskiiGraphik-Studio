// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The ids of the shell's panels and of the docks that hold them. They're saved with the layout, so
/// renaming one makes older layouts fall back to the default.
/// </summary>
internal static class ShellDockIds
{
    public const string MenuBarPanel = "panel.menuBar";
    public const string ToolbarPanel = "panel.toolbar";
    public const string StatusBarPanel = "panel.statusBar";
    public const string HistoryPanel = "panel.history";
    public const string LayersPanel = "panel.layers";

    public const string MenuBarDock = "dock.menuBar";
    public const string ToolbarDock = "dock.toolbar";
    public const string StatusBarDock = "dock.statusBar";

    /// <summary>The empty dock whose space is what widgets leave free for the canvas (ADR 0025).</summary>
    public const string CanvasDock = "dock.canvas";

    public static string PanelTitle(string panelId) => panelId switch
    {
        MenuBarPanel => Strings.PanelMenuBar,
        ToolbarPanel => Strings.PanelToolbar,
        StatusBarPanel => Strings.PanelStatusBar,
        HistoryPanel => Strings.PanelHistory,
        LayersPanel => Strings.PanelLayers,
        _ => throw new ArgumentOutOfRangeException(nameof(panelId), panelId, null),
    };
}
