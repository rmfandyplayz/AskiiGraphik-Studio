// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Ramps;

namespace AskiiArt.Core.Tests.Ramps;

public sealed class RampShapeMatcherTests
{
    // Levels 1 to 4, at intensities 1/4 to 1: 'v' inks the bottom of its cell, '=' all of it evenly, '^' mostly the
    // top, and '#' all of it.
    private static readonly Ramp _ramp = Ramp.FromText("v=^#");

    private static readonly IGlyphDensityProvider _glyphs = new FakeGlyphDensities(
        new Dictionary<char, double> { ['='] = 0.5, ['#'] = 1 },
        new Dictionary<char, GlyphShape>
        {
            ['v'] = TestShapes.Bands(top: 0, bottom: 2 / 3.0),
            ['^'] = TestShapes.Bands(top: 1, bottom: 1 / 3.0)
        });

    private readonly RampShapeMatcher _matcher = new(_ramp, _glyphs);

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.3, 1)]
    [InlineData(0.6, 2)]
    [InlineData(0.8, 3)]
    [InlineData(0.95, 4)]
    public void AnEvenTarget_GetsTheLevelNearestItsIntensity(double intensity, int expected)
    {
        Assert.Equal(expected, _matcher.LevelFor(TestShapes.EvenParts(intensity)));
    }

    [Fact]
    public void AnEvenTargetHalfwayBetweenTwoLevels_GetsTheDarker()
    {
        Assert.Equal(2, _matcher.LevelFor(TestShapes.EvenParts(0.375)));
    }

    // Both are nearer '=' in tone, but an edge cell gets the glyph whose ink lies the way of the edge.
    [Fact]
    public void ATargetDarkerAtTheBottom_GetsTheGlyphInkedAtTheBottom()
    {
        Assert.Equal(1, _matcher.LevelFor(TestShapes.BandParts(top: 0.1, bottom: 0.8)));
    }

    [Fact]
    public void ATargetDarkerAtTheTop_GetsTheGlyphInkedAtTheTop()
    {
        Assert.Equal(3, _matcher.LevelFor(TestShapes.BandParts(top: 0.9, bottom: 0.2)));
    }

    // Exactly halfway between 'v' and '=' in tone, in numbers that add up without rounding. 'v' lies the edge's way,
    // though far more unevenly than so faint an edge, and shows it as far as it fits; it isn't blamed for being
    // bolder.
    [Fact]
    public void AFaintEdge_StillGetsTheGlyphInkedItsWay()
    {
        Assert.Equal(1, _matcher.LevelFor(TestShapes.BandParts(top: 0.3125, bottom: 0.4375)));
    }

    // Exactly halfway between 'v' and '=' in tone, as above. 'v' lies the wrong way, so it shows nothing of the edge
    // and is no better than the even '=', which wins as the darker.
    [Fact]
    public void AGlyphInkedTheWrongWay_ShowsNothingOfTheEdge()
    {
        Assert.Equal(2, _matcher.LevelFor(TestShapes.BandParts(top: 0.4375, bottom: 0.3125)));
    }

    [Fact]
    public void TheLevel_StaysWithinTheBoundsGiven()
    {
        var bottomHeavy = TestShapes.BandParts(top: 0.1, bottom: 0.8);

        Assert.Equal(2, _matcher.LevelFor(bottomHeavy, lowest: 2, highest: 4));
        Assert.Equal(0, _matcher.LevelFor(TestShapes.EvenParts(0.9), lowest: 0, highest: 0));
    }

    [Fact]
    public void ATargetWithTheWrongNumberOfParts_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => _matcher.LevelFor(new double[GlyphShape.PartCount - 1]));
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(0, 5)]
    [InlineData(2, 1)]
    public void BoundsOutOfOrderOrOutsideTheRamp_AreRejected(int lowest, int highest)
    {
        var target = TestShapes.EvenParts(0.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => _matcher.LevelFor(target, lowest, highest));
    }
}
