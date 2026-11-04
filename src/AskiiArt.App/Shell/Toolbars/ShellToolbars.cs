// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;

namespace AskiiArt.App.Shell.Toolbars;

/// <summary>The toolbars the app has, by id. Placeholder sets until the toolbar customizer (M8).</summary>
internal sealed record ShellToolbars(IReadOnlyDictionary<string, ToolbarLayout> Layouts)
{
    public static ShellToolbars CreateDefault() => new(new Dictionary<string, ToolbarLayout>(StringComparer.Ordinal)
    {
        [ShellDockingIds.FileToolbar] = ToolbarLayout.CreateFile(),
        [ShellDockingIds.ToolsToolbar] = ToolbarLayout.CreateTools(),
        [ShellDockingIds.EditToolbar] = ToolbarLayout.CreateEdit(),
        [ShellDockingIds.ViewToolbar] = ToolbarLayout.CreateView(),
    });

    /// <summary>Every action id on every toolbar.</summary>
    public IEnumerable<string> ActionIds() => Layouts.Values.SelectMany(layout => layout.ActionIds());
}
