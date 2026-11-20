// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>Which side of its cells an outline edge runs along.</summary>
public enum CellEdgeSide
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
/// One straight piece of the outline of a set of cells, such as a brush footprint or a selection: the
/// <paramref name="Side"/> of <paramref name="Length"/> cells, starting at (<paramref name="Column"/>,
/// <paramref name="Row"/>). Top and bottom edges run along a row, and left and right edges down a column. The edge
/// belongs to the cells inside the outline, so an outline drawn along it stays inside them.
/// </summary>
public readonly record struct CellEdge(CellEdgeSide Side, int Column, int Row, int Length);
