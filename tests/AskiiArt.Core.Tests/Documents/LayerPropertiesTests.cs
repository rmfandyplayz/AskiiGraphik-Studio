// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class LayerPropertiesTests
{
    [Fact]
    public void Constructor_MakesAVisibleUnlockedOpaqueNormalLayer()
    {
        var properties = new LayerProperties("Sky");

        Assert.Equal("Sky", properties.Name);
        Assert.True(properties.IsVisible);
        Assert.False(properties.IsLocked);
        Assert.Equal(1.0, properties.Opacity);
        Assert.Equal(LayerBlendMode.Normal, properties.BlendMode);
    }

    [Fact]
    public void With_CopiesAndCompareByValue()
    {
        var original = new LayerProperties("Sky");

        var hidden = original with { IsVisible = false };

        Assert.True(original.IsVisible);
        Assert.False(hidden.IsVisible);
        Assert.NotEqual(original, hidden);
        Assert.Equal(original, hidden with { IsVisible = true });
    }

    [Fact]
    public void Name_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new LayerProperties(null!));
        Assert.Throws<ArgumentNullException>(() => new LayerProperties("Sky") with { Name = null! });
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Opacity_RejectsValuesOutsideZeroToOne(double opacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayerProperties("Sky") with { Opacity = opacity });
    }

    [Fact]
    public void BlendMode_RejectsUndefinedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LayerProperties("Sky") with { BlendMode = (LayerBlendMode)99 });
    }
}
