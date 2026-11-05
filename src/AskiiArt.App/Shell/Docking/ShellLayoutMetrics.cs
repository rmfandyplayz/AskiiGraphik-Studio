// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;
using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

/// <summary>The shell's sizes, from the theme tokens, in logical pixels.</summary>
internal sealed record ShellLayoutMetrics(
    DockingMetrics Docking,
    double WidgetWidth,
    double WindowMinHeight,
    double MainWindowMinWidth,
    double MainWindowMinHeight,
    double ToolbarWheelStep,
    double ToolbarFadeLength,
    double DragThreshold,
    TimeSpan MotionDuration,
    double LandingOutlineThickness,
    double LandingOutlineDash,
    double LandingOutlineDashGap,
    double LandingAntsSpeed,
    double DropZoneDimmedOpacity)
{
    public static ShellLayoutMetrics
        FromResources(IResourceNode resources, Avalonia.Styling.ThemeVariant? theme = null) => new(
        new DockingMetrics(
            Margin: Token(resources, "AppDockingMargin", theme),
            WidgetGap: Token(resources, "AppWidgetGap", theme),
            TitleLength: Token(resources, "AppWindowTitleHeight", theme),
            DefaultMinimumWindowLength: Token(resources, "AppWindowMinHeight", theme),
            MinimumCanvasRegionWidth: Token(resources, "AppCanvasRegionMinWidth", theme),
            ToolbarGap: Token(resources, "AppToolbarGap", theme),
            MinimumToolbarLength: Token(resources, "AppToolbarMinLength", theme),
            MiddleToolbarPriority: Token(resources, "AppToolbarMiddlePriority", theme),
            ToolbarZoneMinimumShare: Token(resources, "AppToolbarZoneMinimumShare", theme),
            SubToolbarLengthFactor: Token(resources, "AppSubToolbarLengthFactor", theme)),
        Token(resources, "AppWidgetWidth", theme),
        Token(resources, "AppWindowMinHeight", theme),
        Token(resources, "AppMainWindowMinWidth", theme),
        Token(resources, "AppMainWindowMinHeight", theme),
        Token(resources, "AppToolbarWheelStep", theme),
        Token(resources, "AppToolbarFadeLength", theme),
        Token(resources, "AppDockingDragThreshold", theme),
        TimeSpan.FromMilliseconds(Token(resources, "AppDockingMotionMilliseconds", theme)),
        Token(resources, "AppLandingOutlineThickness", theme),
        Token(resources, "AppLandingOutlineDash", theme),
        Token(resources, "AppLandingOutlineDashGap", theme),
        Token(resources, "AppLandingAntsSpeed", theme),
        Token(resources, "AppDropZoneDimmedOpacity", theme));

    private static double Token(IResourceNode resources, string key, Avalonia.Styling.ThemeVariant? theme)
    {
        if (resources.TryGetResource(key, theme, out var value) && value is double size)
            return size;

        throw new InvalidOperationException($"The theme token {key} is missing.");
    }
}
