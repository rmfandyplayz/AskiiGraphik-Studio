// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The ids of the shell's panels and of the canvas's dock. They're saved with the layout, so renaming
/// one makes older layouts fall back to the default.
/// </summary>
internal static class ShellDockIds
{
    public const string ToolbarPanel = "panel.toolbar";
    public const string HistoryPanel = "panel.history";
    public const string LayersPanel = "panel.layers";

    /// <summary>The empty dock whose space is what widgets leave free for the canvas (ADR 0025).</summary>
    public const string CanvasDock = "dock.canvas";

    /// <summary>The canvas dock's docking group, which no panel belongs to, so no panel can land on it.</summary>
    public const string CanvasDockGroup = "group.canvas";

    public static bool IsPanel(string panelId) => panelId is ToolbarPanel or HistoryPanel or LayersPanel;

    public static string PanelTitle(string panelId) => panelId switch
    {
        ToolbarPanel => Strings.PanelToolbar,
        HistoryPanel => Strings.PanelHistory,
        LayersPanel => Strings.PanelLayers,
        _ => throw new ArgumentOutOfRangeException(nameof(panelId), panelId, null),
    };
}
