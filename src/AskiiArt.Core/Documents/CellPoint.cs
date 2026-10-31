// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// A position in cell units: column <see cref="X"/> and row <see cref="Y"/>, 0-based, with the
/// origin at the top left (ARCHITECTURE §5.2). Also used for offsets between positions, so either
/// coordinate may be negative.
/// </summary>
/// <param name="X">The column.</param>
/// <param name="Y">The row.</param>
public readonly record struct CellPoint(int X, int Y)
{
    /// <summary>Returns this point moved by <paramref name="delta"/>.</summary>
    public CellPoint Offset(CellPoint delta) => new(X + delta.X, Y + delta.Y);

    /// <summary>Returns the point with both coordinates negated, which undoes an offset.</summary>
    public CellPoint Negate() => new(-X, -Y);
}
