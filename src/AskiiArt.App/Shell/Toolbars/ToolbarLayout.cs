// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Layers;
using AskiiArt.App.Ramps;
using AskiiArt.App.Shell.Menus;
using AskiiArt.Core.Tools;

namespace AskiiArt.App.Shell.Toolbars;

/// <summary>
/// Which actions a toolbar shows, in order, using the same entries as a menu (an action or a
/// separator). Plain data, like <see cref="MenuLayout"/>, so the owner's design or the toolbar
/// customizer (M8) can replace it.
/// </summary>
internal sealed record ToolbarLayout(IReadOnlyList<MenuEntry> Entries)
{
    public static ToolbarLayout CreateFile() => new(
    [
        new ActionMenuEntry(DocumentActionIds.New),
        new ActionMenuEntry(DocumentActionIds.Open),
        new ActionMenuEntry(DocumentActionIds.Save)
    ]);

    /// <summary>One button per tool group (ADR 0049).</summary>
    public static ToolbarLayout CreateTools() => new(
    [
        new ActionMenuEntry(ToolGroupActionIds.Hand),
        new ActionMenuEntry(ToolGroupActionIds.Text),
        new ActionMenuEntry(ToolGroupActionIds.Brush),
        new ActionMenuEntry(ToolGroupActionIds.Eraser)
    ]);

    /// <summary>
    /// A tool group's sub-toolbar, beside the Tools toolbar while one of its tools is the active one: a button per
    /// tool, in the group's order, which is also the order of their number keys.
    /// </summary>
    public static ToolbarLayout CreateToolGroupSubToolbar(ToolGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new([.. group.ToolIds.Select(toolId => new ActionMenuEntry(ToolActionIds.For(toolId)))]);
    }

    public static ToolbarLayout CreateEdit() => new(
    [
        new ActionMenuEntry(EditActionIds.Undo),
        new ActionMenuEntry(EditActionIds.Redo),
        new SeparatorMenuEntry(),
        new ActionMenuEntry(DocumentActionIds.ResizeCanvas),
        new ActionMenuEntry(DocumentActionIds.Font)
    ]);

    public static ToolbarLayout CreateView() => new(
    [
        new ActionMenuEntry(CanvasActionIds.ZoomOut),
        new ActionMenuEntry(CanvasActionIds.ZoomIn),
        new ActionMenuEntry(CanvasActionIds.ActualSize),
        new ActionMenuEntry(CanvasActionIds.ShowGrid)
    ]);

    /// <summary>The buttons under the Layers panel's list.</summary>
    public static ToolbarLayout CreateLayersPanel() => new(
    [
        new ActionMenuEntry(LayerActionIds.New),
        new ActionMenuEntry(LayerActionIds.Duplicate),
        new ActionMenuEntry(LayerActionIds.Delete),
        new ActionMenuEntry(LayerActionIds.Rename),
        new ActionMenuEntry(LayerActionIds.MoveUp),
        new ActionMenuEntry(LayerActionIds.MoveDown),
        new ActionMenuEntry(LayerActionIds.MergeDown)
    ]);

    /// <summary>The buttons below the Ramps window's list (ADR 0052).</summary>
    public static ToolbarLayout CreateRampsPanel() => new(
    [
        new ActionMenuEntry(RampActionIds.New),
        new ActionMenuEntry(RampActionIds.Duplicate),
        new ActionMenuEntry(RampActionIds.Delete),
        new ActionMenuEntry(RampActionIds.SortByDarkness),
        new ActionMenuEntry(RampActionIds.Import),
        new ActionMenuEntry(RampActionIds.Export)
    ]);

    /// <summary>Every action id on this toolbar.</summary>
    public IEnumerable<string> ActionIds() => Entries.OfType<ActionMenuEntry>().Select(entry => entry.ActionId);
}
