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
    public void AMatch_SaysWhetherItsGlyphShowsTheTargetsEdge()
    {
        var bottomHeavy = TestShapes.BandParts(top: 0.1, bottom: 0.8);

        Assert.Equal(new RampShapeMatcher.ShapeMatch(1, ShowsEdge: true), Match(bottomHeavy));
        Assert.Equal(new RampShapeMatcher.ShapeMatch(2, ShowsEdge: false), Match(bottomHeavy, lowest: 2, highest: 3));
        Assert.False(Match(TestShapes.EvenParts(0.5)).ShowsEdge);
    }

    // Nearest in tone, 'v' wins as no glyph shows so faint an edge enough to make up for the difference, though 'v'
    // lies the wrong way. Strict edges count shape more where the edge is faint, and never pick a glyph inked the wrong
    // way.
    [Fact]
    public void AStrictMatch_PicksTheGlyphInkedTheEdgesWay_ForAFaintEdge()
    {
        var topHeavy = TestShapes.BandParts(top: 0.4, bottom: 0);

        Assert.Equal(1, Match(topHeavy).Level);
        Assert.Equal(new RampShapeMatcher.ShapeMatch(3, ShowsEdge: true), Match(topHeavy, isStrict: true));
    }

    [Fact]
    public void AStrictMatch_NeverPicksAGlyphInkedTheWrongWay_ForAnUnevenTarget()
    {
        for (var top = 0; top <= 20; top++)
        {
            for (var bottom = 0; bottom <= 20; bottom++)
            {
                // Clearly uneven: the matcher counts a target uneven when its strays, once blurred, weigh more than
                // 5% of its tone's, and these weigh twice that before blurring.
                var difference = (top - bottom) / 20.0;
                var sum = (top + bottom) / 20.0;
                if (difference * difference <= 0.1 * sum * sum)
                    continue;

                var target = TestShapes.BandParts(top / 20.0, bottom / 20.0);
                var wrongWay = top > bottom ? 1 : 3;
                Assert.NotEqual(wrongWay, Match(target, isStrict: true).Level);
            }
        }
    }

    // Judged alone, or beside cells that want the same edge but don't show it yet, the cell's 'v' also helps show
    // theirs, as the eye blurs across cells. Beside cells that already show it, 'v' has less of the edge left to show,
    // and '=', nearer in tone, wins.
    [Fact]
    public void NeighboursThatAlreadyShowTheEdge_LeaveTheCellLessToShow()
    {
        var bottomHeavy = TestShapes.BandParts(top: 0.2, bottom: 0.8);

        Assert.Equal(1, Match(bottomHeavy).Level);
        Assert.Equal(1, Match(bottomHeavy, neighbours: SidesWanting(bottomHeavy, lookingLike: null)).Level);
        Assert.Equal(2, Match(bottomHeavy, neighbours: SidesWanting(bottomHeavy, lookingLike: 1)).Level);
    }

    [Fact]
    public void TheLevel_StaysWithinTheBoundsGiven()
    {
        var bottomHeavy = TestShapes.BandParts(top: 0.1, bottom: 0.8);

        Assert.Equal(2, _matcher.LevelFor(bottomHeavy, lowest: 2, highest: 4));
        Assert.Equal(0, _matcher.LevelFor(TestShapes.EvenParts(0.9), lowest: 0, highest: 0));
    }

    private RampShapeMatcher.ShapeMatch Match(double[] target, int lowest = 0, int highest = 4, bool isStrict = false,
        RampShapeMatcher.CellNeighbours? neighbours = null) =>
        _matcher.Match(target, lowest, highest, isStrict, neighbours);

    // The cells either side want what the middle one wants, and look like the level given, or are empty.
    private RampShapeMatcher.CellNeighbours SidesWanting(double[] target, int? lookingLike)
    {
        var neighbours = new RampShapeMatcher.CellNeighbours();
        foreach (var dx in new[] { -1, 1 })
        {
            target.CopyTo(neighbours.TargetOf(dx, 0));
            if (lookingLike is { } level)
                _matcher.LookOf(level).CopyTo(neighbours.LookOf(dx, 0));
        }

        return neighbours;
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
