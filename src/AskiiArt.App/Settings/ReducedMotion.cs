// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Settings;

/// <summary>
/// One app-wide switch that stills the app's motion (ADR 0072), kept in the settings file as <c>"reduceMotion": "true"</c>.
/// It's off unless set there, and has no UI until there's a Preferences window. Everything that moves reads its speed or
/// duration from a theme token, so the switch sets those tokens to zero at start-up: the selection's dashes and hatching
/// stand still, the docking animations and the view's glide jump.
/// </summary>
internal static class ReducedMotion
{
    /// <summary>The key in the settings file.</summary>
    public const string SettingKey = "reduceMotion";

    // The theme tokens that set how fast or how long things move.
    private static readonly string[] _motionTokens =
    [
        "AppCanvasSelectionAntsSpeed",
        "AppLandingAntsSpeed",
        "AppDockingMotionMilliseconds",
        "AppCanvasViewGlideMilliseconds",
        "AppCanvasZoomGlideMilliseconds"
    ];

    /// <summary>True when the settings ask for reduced motion.</summary>
    public static bool IsOn(IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return string.Equals(settings.GetText(SettingKey), "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets every motion token in <paramref name="resources"/> to zero. Its own entries come before the themes it
    /// merges, so this overrides them.
    /// </summary>
    public static void Apply(IResourceDictionary resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        foreach (var token in _motionTokens)
            resources[token] = 0d;
    }
}
