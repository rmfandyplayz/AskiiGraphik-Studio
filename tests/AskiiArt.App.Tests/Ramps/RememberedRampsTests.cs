// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Ramps;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Libraries;
using AskiiArt.Core.Ramps;

namespace AskiiArt.App.Tests.Ramps;

/// <summary>The user's ramps are kept in settings between runs (ADR 0052).</summary>
public sealed class RememberedRampsTests
{
    private readonly FakeAppSettings _settings = new();

    [Fact]
    public void TheRamps_AreSavedWhenTheyChange_AndComeBackNextTime()
    {
        var library = RememberedRamps.CreateLibrary(_settings);
        var mine = library.Add("Mine", Ramp.FromText("ab", RampZeroLevel.Space));
        var gone = library.Add("Gone", Ramp.FromText("cd"));
        library.Delete(gone.Id);

        var nextTime = RememberedRamps.CreateLibrary(_settings);

        Assert.Equal(mine, Assert.Single(nextTime.UserRamps));
    }

    [Fact]
    public void TheBuiltInNames_AreTaken()
    {
        var library = RememberedRamps.CreateLibrary(_settings);

        Assert.Equal(LibraryNameProblem.Taken, library.CheckName("Standard"));
        Assert.Equal("Standard 2", library.Add("Standard", Ramp.FromText("ab")).Name);
    }

    [Fact]
    public void DamagedSavedRamps_GiveNone()
    {
        _settings.SetText(RememberedRamps.SettingKey, "{ not json");

        Assert.Empty(RememberedRamps.CreateLibrary(_settings).UserRamps);
    }
}
