// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class RampTests
{
    private static readonly FakeGlyphDensities _densities = new(new Dictionary<char, double>
    {
        // Powers of two, so halfway is exactly halfway.
        ['.'] = 0.125,
        [':'] = 0.25,
        ['#'] = 0.75,
        ['a'] = 0.3,
        ['b'] = 0.5,
        ['c'] = 0.03,
    });

    [Theory]
    [InlineData('.', 1)]
    [InlineData('#', 3)]
    [InlineData(' ', 0)]
    [InlineData('a', 2)]
    [InlineData('c', 0)]
    public void NearestLevel_IsAGlyphsOwnLevel_OrTheOneThatMeasuresNearest(char glyph, int level)
    {
        Assert.Equal(level, Ramp.FromText(".:#").NearestLevel(new Rune(glyph), _densities));
    }

    [Fact]
    public void NearestLevel_HalfwayBetweenTwo_IsTheLighter()
    {
        Assert.Equal(2, Ramp.FromText(".:#").NearestLevel(new Rune('b'), _densities));
    }

    private static readonly Rune _space = new(' ');

    [Fact]
    public void FromText_KeepsTheGlyphsInOrder_WithALevelForEachAndOneForNoInk()
    {
        var ramp = Ramp.FromText(".:#");

        Assert.Equal<Rune>([new('.'), new(':'), new('#')], ramp.Glyphs);
        Assert.Equal(3, ramp.TopLevel);
        Assert.Equal(RampZeroLevel.Empty, ramp.ZeroLevel);
        Assert.Equal(".:#", ramp.ToString());
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.16, 0)]
    [InlineData(0.17, 1)]
    [InlineData(0.5, 2)] // halfway between levels 1 and 2 goes to the darker one
    [InlineData(0.84, 3)]
    [InlineData(1.0, 3)]
    [InlineData(-5.0, 0)]
    [InlineData(7.0, 3)]
    public void LevelAt_IsTheNearestOfTheEvenlySpacedLevels(double intensity, int expectedLevel)
    {
        Assert.Equal(expectedLevel, Ramp.FromText(".:#").LevelAt(intensity));
    }

    [Fact]
    public void LevelAt_NaN_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Ramp.FromText(".:#").LevelAt(double.NaN));
    }

    [Fact]
    public void IntensityOf_EveryLevel_GoesBackToTheSameLevel()
    {
        var ramp = BuiltInRamps.Fine.Ramp;

        for (var level = 0; level <= ramp.TopLevel; level++)
            Assert.Equal(level, ramp.LevelAt(ramp.IntensityOf(level)));

        Assert.Equal(0, ramp.IntensityOf(0));
        Assert.Equal(1, ramp.IntensityOf(ramp.TopLevel));
    }

    [Fact]
    public void GlyphAt_PaintsNothingAt0_AndTheDarkestGlyphAt1()
    {
        var ramp = Ramp.FromText(".:#");

        Assert.Null(ramp.GlyphAt(0));
        Assert.Equal(new Rune('.'), ramp.GlyphAt(1.0 / 3));
        Assert.Equal(new Rune('#'), ramp.GlyphAt(1));
    }

    [Fact]
    public void GlyphAt_WithASpaceAsTheZeroLevel_PaintsASpaceAt0()
    {
        var ramp = Ramp.FromText(".:#", RampZeroLevel.Space);

        Assert.Equal(_space, ramp.GlyphAt(0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void GlyphOfAndIntensityOf_ALevelOutsideTheRamp_Throw(int level)
    {
        var ramp = Ramp.FromText(".:#");

        Assert.Throws<ArgumentOutOfRangeException>(() => ramp.GlyphOf(level));
        Assert.Throws<ArgumentOutOfRangeException>(() => ramp.IntensityOf(level));
    }

    [Fact]
    public void LevelOf_FindsTheGlyphsOwnLevel_OrNullWhenTheRampDoesntHoldIt()
    {
        var ramp = Ramp.FromText(".:#");

        Assert.Equal(1, ramp.LevelOf(new Rune('.')));
        Assert.Equal(3, ramp.LevelOf(new Rune('#')));
        Assert.Null(ramp.LevelOf(new Rune('A')));
    }

    [Fact]
    public void LevelOf_ASpace_IsLevel0OnlyWhenTheZeroLevelIsASpace()
    {
        Assert.Null(Ramp.FromText(".:#").LevelOf(_space));
        Assert.Equal(0, Ramp.FromText(".:#", RampZeroLevel.Space).LevelOf(_space));
    }

    [Theory]
    [InlineData("")]
    [InlineData(". #")] // a space is the zero level's
    [InlineData(".:.")] // a glyph twice
    [InlineData(".\t#")] // a control character
    [InlineData(".\u0301")] // a combining accent, which can't fill a cell alone
    public void Creating_WithGlyphsARampCantHold_Throws(string glyphs)
    {
        Assert.Throws<ArgumentException>(() => Ramp.FromText(glyphs));
    }

    [Fact]
    public void FitsIn_IsFalseForAsciiOnlyWhenAnyGlyphIsntAscii()
    {
        Assert.True(Ramp.FromText(".:#").FitsIn(CharacterSet.Ascii));
        Assert.False(Ramp.FromText(".:▓").FitsIn(CharacterSet.Ascii));
        Assert.True(Ramp.FromText(".:▓").FitsIn(CharacterSet.Unicode));
    }

    [Fact]
    public void SortedByDensity_OrdersLightestFirst_KeepingTiesAndTheZeroLevel()
    {
        var densities = new FakeGlyphDensities(new Dictionary<char, double>
        {
            ['#'] = 0.5,
            ['.'] = 0.1,
            ['a'] = 0.3,
            ['b'] = 0.3
        });

        var sorted = Ramp.FromText("#ba.", RampZeroLevel.Space).SortedByDensity(densities);

        Assert.Equal(".ba#", sorted.ToString());
        Assert.Equal(RampZeroLevel.Space, sorted.ZeroLevel);
    }

    [Fact]
    public void Equality_ComparesTheGlyphsAndTheZeroLevel()
    {
        var ramp = Ramp.FromText(".:#");

        Assert.Equal(ramp, Ramp.FromText(".:#"));
        Assert.Equal(ramp.GetHashCode(), Ramp.FromText(".:#").GetHashCode());
        Assert.NotEqual(ramp, Ramp.FromText(".#:"));
        Assert.NotEqual(ramp, Ramp.FromText(".:#", RampZeroLevel.Space));
    }

    [Theory]
    [InlineData(".:#", RampTextProblem.None, 0)]
    [InlineData("", RampTextProblem.Empty, 0)]
    [InlineData(". #", RampTextProblem.Space, ' ')]
    [InlineData(".\t#", RampTextProblem.Unusable, '\t')]
    [InlineData(".:.#", RampTextProblem.Repeated, '.')]
    public void Check_SaysWhatsWrongFirst_AndWithWhichCharacter(string glyphs, RampTextProblem problem, int glyph)
    {
        var check = Ramp.Check(glyphs);

        Assert.Equal(problem, check.Problem);
        Assert.Equal(glyph, check.Glyph.Value);
        Assert.Equal(problem == RampTextProblem.None, check.IsValid);
    }
}
