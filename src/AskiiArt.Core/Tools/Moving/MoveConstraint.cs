// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// How Shift and Ctrl (Cmd on a Mac) keep a Move-tool drag to a few directions (ADR 0069). Shift keeps it to left and
/// right or up and down; Ctrl adds diagonals that look like 45° on screen; Ctrl+Shift adds diagonals of one column per
/// row instead. The pointer's distance from where the drag began goes the way it's nearest to as it looks on screen,
/// by as far as it has come that way.
/// </summary>
internal static class MoveConstraint
{
    private const double RightAngle = Math.PI / 2;

    /// <summary>
    /// The distance the cells move for a drag that has come <paramref name="distance"/> cells from where it began,
    /// with <paramref name="modifiers"/> held, in a font whose cells are <paramref name="cellWidthToHeight"/> as wide
    /// as they're tall.
    /// </summary>
    public static CellPoint Apply(CellPoint distance, ToolModifiers modifiers, double cellWidthToHeight)
    {
        var keepsToAxes = modifiers.HasFlag(ToolModifiers.Shift);
        var allowsDiagonals = modifiers.HasFlag(ToolModifiers.Command);
        if (!keepsToAxes && !allowsDiagonals)
            return distance;

        // On screen, measured in cell heights.
        var across = Math.Abs(distance.X * cellWidthToHeight);
        var down = Math.Abs(distance.Y);
        if (!allowsDiagonals)
            return across >= down ? distance with { Y = 0 } : distance with { X = 0 };

        // A direction takes the angles nearer it than the next one, so the diagonal's reach ends halfway to each axis.
        var angle = Math.Atan2(down, across);
        var diagonalAngle = keepsToAxes ? Math.Atan2(1, cellWidthToHeight) : RightAngle / 2;
        if (angle <= diagonalAngle / 2)
            return distance with { Y = 0 };

        if (angle > (diagonalAngle + RightAngle) / 2)
            return distance with { X = 0 };

        var (columns, rows) = keepsToAxes
            ? OneColumnPerRow(across, down, cellWidthToHeight)
            : LookingDiagonal(across, down, cellWidthToHeight);
        return new CellPoint(Math.Sign(distance.X) * columns, Math.Sign(distance.Y) * rows);
    }

    // As far along the diagonal as the pointer has come, in whole steps of one column and one row.
    private static (int Columns, int Rows) OneColumnPerRow(double across, double down, double cellWidthToHeight)
    {
        var steps = ((across * cellWidthToHeight) + down) / ((cellWidthToHeight * cellWidthToHeight) + 1);
        var wholeSteps = WholeCells(steps);
        return (wholeSteps, wholeSteps);
    }

    // As far along the 45° line as the pointer has come. The side whose cells are bigger on screen is rounded first
    // and the other follows it, so the cells land as near the line as whole cells can.
    private static (int Columns, int Rows) LookingDiagonal(double across, double down, double cellWidthToHeight)
    {
        var alongEachSide = (across + down) / 2;
        if (cellWidthToHeight <= 1)
        {
            var rows = WholeCells(alongEachSide);
            return (WholeCells(rows / cellWidthToHeight), rows);
        }

        var columns = WholeCells(alongEachSide / cellWidthToHeight);
        return (columns, WholeCells(columns * cellWidthToHeight));
    }

    private static int WholeCells(double cells) => (int)Math.Round(cells, MidpointRounding.AwayFromZero);
}
