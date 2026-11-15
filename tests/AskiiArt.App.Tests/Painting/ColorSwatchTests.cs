// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Painting;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace AskiiArt.App.Tests.Painting;

public sealed class ColorSwatchTests
{
    [AvaloniaFact]
    public void WithNoColor_ItShowsTheNoneSlash()
    {
        var swatch = new ColorSwatch();

        Assert.True(swatch.IsShowingNone);
        Assert.Null(swatch.Background);
    }

    [AvaloniaFact]
    public void AColor_FillsIt_AndHidesTheSlash_UntilItsNoneAgain()
    {
        var swatch = new ColorSwatch { Fill = Brushes.Teal };

        Assert.False(swatch.IsShowingNone);
        Assert.Same(Brushes.Teal, swatch.Background);

        swatch.Fill = null;

        Assert.True(swatch.IsShowingNone);
    }
}
