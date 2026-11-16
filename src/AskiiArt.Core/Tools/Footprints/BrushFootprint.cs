// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Footprints;

/// <summary>The cells of one row of a footprint: <see cref="Length"/> cells from column <see cref="Start"/>.</summary>
public readonly record struct FootprintRow(int Start, int Length)
{
    /// <summary>The column just past the row's last cell.</summary>
    public int End => Start + Length;
}

/// <summary>Which side of its cells an outline edge runs along.</summary>
public enum FootprintEdgeSide
{
    /// <summary>The top of a run of cells in one row.</summary>
    Top,

    /// <summary>The bottom of a run of cells in one row.</summary>
    Bottom,

    /// <summary>The left of a run of cells in one column.</summary>
    Left,

    /// <summary>The right of a run of cells in one column.</summary>
    Right
}

/// <summary>
/// One straight piece of a footprint's outline: the <paramref name="Side"/> of <paramref name="Length"/> cells,
/// starting at (<paramref name="Column"/>, <paramref name="Row"/>) relative to the footprint's top-left. Top and
/// bottom edges run along a row, and left and right edges down a column.
/// </summary>
public readonly record struct FootprintEdge(FootprintEdgeSide Side, int Column, int Row, int Length);

/// <summary>
/// The cells a <see cref="BrushTip"/> covers, relative to its top-left (ADR 0047), in a font whose cells are
/// <c>cellWidthToHeight</c> times as wide as they're tall. Every row is one unbroken run of cells, because both shapes
/// are convex. Immutable.
/// </summary>
public sealed class BrushFootprint
{
    private readonly FootprintRow[] _rows;

    private BrushFootprint(BrushTip tip, int width, FootprintRow[] rows)
    {
        Tip = tip;
        Width = width;
        _rows = rows;
        CellCount = rows.Sum(row => row.Length);
        Edges = FindEdges();
    }

    /// <summary>The tip this is the footprint of.</summary>
    public BrushTip Tip { get; }

    /// <summary>How many cells wide the footprint is.</summary>
    public int Width { get; }

    /// <summary>How many cells tall the footprint is.</summary>
    public int Height => _rows.Length;

    /// <summary>The cells of each row, from the top.</summary>
    public IReadOnlyList<FootprintRow> Rows => _rows;

    /// <summary>How many cells the footprint covers.</summary>
    public int CellCount { get; }

    /// <summary>The outline: every side of a covered cell whose neighbour there isn't covered.</summary>
    public IReadOnlyList<FootprintEdge> Edges { get; }

    /// <summary>The footprint of <paramref name="tip"/>.</summary>
    /// <param name="tip">The tip.</param>
    /// <param name="cellWidthToHeight">
    /// A cell's width divided by its height in the document's font, which a perceptual tip uses to look right on
    /// screen.
    /// </param>
    public static BrushFootprint Create(BrushTip tip, double cellWidthToHeight)
    {
        ArgumentNullException.ThrowIfNull(tip);
        if (!double.IsFinite(cellWidthToHeight) || cellWidthToHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(cellWidthToHeight), cellWidthToHeight, "Must be above zero.");

        var width = tip.Size;
        var height = HeightOf(tip, cellWidthToHeight);
        var rows = new FootprintRow[height];
        for (var row = 0; row < height; row++)
        {
            rows[row] = tip.Shape == BrushShape.Square
                ? new FootprintRow(0, width)
                : CircleRow(width, height, row);
        }

        return new BrushFootprint(tip, width, rows);
    }

    /// <summary>Whether the cell at (<paramref name="column"/>, <paramref name="row"/>) is covered.</summary>
    public bool Contains(int column, int row)
    {
        if (row < 0 || row >= _rows.Length)
            return false;

        var cells = _rows[row];
        return column >= cells.Start && column < cells.End;
    }

