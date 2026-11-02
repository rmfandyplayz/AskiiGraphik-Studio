// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>The sizes of the docks, from the theme tokens, in logical pixels.</summary>
internal sealed record ShellLayoutMetrics(double SideWidth, double MenuBarHeight, double ToolbarHeight, double StatusBarHeight)
{
    public static ShellLayoutMetrics FromResources(IResourceNode resources, Avalonia.Styling.ThemeVariant? theme = null) => new(
        Token(resources, "AppDockSideWidth", theme),
        Token(resources, "AppDockMenuBarHeight", theme),
        Token(resources, "AppDockToolbarHeight", theme),
        Token(resources, "AppDockStatusBarHeight", theme));

    private static double Token(IResourceNode resources, string key, Avalonia.Styling.ThemeVariant? theme)
    {
        if (resources.TryGetResource(key, theme, out var value) && value is double size)
            return size;

        throw new InvalidOperationException($"The theme token {key} is missing.");
    }
}
