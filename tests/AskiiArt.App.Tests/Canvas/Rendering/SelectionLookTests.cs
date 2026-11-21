// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class SelectionLookTests
{
    [AvaloniaFact]
    public void TheThemeTokens_GiveALookThatHatchesAndHasMarkers_InBothThemes()
    {
        foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            var look = SelectionLook.FromResources(Application.Current!, theme);

            Assert.True(look.HatchPeriod >= 2);
            Assert.True(look.HatchThickness > 0);
            Assert.True(look.FinishMarkerRadius > look.CornerMarkerRadius);
            Assert.InRange(look.FaintOpacity, 0.01, 0.99);
            Assert.NotEqual(look.SubtractLine, look.IntersectLine);
        }
    }

    [Fact]
    public void Scaling_ChangesTheLengths_ButNotHowStronglyThingsShow()
    {
        var look = new SelectionLook(SKColors.Black, SKColors.White, SKColors.Red, SKColors.DarkRed, SKColors.Blue,
            SKColors.DarkBlue, HatchPeriod: 8, HatchThickness: 1, FaintOpacity: 0.3, LostHatchOpacity: 0.7,
            ClosingLineOpacity: 0.5, CornerMarkerRadius: 3, FinishMarkerRadius: 8, CornerMarkerOpacity: 0.9);

        var scaled = look.ScaledBy(2);

        var expected = look with
        {
            HatchPeriod = 16,
            HatchThickness = 2,
            CornerMarkerRadius = 6,
            FinishMarkerRadius = 16
        };
        Assert.Equal(expected, scaled);
    }

    [Fact]
    public void Fading_ScalesTheAlpha_AndKeepsTheColour()
    {
        Assert.Equal(new SKColor(10, 20, 30, 100), SelectionLook.Faded(new SKColor(10, 20, 30, 200), 0.5));
        Assert.Equal(new SKColor(10, 20, 30, 0), SelectionLook.Faded(new SKColor(10, 20, 30, 200), -1));
    }
}
