// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// A point on the canvas measured in cells, with fractions: (0, 0) is the canvas's top-left corner, and (2.5, 1) the
/// middle of the top edge of the cell at column 2, row 1. A lasso's path is made of these.
/// </summary>
/// <param name="X">How many cell widths from the canvas's left edge.</param>
/// <param name="Y">How many cell heights from the canvas's top edge.</param>
public readonly record struct CanvasPoint(double X, double Y)
{
    /// <summary>The cell the point is in.</summary>
    public CellPoint Cell => new((int)Math.Floor(X), (int)Math.Floor(Y));
}
