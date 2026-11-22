// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// How Shift and Ctrl (Cmd on a Mac) keep a Move-tool drag to a few directions (ADR 0069). Shift keeps it to left and
/// right or up and down, Ctrl to the diagonals that look like 45° on screen, and Ctrl+Shift to the diagonals of one
/// column per row. The pointer's distance from where the drag began goes whichever of those ways is nearest as it
/// looks on screen, by as far as it has come that way.
/// </summary>
internal static class MoveConstraint
{
    /// <summary>
    /// The distance the cells move for a drag that has come <paramref name="distance"/> cells from where it began,
    /// with <paramref name="modifiers"/> held, in a font whose cells are <paramref name="cellWidthToHeight"/> as wide
    /// as they're tall.
    /// </summary>
    public static CellPoint Apply(CellPoint distance, ToolModifiers modifiers, double cellWidthToHeight)
    {
        var isShiftHeld = modifiers.HasFlag(ToolModifiers.Shift);
        var isCtrlHeld = modifiers.HasFlag(ToolModifiers.Command);
        if (!isShiftHeld && !isCtrlHeld)
            return distance;

        // On screen, measured in cell heights.
        var across = Math.Abs(distance.X * cellWidthToHeight);
        var down = Math.Abs(distance.Y);
        if (!isCtrlHeld)
            return across >= down ? distance with { Y = 0 } : distance with { X = 0 };

        var (columns, rows) = isShiftHeld
            ? OneColumnPerRow(across, down, cellWidthToHeight)
            : LookingDiagonal(across, down, cellWidthToHeight);
        return new CellPoint(SideOf(distance.X) * columns, SideOf(distance.Y) * rows);
    }

    // Which way a diagonal goes along one side. A drag straight along the other side is as near the diagonals either
    // way, so it takes the one to the right or down.
    private static int SideOf(int distance) => distance < 0 ? -1 : 1;

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
