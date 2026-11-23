// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Settings;
using AskiiArt.App.Tests.Fakes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.Settings;

/// <summary>The one switch that stills the app's motion (ADR 0072).</summary>
public sealed class ReducedMotionTests
{
    private static readonly string[] _motionTokens =
    [
        "AppCanvasSelectionAntsSpeed",
        "AppLandingAntsSpeed",
        "AppDockingMotionMilliseconds",
        "AppCanvasViewGlideMilliseconds"
    ];

    [Fact]
    public void MotionIsOnByDefault_AndOffOnlyWhenTheSettingsSaySo()
    {
        var settings = new FakeAppSettings();
        Assert.False(ReducedMotion.IsOn(settings));

        settings.SetText(ReducedMotion.SettingKey, "false");
        Assert.False(ReducedMotion.IsOn(settings));

        settings.SetText(ReducedMotion.SettingKey, "true");
        Assert.True(ReducedMotion.IsOn(settings));
    }

    [AvaloniaFact]
    public void ReducingMotion_ZeroesEveryMotionToken_OverTheTheme()
    {
        // The theme's own tokens, merged in as the app merges them.
        var resources = new ResourceDictionary();
        resources.MergedDictionaries.Add(Application.Current!.Resources);
        foreach (var token in _motionTokens)
        {
            Assert.True(resources.TryGetResource(token, null, out var themed));
            Assert.NotEqual(0d, themed);
        }

        ReducedMotion.Apply(resources);

        foreach (var token in _motionTokens)
        {
            Assert.True(resources.TryGetResource(token, null, out var stilled));
            Assert.Equal(0d, stilled);
        }
    }
}
