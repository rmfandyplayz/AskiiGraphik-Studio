// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class RampGeneratorTests
{
    // Levels of a 4-glyph ramp would ideally measure 0.2, 0.4, 0.6 and 0.8.
    private static readonly FakeGlyphDensities _densities = new(new Dictionary<char, double>
    {
        ['a'] = 0.05,
        ['b'] = 0.19,
        ['c'] = 0.25,
        ['d'] = 0.42,
        ['e'] = 0.45,
        ['f'] = 0.58,
        ['g'] = 0.8,
    });

    [Fact]
    public void ItPicks_TheCharactersNearestEvenSteps_LightestFirst_EndingWithTheDarkest()
    {
        var ramp = RampGenerator.Generate("gfedcba", 4, _densities);

        Assert.Equal("bdfg", ramp?.ToString());
    }

    [Fact]
    public void FewerCandidatesThanAsked_AreAllPicked_InOrderOfDarkness()
    {
        var ramp = RampGenerator.Generate("gca", 10, _densities);

        Assert.Equal("acg", ramp?.ToString());
    }

    [Fact]
    public void SpacesRepeatsAndCharactersWithNoInk_AreLeftOut()
    {
        var ramp = RampGenerator.Generate("a a\tz g g", 10, _densities);

        Assert.Equal("ag", ramp?.ToString());
    }

    [Fact]
    public void NoUsableCandidates_MakeNoRamp()
    {
        Assert.Null(RampGenerator.Generate(" z", 3, _densities));
    }

    [Fact]
    public void TheRamp_GetsTheZeroLevelAskedFor()
    {
        Assert.Equal(RampZeroLevel.Space, RampGenerator.Generate("ab", 2, _densities, RampZeroLevel.Space)?.ZeroLevel);
    }

    [Fact]
    public void PrintableAscii_IsEveryVisibleAsciiCharacter()
    {
        Assert.Equal(94, RampGenerator.PrintableAscii.Length);
        Assert.Equal('!', RampGenerator.PrintableAscii[0]);
        Assert.Equal('~', RampGenerator.PrintableAscii[^1]);
    }
}
