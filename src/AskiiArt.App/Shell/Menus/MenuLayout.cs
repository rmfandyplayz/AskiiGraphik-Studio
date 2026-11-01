// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Resources;

namespace AskiiArt.App.Shell.Menus;

/// <summary>An entry in a menu: an action, or a separator.</summary>
internal abstract record MenuEntry;

/// <summary>
/// A menu item that invokes an action.
/// </summary>
/// <param name="ActionId">The action to invoke.</param>
/// <param name="LabelWithAppNameKey">
/// Optional. The key of a label that replaces the action's own label here, where <c>{0}</c> is
/// the app's display name (for example "About {0}" in the macOS application menu).
/// </param>
internal sealed record ActionMenuEntry(string ActionId, string? LabelWithAppNameKey = null) : MenuEntry;

internal sealed record SeparatorMenuEntry : MenuEntry;

/// <summary>A top-level menu, such as File or Help.</summary>
internal sealed record MenuDefinition(string LabelKey, IReadOnlyList<MenuEntry> Entries);

/// <summary>
/// Which menus exist and which actions they contain. This is plain data, so it can be tested
/// without a UI and later replaced by the owner's design or by user customization.
/// </summary>
/// <param name="ApplicationMenu">
/// The macOS application menu (the one named after the app). Avalonia adds the standard macOS
/// items below these entries: Services, Hide, Hide Others, Show All and Quit. Empty elsewhere.
/// </param>
/// <param name="WindowMenus">The menu bar: the macOS menu bar, or the bar inside the window.</param>
/// <param name="IsSystemMenuBar">
/// True on macOS, where the menus live in the system menu bar and it handles their shortcuts.
/// </param>
internal sealed record MenuLayout(
    IReadOnlyList<MenuEntry> ApplicationMenu,
    IReadOnlyList<MenuDefinition> WindowMenus,
    bool IsSystemMenuBar)
{
    public static MenuLayout CreateDefault(bool isMacOS, bool includeDebugMenu)
    {
        var windowMenus = new List<MenuDefinition>();
        IReadOnlyList<MenuEntry> applicationMenu;

        var editMenu = new MenuDefinition(nameof(Strings.MenuEdit),
        [
            new ActionMenuEntry(EditActionIds.Undo),
            new ActionMenuEntry(EditActionIds.Redo)
        ]);

        // ASCII Only sits here until the owner designs document options.
        var documentMenu = new MenuDefinition(nameof(Strings.MenuDocument),
        [
            new ActionMenuEntry(DocumentActionIds.ResizeCanvas),
            new ActionMenuEntry(DocumentActionIds.Font),
            new SeparatorMenuEntry(),
            new ActionMenuEntry(DocumentActionIds.AsciiOnly)
        ]);

        var viewMenu = new MenuDefinition(nameof(Strings.MenuView),
        [
            new ActionMenuEntry(CanvasActionIds.ZoomIn),
            new ActionMenuEntry(CanvasActionIds.ZoomOut),
            new ActionMenuEntry(CanvasActionIds.ActualSize),
            new SeparatorMenuEntry(),
            new ActionMenuEntry(CanvasActionIds.ShowGrid)
        ]);

        if (isMacOS)
        {
            // macOS convention: About and Quit live in the application menu. Avalonia supplies
            // the Quit item there (with Cmd+Q), so the Quit action isn't listed again.
            applicationMenu = [new ActionMenuEntry(ShellActionIds.About, nameof(Strings.MenuAboutAppFormat))];
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuFile), [new ActionMenuEntry(DocumentActionIds.New)]));
            windowMenus.Add(editMenu);
            windowMenus.Add(documentMenu);
            windowMenus.Add(viewMenu);
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuHelp), [new ActionMenuEntry(ShellActionIds.OpenLogFolder)]));
        }
        else
        {
            applicationMenu = [];
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuFile),
            [
                new ActionMenuEntry(DocumentActionIds.New),
                new SeparatorMenuEntry(),
                new ActionMenuEntry(ShellActionIds.Quit)
            ]));
            windowMenus.Add(editMenu);
            windowMenus.Add(documentMenu);
            windowMenus.Add(viewMenu);
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuHelp),
            [
                new ActionMenuEntry(ShellActionIds.OpenLogFolder),
                new SeparatorMenuEntry(),
                new ActionMenuEntry(ShellActionIds.About),
            ]));
        }

        if (includeDebugMenu)
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuDebug), [new ActionMenuEntry(ShellActionIds.ThrowTestException)]));

        return new MenuLayout(applicationMenu, windowMenus, IsSystemMenuBar: isMacOS);
    }

    /// <summary>Every action id that appears in any menu.</summary>
    public IEnumerable<string> ActionIds()
    {
        return ApplicationMenu
            .Concat(WindowMenus.SelectMany(menu => menu.Entries))
            .OfType<ActionMenuEntry>()
            .Select(entry => entry.ActionId);
    }
}
