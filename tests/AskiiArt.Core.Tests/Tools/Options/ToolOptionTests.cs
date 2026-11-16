// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools.Options;

public sealed class ToolOptionTests
{
    private static readonly NumberToolOption _size = new("size", "SizeName", 1, 10, 3);

    private static readonly ChoiceToolOption _shape = new("shape", "ShapeName",
        [new("square", "SquareName"), new("circle", "CircleName")], "circle");

    private static readonly ToolOptionGroup _group = new("test.group", [_size, _shape]);

    [Theory]
    [InlineData(5, 4, 4)]
    [InlineData(1, 10, 0)]
    [InlineData(1, 10, 11)]
    public void ANumberOption_NeedsARange_ThatHoldsItsDefault(int minimum, int maximum, int defaultValue)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new NumberToolOption("n", "Name", minimum, maximum, defaultValue));
    }

    [Fact]
    public void AChoiceOption_NeedsDistinctChoices_ThatHoldItsDefault()
    {
        Assert.Throws<ArgumentException>(() => new ChoiceToolOption("c", "Name", [], "a"));
        Assert.Throws<ArgumentException>(() => new ChoiceToolOption("c", "Name", [new("a", "A"), new("a", "B")], "a"));
        Assert.Throws<ArgumentException>(() => new ChoiceToolOption("c", "Name", [new("a", "A")], "b"));
    }

    [Fact]
    public void Values_StartAtTheDefaults()
    {
        var values = new ToolOptionValues(_group);

        Assert.Equal(3, values.Get(_size));
        Assert.Equal("circle", values.Get(_shape));
        Assert.Same(_group, values.Group);
    }

    [Fact]
    public void ANumber_IsKeptInsideItsRange()
    {
        var values = new ToolOptionValues(_group);

        values.Set(_size, 50);
        Assert.Equal(10, values.Get(_size));

        values.Set(_size, -50);
        Assert.Equal(1, values.Get(_size));
    }

    [Fact]
    public void AChoiceThatIsntOffered_IsRejected()
    {
        var values = new ToolOptionValues(_group);

        Assert.Throws<ArgumentException>(() => values.Set(_shape, "triangle"));
        Assert.Equal("circle", values.Get(_shape));
    }

    [Fact]
    public void Changed_IsRaised_OnlyWhenAValueChanges()
    {
        var values = new ToolOptionValues(_group);
        var changes = 0;
        values.Changed += (_, _) => changes++;

        values.Set(_size, 3);
        values.Set(_shape, "circle");
        values.Set(_size, 11);
        values.Set(_size, 10);
        values.Set(_shape, "square");

        Assert.Equal(2, changes);
    }

    [Fact]
    public void AnOptionFromAnotherGroup_IsRejected_EvenWithTheSameId()
    {
        var values = new ToolOptionValues(_group);
        var lookalike = new NumberToolOption("size", "SizeName", 1, 10, 3);

        Assert.Throws<ArgumentException>(() => values.Get(lookalike));
        Assert.Throws<ArgumentException>(() => values.Set(lookalike, 2));
    }

    [Fact]
    public void AGroupWithTwoOptionsOfOneId_IsRejected()
    {
        var group = new ToolOptionGroup("test.twice", [_size, new NumberToolOption("size", "Other", 0, 1, 0)]);

        Assert.Throws<ArgumentException>(() => new ToolOptionValues(group));
    }

    [Fact]
    public void Settings_GiveEachGroupOneSetOfValues()
    {
        var settings = new ToolSettings();
        var other = new ToolOptionGroup("test.other", [_size]);

        Assert.Same(settings.For(_group), settings.For(_group));
        Assert.NotSame(settings.For(_group), settings.For(other));
    }

    [Fact]
    public void Settings_RejectTwoGroupsWithOneId()
    {
        var settings = new ToolSettings();
        settings.For(_group);

        Assert.Throws<ArgumentException>(() => settings.For(new ToolOptionGroup("test.group", [_size])));
    }
}
