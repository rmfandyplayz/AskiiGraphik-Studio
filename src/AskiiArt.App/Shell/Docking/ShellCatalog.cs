// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A window the app has. The view doesn't know where it's hosted (ARCHITECTURE §14.4).</summary>
/// <param name="Id">Its id in the layout.</param>
/// <param name="Title">The name at its top.</param>
/// <param name="MinimumHeight">The smallest height that shows its contents.</param>
/// <param name="CreateView">Makes its contents.</param>
/// <param name="CreateMenu">Makes the menu of its title and its menu button, if it has one.</param>
/// <param name="CloseCommand">What its close button does, given its id.</param>
internal sealed record ShellWindowEntry(
    string Id,
    string Title,
    double MinimumHeight,
    Func<Control> CreateView,
    Func<ContextMenu>? CreateMenu = null,
    ICommand? CloseCommand = null);

/// <summary>A toolbar the app has.</summary>
/// <param name="Id">Its id in the layout.</param>
/// <param name="CreateView">Makes its buttons.</param>
/// <param name="CreateMenu">Makes its right-click menu, if it has one.</param>
internal sealed record ShellToolbarEntry(string Id, Func<Control> CreateView, Func<ContextMenu>? CreateMenu = null);

/// <summary>
/// The windows and toolbars that exist, registered by id, so the layout can stay plain data. The toolbar
/// customizer (M8) and plugins (M8b) add to this.
/// </summary>
internal sealed class ShellCatalog(IReadOnlyList<ShellWindowEntry> windows, IReadOnlyList<ShellToolbarEntry> toolbars)
{
    public IReadOnlyDictionary<string, ShellWindowEntry> Windows { get; } =
        windows.ToDictionary(window => window.Id, StringComparer.Ordinal);

    public IReadOnlyDictionary<string, ShellToolbarEntry> Toolbars { get; } =
        toolbars.ToDictionary(toolbar => toolbar.Id, StringComparer.Ordinal);

    public IReadOnlyDictionary<string, double> MinimumWindowHeights { get; } =
        windows.ToDictionary(window => window.Id, window => window.MinimumHeight, StringComparer.Ordinal);
}
