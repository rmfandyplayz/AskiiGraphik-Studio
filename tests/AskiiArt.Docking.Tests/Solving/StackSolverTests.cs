// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using CsCheck;
using static AskiiArt.Docking.Solving.StackSolver;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class StackSolverTests
{
    private const double Gap = 8;
    private const double Title = 28;
    private const double Minimum = 100;

    private static Entry Open(double share = 1) => new(share, IsTitleOnly: false, Minimum);

    private static Entry TitleOnly() => new(1, IsTitleOnly: true, Minimum);

    [Fact]
    public void ALoneWindow_FillsTheStack()
    {
        var slot = Assert.Single(Solve([Open()], 500, Gap, Title));

        Assert.Equal(new Slot(0, 500, false), slot);
    }

    [Fact]
    public void EqualShares_SplitTheLengthEvenly_WithTheGapBetween()
    {
        var slots = Solve([Open(), Open()], 508, Gap, Title);

        Assert.Equal(new Slot(0, 250, false), slots[0]);
        Assert.Equal(new Slot(258, 250, false), slots[1]);
    }

    [Fact]
    public void Shares_DecideWhoGetsMore()
    {
        var slots = Solve([Open(3), Open(1)], 408, Gap, Title);

        Assert.Equal(300, slots[0].Length);
        Assert.Equal(100, slots[1].Length);
    }

    [Fact]
    public void ATitleOnlyWindow_StaysThatWay_AndTheOthersGetTheRest()
    {
        var slots = Solve([TitleOnly(), Open()], 500, Gap, Title);

        Assert.Equal(new Slot(0, Title, true), slots[0]);
        Assert.Equal(new Slot(Title + Gap, 500 - Title - Gap, false), slots[1]);
    }

    [Fact]
    public void AWindowBelowItsMinimum_GetsItsMinimum_AndTheOthersShareTheRest()
    {
        var slots = Solve([Open(9), Open(1)], 508, Gap, Title);

        Assert.Equal(400, slots[0].Length);
        Assert.Equal(Minimum, slots[1].Length);
    }

    [Theory]
    [InlineData(330, new[] { false, false, false })]
    [InlineData(260, new[] { false, false, true })]
    [InlineData(200, new[] { false, true, true })]
    public void WithTooLittleRoom_WindowsGoTitleOnly_FromTheEndOfTheStack(double length, bool[] expected)
    {
        var slots = Solve([Open(), Open(), Open()], length, Gap, Title);

        Assert.Equal(expected, slots.Select(slot => slot.IsTitleOnly));
        Assert.Equal(length, slots[^1].Start + slots[^1].Length, tolerance: 0.001);
    }

    [Fact]
    public void WhenEveryWindowIsTitleOnly_TheLastOneOpens()
    {
        var slots = Solve([TitleOnly(), TitleOnly()], 500, Gap, Title);

        Assert.Equal([true, false], slots.Select(slot => slot.IsTitleOnly));
    }

    [Fact]
    public void NoWindows_GiveNoSlots() => Assert.Empty(Solve([], 500, Gap, Title));

    [Fact]
    public void WithRoomForEveryTitle_TheStackIsExactlyFilled_AndEachIsTitleOnlyOrAtLeastItsMinimum()
    {
        var entries = Gen.Select(Gen.Double[0.1, 5], Gen.Bool,
            (share, isTitleOnly) => new Entry(share, isTitleOnly, Minimum));

        Gen.Select(entries.List[1, 6], Gen.Double[0, 3000]).Sample((stack, extra) =>
        {
            var length = (stack.Count * Title) + ((stack.Count - 1) * Gap) + Minimum + extra;

            var slots = Solve(stack, length, Gap, Title);

            Assert.Equal(length, slots[^1].Start + slots[^1].Length, tolerance: 0.001);
            for (var index = 0; index < slots.Count; index++)
            {
                if (slots[index].IsTitleOnly)
                    Assert.Equal(Title, slots[index].Length, tolerance: 0.001);
                else
                    Assert.True(slots[index].Length >= Minimum - 0.001);

                var previousEnd = index == 0 ? -Gap : slots[index - 1].Start + slots[index - 1].Length;
                Assert.Equal(previousEnd + Gap, slots[index].Start, tolerance: 0.001);
            }
        });
    }

    [Fact]
    public void WithNoRoomAtAll_NothingLeavesTheColumn()
    {
        Gen.Select(Gen.Int[1, 6], Gen.Double[0, 120]).Sample((count, length) =>
        {
            var slots = Solve([.. Enumerable.Repeat(Open(), count)], length, Gap, Title);

            Assert.All(slots, slot =>
            {
                Assert.True(slot.Length >= 0);
                Assert.True(slot.Start + slot.Length <= length + 0.001);
            });
        });
    }
}
