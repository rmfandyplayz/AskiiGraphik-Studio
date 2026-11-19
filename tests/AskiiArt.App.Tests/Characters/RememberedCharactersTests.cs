// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Characters;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Libraries;

namespace AskiiArt.App.Tests.Characters;

/// <summary>The user's character sets and the recent characters are kept in settings between runs (ADR 0053).</summary>
public sealed class RememberedCharactersTests
{
    private readonly FakeAppSettings _settings = new();

    [Fact]
    public void TheSets_AreSavedWhenTheyChange_AndComeBackNextTime()
    {
        var library = RememberedCharacters.CreateLibrary(_settings);
        var mine = library.Add("Mine", GlyphSet.FromText("ab\U0001F642"));
        var gone = library.Add("Gone", GlyphSet.FromText("cd"));
        library.Delete(gone.Id);

        var nextTime = RememberedCharacters.CreateLibrary(_settings);

        Assert.Equal(mine, Assert.Single(nextTime.UserSets));
    }

    [Theory]
    [InlineData("Recent")]
    [InlineData("Basic Latin")]
    [InlineData("Box Drawing")]
    public void RecentsAndTheBuiltInSetsNames_AreTaken(string name)
    {
        var library = RememberedCharacters.CreateLibrary(_settings);

        Assert.Equal(LibraryNameProblem.Taken, library.CheckName(name));
        Assert.Equal(name + " 2", library.Add(name, GlyphSet.FromText("ab")).Name);
    }

    [Fact]
    public void DamagedSavedSets_GiveNone()
    {
        _settings.SetText(RememberedCharacters.SetsSettingKey, "{ not json");

        Assert.Empty(RememberedCharacters.CreateLibrary(_settings).UserSets);
    }

    [Fact]
    public void TheRecentCharacters_AreSavedWhenTheyChange_AndComeBackNextTime()
    {
        var recent = RememberedCharacters.CreateRecent(_settings);
        recent.Use(new Rune('a'));
        recent.Use(new Rune(0x1F642));

        var nextTime = RememberedCharacters.CreateRecent(_settings);

        Assert.Equal([new Rune(0x1F642), new Rune('a')], nextTime.Glyphs);
    }

    [Fact]
    public void SavedRecentCharactersThatCantBePainted_AreLeftOut()
    {
        _settings.SetText(RememberedCharacters.RecentSettingKey, "a \u0007b");

        Assert.Equal("ab", RememberedCharacters.CreateRecent(_settings).ToString());
    }
}
