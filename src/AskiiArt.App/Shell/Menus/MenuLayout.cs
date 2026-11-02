// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Docking;

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
    /// <summary>
    /// The macOS Dock icon's menu. Its items work with no window open, which the menu bar can't
    /// offer: on macOS the File menu belongs to the window (ADR 0023). Empty elsewhere.
    /// </summary>
    public IReadOnlyList<MenuEntry> DockMenu { get; init; } = [];

    public static MenuLayout CreateDefault(bool isMacOS, bool includeDebugMenu)
    {
        var windowMenus = new List<MenuDefinition>();
        IReadOnlyList<MenuEntry> applicationMenu;

        var editMenu = new MenuDefinition(nameof(Strings.MenuEdit),
        [
            new ActionMenuEntry(EditActionIds.Undo),
            new ActionMenuEntry(EditActionIds.Redo),
            new SeparatorMenuEntry(),
            new ActionMenuEntry(EditActionIds.CopyAsText)
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

        var panelMenu = new MenuDefinition(nameof(Strings.MenuWindow),
        [
            new ActionMenuEntry(ShellDockActionIds.ToggleLayers),
            new ActionMenuEntry(ShellDockActionIds.ToggleHistory),
            new SeparatorMenuEntry(),
            new ActionMenuEntry(ShellDockActionIds.ResetLayout)
        ]);

        if (isMacOS)
        {
            // macOS convention: About and Quit live in the application menu. Avalonia supplies
            // the Quit item there (with Cmd+Q), so the Quit action isn't listed again.
            applicationMenu = [new ActionMenuEntry(ShellActionIds.About, nameof(Strings.MenuAboutAppFormat))];
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuFile), [.. FileEntries(isMacOS)]));
            windowMenus.Add(editMenu);
            windowMenus.Add(documentMenu);
            windowMenus.Add(viewMenu);
            windowMenus.Add(panelMenu);
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuHelp),
                [new ActionMenuEntry(ShellActionIds.OpenLogFolder)]));
        }
        else
        {
            applicationMenu = [];
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuFile),
            [
                .. FileEntries(isMacOS),
                new SeparatorMenuEntry(),
                new ActionMenuEntry(ShellActionIds.Quit)
            ]));
            windowMenus.Add(editMenu);
            windowMenus.Add(documentMenu);
            windowMenus.Add(viewMenu);
            windowMenus.Add(panelMenu);
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuHelp),
            [
                new ActionMenuEntry(ShellActionIds.OpenLogFolder),
                new SeparatorMenuEntry(),
                new ActionMenuEntry(ShellActionIds.About),
            ]));
        }

        if (includeDebugMenu)
        {
            windowMenus.Add(new MenuDefinition(nameof(Strings.MenuDebug),
            [
                new ActionMenuEntry(DocumentActionIds.OpenSampleDocument),
                new ActionMenuEntry(ShellActionIds.ThrowTestException)
            ]));
        }

        IReadOnlyList<MenuEntry> dockMenu = [];
        if (isMacOS)
            dockMenu = [new ActionMenuEntry(DocumentActionIds.New), new ActionMenuEntry(DocumentActionIds.Open)];

        return new MenuLayout(applicationMenu, windowMenus, IsSystemMenuBar: isMacOS) { DockMenu = dockMenu };
    }

    // The File menu's own items; on Windows and Linux, Quit follows them. On macOS, Close sits where
    // Mac apps put it.
    private static IEnumerable<MenuEntry> FileEntries(bool isMacOS)
    {
        yield return new ActionMenuEntry(DocumentActionIds.New);
        yield return new ActionMenuEntry(DocumentActionIds.Open);
        yield return new SeparatorMenuEntry();
        if (isMacOS)
            yield return new ActionMenuEntry(ShellActionIds.CloseWindow);

        yield return new ActionMenuEntry(DocumentActionIds.Save);
        yield return new ActionMenuEntry(DocumentActionIds.SaveAs);
        yield return new SeparatorMenuEntry();
        yield return new ActionMenuEntry(DocumentActionIds.ExportAsText);
    }

    /// <summary>Every action id that appears in any menu, the Dock menu included.</summary>
    public IEnumerable<string> ActionIds()
    {
        return ApplicationMenu
            .Concat(WindowMenus.SelectMany(menu => menu.Entries))
            .Concat(DockMenu)
            .OfType<ActionMenuEntry>()
            .Select(entry => entry.ActionId);
    }
}