    /// <summary>
    /// Where the footprint's top-left goes when the pointer is in <paramref name="cell"/>, at
    /// (<paramref name="subCellX"/>, <paramref name="subCellY"/>) within it. The footprint is centred on the pointer
    /// as nearly as whole cells allow: an odd size centres on the pointer's cell, and an even size leans towards the
    /// half of the cell the pointer is in.
    /// </summary>
    public CellPoint TopLeftAt(CellPoint cell, double subCellX, double subCellY) => new(
        (int)Math.Floor(cell.X + subCellX - Width / 2.0 + 0.5),
        (int)Math.Floor(cell.Y + subCellY - Height / 2.0 + 0.5));

    /// <summary>Where the footprint's top-left goes for <paramref name="input"/>.</summary>
    public CellPoint TopLeftAt(ToolPointerInput input) => TopLeftAt(input.Cell, input.SubCellX, input.SubCellY);

    // A perceptual tip as tall as it looks wide, which is never less than one cell.
    private static int HeightOf(BrushTip tip, double cellWidthToHeight)
    {
        if (tip.Proportions == BrushProportions.Mathematical)
            return tip.Size;

        var height = Math.Round(tip.Size * cellWidthToHeight, MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(height, BrushTip.MinimumSize, BrushTip.MaximumSize);
    }

    // The cells whose centres lie inside the ellipse that fills the footprint. The ellipse shrinks by a quarter of a
    // cell at the widest point, so small sizes look round: size 3 is a plus rather than a full square.
    private static FootprintRow CircleRow(int width, int height, int row)
    {
        var limit = 1 - 1.0 / (2 * Math.Max(width, height));
        var y = (row + 0.5 - height / 2.0) / (height / 2.0);
        var start = width;
        var end = 0;
        for (var column = 0; column < width; column++)
        {
            var x = (column + 0.5 - width / 2.0) / (width / 2.0);
            if (x * x + y * y > limit)
                continue;

            start = Math.Min(start, column);
            end = column + 1;
        }

        return end > start ? new FootprintRow(start, end - start) : new FootprintRow(width / 2, 1);
    }

    private FootprintEdge[] FindEdges()
    {
        var edges = new List<FootprintEdge>();
        for (var row = 0; row < Height; row++)
        {
            AddRuns(edges, FootprintEdgeSide.Top, row, row - 1);
            AddRuns(edges, FootprintEdgeSide.Bottom, row, row + 1);
        }

        for (var column = 0; column < Width; column++)
        {
            AddColumnRuns(edges, FootprintEdgeSide.Left, column, column - 1);
            AddColumnRuns(edges, FootprintEdgeSide.Right, column, column + 1);
        }

        return [.. edges];
    }

    // The runs of cells in row that have no covered neighbour in neighbourRow.
    private void AddRuns(List<FootprintEdge> edges, FootprintEdgeSide side, int row, int neighbourRow)
    {
        var cells = _rows[row];
        var runStart = -1;
        for (var column = cells.Start; column <= cells.End; column++)
        {
            var isEdge = column < cells.End && !Contains(column, neighbourRow);
            if (isEdge && runStart < 0)
            {
                runStart = column;
            }
            else if (!isEdge && runStart >= 0)
            {
                edges.Add(new FootprintEdge(side, runStart, row, column - runStart));
                runStart = -1;
            }
        }
    }

    // The runs of cells in column that have no covered neighbour in neighbourColumn.
    private void AddColumnRuns(List<FootprintEdge> edges, FootprintEdgeSide side, int column, int neighbourColumn)
    {
        var runStart = -1;
        for (var row = 0; row <= Height; row++)
        {
            var isEdge = row < Height && Contains(column, row) && !Contains(neighbourColumn, row);
            if (isEdge && runStart < 0)
            {
                runStart = row;
            }
            else if (!isEdge && runStart >= 0)
            {
                edges.Add(new FootprintEdge(side, column, runStart, row - runStart));
                runStart = -1;
            }
        }
    }
}
