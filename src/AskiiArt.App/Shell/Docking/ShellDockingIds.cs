// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// The ids of the shell's windows and toolbars. They're saved with the layout, so renaming one makes
/// a saved layout lose it, and the window or toolbar comes back in its default place.
/// </summary>
internal static class ShellDockingIds
{
    public const string LayersPanel = "panel.layers";
    public const string HistoryPanel = "panel.history";

    // Debug builds only.
    public const string SampleWindow1 = "panel.sample1";
    public const string SampleWindow2 = "panel.sample2";
    public const string SampleWindow3 = "panel.sample3";

    public const string FileToolbar = "toolbar.file";
    public const string ToolsToolbar = "toolbar.tools";
    public const string EditToolbar = "toolbar.edit";
    public const string ViewToolbar = "toolbar.view";
}
