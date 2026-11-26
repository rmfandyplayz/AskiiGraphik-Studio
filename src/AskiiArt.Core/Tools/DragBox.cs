// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>
/// The box a drag from one cell to another makes, for the box-shaped selection tools and the rectangle and ellipse
/// (ADR 0060, ADR 0080): both cells are in it, it can look square on screen, and it can grow from the first cell
/// outwards.
/// </summary>
internal static class DragBox
{
    /// <summary>
    /// The box from <paramref name="anchor"/> to <paramref name="corner"/>, both included. When
    /// <paramref name="looksSquare"/>, it's grown along its shorter side, as it looks on screen, until it looks square;
    /// when <paramref name="fromCentre"/>, it's centred on <paramref name="anchor"/> with odd sides and reaches as far
    /// the other way.
    /// </summary>
    /// <param name="anchor">The cell the drag started in.</param>
    /// <param name="corner">The cell the drag has reached.</param>
    /// <param name="looksSquare">Whether the box is made to look square on screen.</param>
    /// <param name="fromCentre">Whether the box grows from the anchor outwards.</param>
    /// <param name="cellWidthToHeight">How wide a cell looks next to its height.</param>
    public static CellRect Between(CellPoint anchor, CellPoint corner, bool looksSquare, bool fromCentre,
        double cellWidthToHeight)
    {
        var across = corner.X - anchor.X;
        var down = corner.Y - anchor.Y;
        var width = fromCentre ? 2 * Math.Abs(across) + 1 : Math.Abs(across) + 1;
        var height = fromCentre ? 2 * Math.Abs(down) + 1 : Math.Abs(down) + 1;
        if (looksSquare)
            (width, height) = LookingSquare(width, height, fromCentre, cellWidthToHeight);

        if (fromCentre)
            return new CellRect(anchor.X - (width - 1) / 2, anchor.Y - (height - 1) / 2, width, height);

        var left = across >= 0 ? anchor.X : anchor.X - width + 1;
        var top = down >= 0 ? anchor.Y : anchor.Y - height + 1;
        return new CellRect(left, top, width, height);
    }

    // The box grown along its shorter side, as it looks on screen, until it looks square: a cell is usually about
    // twice as tall as it's wide, so a square box has about twice as many columns as rows (ADR 0047's perceptual
    // proportions). From the centre the sides stay odd, so the box stays centred on its first cell.
    private static (int Width, int Height) LookingSquare(int width, int height, bool fromCentre,
        double cellWidthToHeight)
    {
        if (width * cellWidthToHeight >= height)
            return (width, WholeCells(width * cellWidthToHeight, fromCentre));

        return (WholeCells(height / cellWidthToHeight, fromCentre), height);
    }

    // The nearest whole number of cells to size, at least one, and odd when it must be.
    private static int WholeCells(double size, bool isOdd)
    {
        if (!isOdd)
            return Math.Max(1, (int)Math.Round(size, MidpointRounding.AwayFromZero));

        return 2 * Math.Max(0, (int)Math.Round((size - 1) / 2, MidpointRounding.AwayFromZero)) + 1;
    }
}
