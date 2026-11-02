// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>The shell's sizes, from the theme tokens, in logical pixels.</summary>
internal sealed record ShellLayoutMetrics(
    double SideWidth,
    double ToolbarHeight,
    double WindowMinWidth,
    double WindowMinHeight,
    double ToolbarWheelStep)
{
    public static ShellLayoutMetrics
        FromResources(IResourceNode resources, Avalonia.Styling.ThemeVariant? theme = null) => new(
        Token(resources, "AppDockSideWidth", theme),
        Token(resources, "AppDockToolbarHeight", theme),
        Token(resources, "AppWindowMinWidth", theme),
        Token(resources, "AppWindowMinHeight", theme),
        Token(resources, "AppToolbarWheelStep", theme));

    private static double Token(IResourceNode resources, string key, Avalonia.Styling.ThemeVariant? theme)
    {
        if (resources.TryGetResource(key, theme, out var value) && value is double size)
            return size;

        throw new InvalidOperationException($"The theme token {key} is missing.");
    }
}
