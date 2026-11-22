// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Moving;

namespace AskiiArt.Core.Tests.Tools;

// How Shift and Ctrl keep a Move-tool drag to a few directions (ADR 0069). Most fonts' cells are about half as wide as
// they're tall, so a row looks as long as two columns.
public sealed class MoveConstraintTests
{
    private const double HalfAsWide = 0.5;

    [Fact]
    public void WithNoModifiers_TheMoveFollowsThePointer()
    {
        Assert.Equal(new CellPoint(3, 2), Apply(3, 2, ToolModifiers.None));
        Assert.Equal(new CellPoint(3, 2), Apply(3, 2, ToolModifiers.Alt));
    }

    // Three columns look shorter than two rows.
    [Theory]
    [InlineData(1.0, 3, 2, 3, 0)]
    [InlineData(HalfAsWide, 3, 2, 0, 2)]
    [InlineData(HalfAsWide, -5, 2, -5, 0)]
    [InlineData(HalfAsWide, 1, -3, 0, -3)]
    public void Shift_KeepsTheMoveToTheAxisThePointerHasGoneFurtherAlong_OnScreen(double cellWidthToHeight, int across,
        int down, int expectedAcross, int expectedDown)
    {
        Assert.Equal(new CellPoint(expectedAcross, expectedDown),
            Apply(across, down, ToolModifiers.Shift, cellWidthToHeight));
    }

    // Four columns look as long as two rows, so (4, 2) looks like 45°. A drag mostly along one side still goes along
    // a diagonal, as far as it has come along it.
    [Theory]
    [InlineData(5, 2, 4, 2)]
    [InlineData(-4, -2, -4, -2)]
    [InlineData(-3, 2, -4, 2)]
    [InlineData(9, 1, 6, 3)]
    [InlineData(-1, -5, -6, -3)]
    public void Ctrl_KeepsADragToTheDiagonalsThatLookLike45Degrees(int across, int down, int expectedAcross,
        int expectedDown)
    {
        Assert.Equal(new CellPoint(expectedAcross, expectedDown),
            Apply(across, down, ToolModifiers.Command, HalfAsWide));
    }

    [Theory]
    [InlineData(3, 2, 2, 2)]
    [InlineData(-2, 3, -3, 3)]
    [InlineData(4, 1, 2, 2)]
    [InlineData(-1, -6, -5, -5)]
    public void CtrlAndShift_KeepADragToTheDiagonalsOfOneColumnPerRow(int across, int down, int expectedAcross,
        int expectedDown)
    {
        Assert.Equal(new CellPoint(expectedAcross, expectedDown),
            Apply(across, down, ToolModifiers.Command | ToolModifiers.Shift, HalfAsWide));
    }

    // As near the diagonals one way as the other, so it goes to the right or down.
    [Theory]
    [InlineData(6, 0, 4, 2)]
    [InlineData(-6, 0, -4, 2)]
    [InlineData(0, -4, 4, -2)]
    public void ACtrlDragStraightAlongOneSide_TakesTheDiagonalToTheRightOrDown(int across, int down,
        int expectedAcross, int expectedDown)
    {
        Assert.Equal(new CellPoint(expectedAcross, expectedDown),
            Apply(across, down, ToolModifiers.Command, HalfAsWide));
    }

    [Fact]
    public void WithCellsWiderThanTheyreTall_ALookingDiagonal_StillLooksLike45Degrees()
    {
        Assert.Equal(new CellPoint(2, 4), Apply(2, 5, ToolModifiers.Command, cellWidthToHeight: 2));
    }

    private static CellPoint Apply(int across, int down, ToolModifiers modifiers, double cellWidthToHeight = 1) =>
        MoveConstraint.Apply(new CellPoint(across, down), modifiers, cellWidthToHeight);
}
