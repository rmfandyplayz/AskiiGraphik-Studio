// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Solving;

namespace AskiiArt.Docking.Tests.Solving;

public sealed class ColumnWidthsTests
{
    private const double Minimum = 160;
    private const double Tolerance = 0.001;

    [Fact]
    public void ColumnsThatFit_KeepTheirWidths() =>
        AssertWidths([300, 400], ColumnWidths.Fit([300, 400], 1000, Minimum));

    [Fact]
    public void ColumnsTooWide_ShrinkInProportion() =>
        AssertWidths([200, 400], ColumnWidths.Fit([300, 600], 600, Minimum));

    [Fact]
    public void AColumnThatWouldGoBelowTheMinimum_StaysThere_AndTheOthersShrinkMore() =>
        AssertWidths([160, 440], ColumnWidths.Fit([300, 1000], 600, Minimum));

    [Fact]
    public void WithTooLittleRoomEvenForTheMinimums_EveryColumnIsAtItsMinimum() =>
        AssertWidths([160, 160], ColumnWidths.Fit([300, 300], 200, Minimum));

    [Fact]
    public void AColumnAlreadyNarrowerThanTheMinimum_KeepsItsOwnWidth() =>
        AssertWidths([100, 300], ColumnWidths.Fit([100, 500], 400, Minimum));

    private static void AssertWidths(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(expected[index], actual[index], tolerance: Tolerance);
    }
}
