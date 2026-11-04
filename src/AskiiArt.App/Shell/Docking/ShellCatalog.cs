// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>A window the app has. The view doesn't know where it's hosted (ARCHITECTURE §14.4).</summary>
internal sealed record ShellWindowEntry(string Id, string Title, double MinimumHeight, Func<Control> CreateView);

/// <summary>A toolbar the app has.</summary>
internal sealed record ShellToolbarEntry(string Id, Func<Control> CreateView);

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
