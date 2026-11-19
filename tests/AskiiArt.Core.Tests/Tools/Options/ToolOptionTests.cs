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
    public void AChoiceThatCantBeChosenYet_IsShown_ButCantBeTheDefaultOrBeSet()
    {
        ToolOptionChoice[] choices = [new("a", "A"), new("b", "B") { IsAvailable = false }];
        var option = new ChoiceToolOption("pick", "Pick", choices, "a");
        var values = new ToolOptionValues(new ToolOptionGroup("group", [option]));

        Assert.Equal(2, option.Choices.Count);
        Assert.False(option.Allows("b"));
        Assert.Throws<ArgumentException>(() => values.Set(option, "b"));
        Assert.Throws<ArgumentException>(() => new ChoiceToolOption("pick", "Pick", choices, "b"));
    }

    [Fact]
    public void ANumber_IsPlainUnlessToldOtherwise()
    {
        var plain = new NumberToolOption("size", "Size", 1, 10, 1);
        var percent = new NumberToolOption("strength", "Strength", 0, 100, 50) { Unit = ToolOptionUnit.Percent };

        Assert.Equal(ToolOptionUnit.None, plain.Unit);
        Assert.Equal(ToolOptionUnit.Percent, percent.Unit);
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

    [Fact]
    public void AChoiceOption_OffersItsSourcesChoices_AfterItsOwn()
    {
        var (values, option, source) = ValuesWithASource();

        Assert.Equal(["a", "x", "y"], values.ChoicesOf(option).Select(choice => choice.Value));
        Assert.True(values.Allows(option, "y"));
        Assert.False(option.Allows("y"));

        values.Set(option, "y");
        Assert.Equal("y", values.Get(option));

        source.Set([new ToolOptionChoice("z", "")]);
        Assert.Equal(["a", "z"], values.ChoicesOf(option).Select(choice => choice.Value));
        Assert.Throws<ArgumentException>(() => values.Set(option, "x"));
    }

    [Fact]
    public void WhenTheChosenChoiceGoes_TheOptionGoesBackToItsDefault()
    {
        var (values, option, source) = ValuesWithASource();
        values.Set(option, "y");
        var changes = 0;
        var choiceChanges = 0;
        values.Changed += (_, _) => changes++;
        values.ChoicesChanged += (_, _) => choiceChanges++;

        source.Set([new ToolOptionChoice("y", "")]);
        Assert.Equal("y", values.Get(option));
        Assert.Equal((0, 1), (changes, choiceChanges));

        source.Set([]);
        Assert.Equal("a", values.Get(option));
        Assert.Equal((1, 2), (changes, choiceChanges));
    }

    [Fact]
    public void ValuesMadeWithoutTheSource_OfferOnlyTheOptionsOwnChoices()
    {
        var option = new ChoiceToolOption("pick", "Pick", [new("a", "A")], "a") { MoreChoicesFrom = "test.source" };
        var values = new ToolOptionValues(new ToolOptionGroup("group", [option]));

        Assert.Equal(["a"], values.ChoicesOf(option).Select(choice => choice.Value));
    }

    [Fact]
    public void SavedSettings_RestoreEveryGroup_EvenOnesAskedForLater()
    {
        var saved = new ToolSettings();
        saved.For(_group).Set(_size, 7);
        saved.For(_group).Set(_shape, "square");
        var json = saved.ToJson();

        var restored = new ToolSettings();
        restored.Restore(json);

        Assert.Equal(7, restored.For(_group).Get(_size));
        Assert.Equal("square", restored.For(_group).Get(_shape));
    }

    [Fact]
    public void SavedSettings_ApplyToGroupsAlreadyAskedFor()
    {
        var restored = new ToolSettings();
        var values = restored.For(_group);

        restored.Restore("{\"test.group\": {\"size\": 9}}");

        Assert.Equal(9, values.Get(_size));
    }

    [Fact]
    public void SavingAgain_KeepsTheValuesOfGroupsNotAskedForSince()
    {
        var settings = new ToolSettings();
        settings.Restore("{\"test.later\": {\"size\": 2}}");
        settings.For(_group).Set(_size, 4);

        var again = new ToolSettings();
        again.Restore(settings.ToJson());

        Assert.Equal(2, again.For(new ToolOptionGroup("test.later", [_size])).Get(_size));
        Assert.Equal(4, again.For(_group).Get(_size));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"test.group\": 5}")]
    [InlineData("{\"test.group\": {\"size\": \"big\", \"shape\": 3}}")]
    [InlineData("{\"test.group\": {\"shape\": \"hexagon\"}}")]
    public void SavedValuesThatCantBeUsed_AreIgnored(string json)
    {
        var settings = new ToolSettings();
        settings.Restore(json);

        Assert.Equal(3, settings.For(_group).Get(_size));
        Assert.Equal("circle", settings.For(_group).Get(_shape));
    }

    [Fact]
    public void ASavedNumberOutsideTheRange_IsMovedIntoIt()
    {
        var settings = new ToolSettings();
        settings.Restore("{\"test.group\": {\"size\": 99}}");

        Assert.Equal(10, settings.For(_group).Get(_size));
    }

    private static (ToolOptionValues Values, ChoiceToolOption Option, FakeChoiceSource Source) ValuesWithASource()
    {
        var source = new FakeChoiceSource("test.source");
        source.Set([new ToolOptionChoice("x", "") { Name = "X" }, new ToolOptionChoice("y", "") { Name = "Y" }]);
        var option = new ChoiceToolOption("pick", "Pick", [new("a", "A")], "a") { MoreChoicesFrom = source.Id };
        var values = new ToolOptionValues(new ToolOptionGroup("group", [option]),
            new Dictionary<string, IToolOptionChoiceSource> { [source.Id] = source });
        return (values, option, source);
    }

    private sealed class FakeChoiceSource(string id) : IToolOptionChoiceSource
    {
        public event EventHandler? Changed;

        public string Id => id;

        public IReadOnlyList<ToolOptionChoice> Choices { get; private set; } = [];

        public void Set(IReadOnlyList<ToolOptionChoice> choices)
        {
            Choices = choices;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
