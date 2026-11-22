// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// The zone around the canvas that the Move tool can take the selection into (ADR 0071): 10 columns past the left and
/// right edges, and as far on screen past the top and bottom, which is 5 rows when cells are twice as tall as wide.
/// </summary>
public static class SelectionZone
{
    /// <summary>How many columns the zone reaches past the canvas's left and right edges.</summary>
    public const int ColumnsPastCanvas = 10;

    /// <summary>
    /// How many rows the zone reaches past the canvas's top and bottom edges, for cells
    /// <paramref name="cellWidthToHeight"/> as wide as they're tall: as far on screen as the columns reach, and at
    /// least one row.
    /// </summary>
    public static int RowsPastCanvas(double cellWidthToHeight)
    {
        var rows = Math.Round(ColumnsPastCanvas * cellWidthToHeight, MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(rows, 1, ColumnsPastCanvas * 10);
    }

    /// <summary>The canvas and the zone around it, for cells <paramref name="cellWidthToHeight"/> as wide as tall.</summary>
    public static CellRect Around(CanvasSize canvasSize, double cellWidthToHeight)
    {
        var rows = RowsPastCanvas(cellWidthToHeight);
        return CellRect.FromEdges(-ColumnsPastCanvas, -rows, canvasSize.Width + ColumnsPastCanvas,
            canvasSize.Height + rows);
    }
}
