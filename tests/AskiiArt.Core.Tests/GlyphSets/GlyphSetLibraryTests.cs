// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Tests.GlyphSets;

public sealed class GlyphSetLibraryTests
{
    private static readonly GlyphSet _arrows = GlyphSet.FromText("←↑→↓");
    private static readonly GlyphSet _shades = GlyphSet.FromText("░▒▓");

    private readonly GlyphSetLibrary _library = new(["Recent", "Basic Latin"], []);
    private int _changes;

    public GlyphSetLibraryTests()
    {
        _library.Changed += (_, _) => _changes++;
    }

    [Fact]
    public void Find_GivesBuiltInSetsAndUserSets_ByTheirIds()
    {
        var added = _library.Add("Mine", _arrows);

        Assert.Equal(BuiltInGlyphSets.BasicLatin.Glyphs, _library.Find(BuiltInGlyphSets.BasicLatin.Id));
        Assert.Equal(_arrows, _library.Find(added.Id));
        Assert.Equal(added, _library.FindUserSet(added.Id));
        Assert.Null(_library.Find("user.gone"));
        Assert.Null(_library.FindUserSet(BuiltInGlyphSets.BasicLatin.Id));
    }

    [Fact]
    public void Add_PutsTheSetLast_WithANewUserId()
    {
        var first = _library.Add("First", _arrows);
        var second = _library.Add("Second", _shades);

        Assert.Equal([first, second], _library.UserSets);
        Assert.StartsWith(GlyphSetLibrary.UserSetIdPrefix, first.Id, StringComparison.Ordinal);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _changes);
    }

    [Theory]
    [InlineData("New set", "New set 2")]
    [InlineData("recent", "recent 2")]
    [InlineData(" Basic latin ", "Basic latin 2")]
    [InlineData("Other", "Other")]
    public void Add_NumbersATakenOrReservedName(string name, string expected)
    {
        _library.Add("New set", _arrows);

        Assert.Equal(expected, _library.Add(name, _shades).Name);
    }

    [Theory]
    [InlineData("", LibraryNameProblem.Blank)]
    [InlineData("RECENT", LibraryNameProblem.Taken)]
    [InlineData("Other", LibraryNameProblem.Taken)]
    [InlineData(" Mine ", LibraryNameProblem.None)]
    public void CheckName_SaysWhyANameCantBeTheSets(string name, LibraryNameProblem expected)
    {
        var mine = _library.Add("Mine", _arrows);
        _library.Add("Other", _shades);

        Assert.Equal(expected, _library.CheckName(name, mine.Id));
    }

    [Fact]
    public void Rename_Change_AndDelete_ChangeTheSet_AndSaySo()
    {
        var set = _library.Add("Mine", _arrows);

        _library.Rename(set.Id, "  Arrows ");
        _library.Change(set.Id, _shades);
        Assert.Equal(new UserGlyphSet(set.Id, "Arrows", _shades), _library.FindUserSet(set.Id));

        _library.Delete(set.Id);
        Assert.Empty(_library.UserSets);
        Assert.Equal(4, _changes);
    }

    [Fact]
    public void ChangingASetToWhatItIs_IsNoChange()
    {
        var set = _library.Add("Mine", _arrows);

        _library.Rename(set.Id, "Mine");
        _library.Change(set.Id, GlyphSet.FromText("←↑→↓"));

        Assert.Equal(1, _changes);
    }

    [Fact]
    public void BuiltInSets_CantBeChanged()
    {
        var id = BuiltInGlyphSets.BasicLatin.Id;

        Assert.Throws<ArgumentException>(() => _library.Rename(id, "Mine"));
        Assert.Throws<ArgumentException>(() => _library.Change(id, _arrows));
        Assert.Throws<ArgumentException>(() => _library.Delete(id));
    }

    [Fact]
    public void ATakenOrBlankName_CantBeGiven()
    {
        var mine = _library.Add("Mine", _arrows);
        _library.Add("Other", _shades);

        Assert.Throws<ArgumentException>(() => _library.Rename(mine.Id, "other"));
        Assert.Throws<ArgumentException>(() => _library.Rename(mine.Id, "Recent"));
        Assert.Throws<ArgumentException>(() => _library.Rename(mine.Id, " "));
    }

    [Fact]
    public void SavedSets_KeepTheirIds_AndNewOnesDontReuseThem()
    {
        var library = new GlyphSetLibrary([], [new UserGlyphSet("user.4", "Saved", _arrows)]);

        var added = library.Add("New", _shades);

        Assert.Equal("Saved", library.FindUserSet("user.4")?.Name);
        Assert.NotEqual("user.4", added.Id);
    }
}
