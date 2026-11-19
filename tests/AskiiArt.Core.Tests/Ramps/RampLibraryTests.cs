// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Libraries;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class RampLibraryTests
{
    private static readonly Ramp _dots = Ramp.FromText(".:");
    private static readonly Ramp _hashes = Ramp.FromText("+#");

    private readonly RampLibrary _library = new(["Standard", "Fine"], []);
    private int _changes;

    public RampLibraryTests()
    {
        _library.Changed += (_, _) => _changes++;
    }

    [Fact]
    public void Find_GivesBuiltInRampsAndUserRamps_ByTheirIds()
    {
        var added = _library.Add("Mine", _dots);

        Assert.Equal(BuiltInRamps.Fine.Ramp, _library.Find(BuiltInRamps.Fine.Id));
        Assert.Equal(_dots, _library.Find(added.Id));
        Assert.Null(_library.Find("user.gone"));
        Assert.True(RampLibrary.IsBuiltIn(BuiltInRamps.Fine.Id));
        Assert.False(RampLibrary.IsBuiltIn(added.Id));
    }

    [Fact]
    public void Add_PutsTheRampLast_WithANewUserId()
    {
        var first = _library.Add("First", _dots);
        var second = _library.Add("Second", _hashes);

        Assert.Equal([first, second], _library.UserRamps);
        Assert.StartsWith(RampLibrary.UserRampIdPrefix, first.Id, StringComparison.Ordinal);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, _changes);
    }

    [Theory]
    [InlineData("Mine", "Mine 2")]
    [InlineData("  mine ", "mine 2")]
    [InlineData("fine", "fine 2")]
    [InlineData("Other", "Other")]
    public void Add_NumbersATakenName_IgnoringCaseAndSpaces(string name, string expected)
    {
        _library.Add("Mine", _dots);

        Assert.Equal(expected, _library.Add(name, _hashes).Name);
    }

    [Fact]
    public void Add_CountsUpUntilTheNameIsFree()
    {
        _library.Add("Mine", _dots);
        _library.Add("Mine", _dots);

        Assert.Equal("Mine 3", _library.Add("Mine", _dots).Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_RejectsABlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => _library.Add(name, _dots));
    }

    [Theory]
    [InlineData("", LibraryNameProblem.Blank)]
    [InlineData("STANDARD", LibraryNameProblem.Taken)]
    [InlineData("Other", LibraryNameProblem.Taken)]
    [InlineData("Mine", LibraryNameProblem.None)]
    [InlineData(" Renamed ", LibraryNameProblem.None)]
    public void CheckName_SaysWhyANameCantBeTheRamps(string name, LibraryNameProblem expected)
    {
        var mine = _library.Add("Mine", _dots);
        _library.Add("Other", _hashes);

        Assert.Equal(expected, _library.CheckName(name, mine.Id));
    }

    [Fact]
    public void Rename_TrimsTheName_AndRejectsOneThatsTaken()
    {
        var mine = _library.Add("Mine", _dots);
        _library.Add("Other", _hashes);
        _changes = 0;

        _library.Rename(mine.Id, "  Renamed  ");

        Assert.Equal("Renamed", _library.UserRamps[0].Name);
        Assert.Equal(1, _changes);
        Assert.Throws<ArgumentException>(() => _library.Rename(mine.Id, "other"));
        Assert.Throws<ArgumentException>(() => _library.Rename(mine.Id, " "));
        Assert.Equal("Renamed", _library.UserRamps[0].Name);
    }

    [Fact]
    public void Change_ReplacesTheRamp_AndSaysSoOnlyWhenItDiffers()
    {
        var mine = _library.Add("Mine", _dots);
        _changes = 0;

        _library.Change(mine.Id, _dots);
        Assert.Equal(0, _changes);

        _library.Change(mine.Id, _hashes);
        Assert.Equal(_hashes, _library.Find(mine.Id));
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void Delete_RemovesTheRamp()
    {
        var mine = _library.Add("Mine", _dots);
        _changes = 0;

        _library.Delete(mine.Id);

        Assert.Empty(_library.UserRamps);
        Assert.Null(_library.Find(mine.Id));
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void BuiltInRamps_CantBeRenamedChangedOrDeleted()
    {
        Assert.Throws<ArgumentException>(() => _library.Rename(BuiltInRamps.Standard.Id, "Mine"));
        Assert.Throws<ArgumentException>(() => _library.Change(BuiltInRamps.Standard.Id, _dots));
        Assert.Throws<ArgumentException>(() => _library.Delete(BuiltInRamps.Standard.Id));
    }

    [Fact]
    public void ALibraryMadeFromSavedRamps_KeepsTheirIds_AndFixesClashes()
    {
        UserRamp[] saved =
        [
            new("user.a", "Mine", _dots),
            new("user.a", "mine", _hashes),
            new("askiiart.ramp.fine", "Standard", _dots),
        ];

        var library = new RampLibrary(["Standard"], saved);

        Assert.Equal("user.a", library.UserRamps[0].Id);
        Assert.Equal("Mine", library.UserRamps[0].Name);
        Assert.NotEqual("user.a", library.UserRamps[1].Id);
        Assert.Equal("mine 2", library.UserRamps[1].Name);
        Assert.StartsWith(RampLibrary.UserRampIdPrefix, library.UserRamps[2].Id, StringComparison.Ordinal);
        Assert.Equal("Standard 2", library.UserRamps[2].Name);
    }
}
