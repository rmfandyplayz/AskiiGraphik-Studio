// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Shell.Menus;

namespace AskiiArt.App.Shell.Toolbars;

/// <summary>
/// Which actions the toolbar shows, in order, using the same entries as a menu (an action or a
/// separator). Plain data, like <see cref="MenuLayout"/>, so the owner's design or user
/// customization (M8) can replace it.
/// </summary>
internal sealed record ToolbarLayout(IReadOnlyList<MenuEntry> Entries)
{
    public static ToolbarLayout CreateDefault() => new(
    [
        new ActionMenuEntry(DocumentActionIds.New),
        new SeparatorMenuEntry(),
        new ActionMenuEntry(EditActionIds.Undo),
        new ActionMenuEntry(EditActionIds.Redo),
        new SeparatorMenuEntry(),
        new ActionMenuEntry(DocumentActionIds.ResizeCanvas),
        new ActionMenuEntry(DocumentActionIds.Font),
        new SeparatorMenuEntry(),
        new ActionMenuEntry(CanvasActionIds.ZoomOut),
        new ActionMenuEntry(CanvasActionIds.ZoomIn),
        new ActionMenuEntry(CanvasActionIds.ActualSize),
        new ActionMenuEntry(CanvasActionIds.ShowGrid)
    ]);

    /// <summary>Every action id on the toolbar.</summary>
    public IEnumerable<string> ActionIds() => Entries.OfType<ActionMenuEntry>().Select(entry => entry.ActionId);
}
