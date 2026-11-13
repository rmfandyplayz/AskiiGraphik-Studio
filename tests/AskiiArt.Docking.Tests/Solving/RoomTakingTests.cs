// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class RoomTakingTests
{
    [Fact]
    public void BetweenTwo_EachGivesHalf()
    {
        double[] lengths = [100, 100];

        var found = RoomTaking.Take(lengths, [50, 50], 1, 20, othersToo: false);

        Assert.Equal(20, found);
        Assert.Equal([90, 90], lengths);
    }

    [Fact]
    public void BetweenTwo_WhenOneCantSpareItsHalf_TheOtherGivesMore()
    {
        double[] lengths = [100, 100];

        var found = RoomTaking.Take(lengths, [95, 50], 1, 20, othersToo: false);

        Assert.Equal(20, found);
        Assert.Equal([95, 85], lengths);
    }

    [Fact]
    public void AtEitherEnd_OnlyTheOneBesideItGives()
    {
        double[] first = [100, 100];
        double[] last = [100, 100];

        RoomTaking.Take(first, [50, 50], 0, 20, othersToo: false);
        RoomTaking.Take(last, [50, 50], 2, 20, othersToo: false);

        Assert.Equal([80, 100], first);
        Assert.Equal([100, 80], last);
    }

    [Fact]
    public void WhenTheTwoBesideItCantSpareAny_OnlyOthersTooTakesFromTheRest_NearestFirst()
    {
        double[] alone = [100, 60, 60, 100];
        double[] withOthers = [100, 60, 60, 100];
        double[] floors = [50, 60, 60, 50];

        var foundAlone = RoomTaking.Take(alone, floors, 2, 20, othersToo: false);
        var foundWithOthers = RoomTaking.Take(withOthers, floors, 2, 20, othersToo: true);

        Assert.Equal(0, foundAlone);
        Assert.Equal([100, 60, 60, 100], alone);
        Assert.Equal(20, foundWithOthers);
        Assert.Equal([80, 60, 60, 100], withOthers);
    }

    [Fact]
    public void WithTooLittleToSpare_ItFindsWhatThereIs_AndNothingGoesBelowItsFloor()
    {
        double[] lengths = [60, 60];

        var found = RoomTaking.Take(lengths, [50, 50], 1, 30, othersToo: true);

        Assert.Equal(20, found);
        Assert.Equal([50, 50], lengths);
    }
}
