// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// A rectangle of cells (ARCHITECTURE §5.2). <see cref="Right"/> and <see cref="Bottom"/> are
/// exclusive, so a rect at (2, 3) with width 4 covers columns 2 to 5.
/// </summary>
public readonly record struct CellRect
{
    /// <summary>Creates a rect from its top-left cell and its size.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The width or height is negative.</exception>
    public CellRect(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// The rect that covers no cells. Every empty rect behaves like this one, but equality
    /// compares all four fields, so an empty rect at another position isn't equal to it.
    /// </summary>
    public static CellRect Empty => default;

    /// <summary>The column of the left edge.</summary>
    public int X { get; }

    /// <summary>The row of the top edge.</summary>
    public int Y { get; }

    /// <summary>The number of columns.</summary>
    public int Width { get; }

    /// <summary>The number of rows.</summary>
    public int Height { get; }

    /// <summary>The first column inside the rect. Same as <see cref="X"/>.</summary>
    public int Left => X;

    /// <summary>The first row inside the rect. Same as <see cref="Y"/>.</summary>
    public int Top => Y;

    /// <summary>The first column to the right of the rect (exclusive).</summary>
    public int Right => X + Width;

    /// <summary>The first row below the rect (exclusive).</summary>
    public int Bottom => Y + Height;

    /// <summary>The top-left cell.</summary>
    public CellPoint Position => new(X, Y);

    /// <summary>True when the rect covers no cells.</summary>
    public bool IsEmpty => Width == 0 || Height == 0;

    /// <summary>The number of cells the rect covers.</summary>
    public int Area => Width * Height;

    /// <summary>Creates the rect between two edges. Right and bottom are exclusive.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Right is left of left, or bottom is above top.</exception>
    public static CellRect FromEdges(int left, int top, int right, int bottom) =>
        new(left, top, right - left, bottom - top);

    /// <summary>Creates the 1 × 1 rect that covers exactly <paramref name="point"/>.</summary>
    public static CellRect FromPoint(CellPoint point) => new(point.X, point.Y, 1, 1);

    /// <summary>True when <paramref name="point"/> is inside the rect.</summary>
    public bool Contains(CellPoint point) =>
        point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    /// <summary>True when every cell of <paramref name="other"/> is inside this rect. An empty rect is inside every rect.</summary>
    public bool Contains(CellRect other) =>
        other.IsEmpty || (!IsEmpty && other.Left >= Left && other.Right <= Right && other.Top >= Top &&
                          other.Bottom <= Bottom);

    /// <summary>True when the two rects share at least one cell.</summary>
    public bool Intersects(CellRect other) =>
        !IsEmpty && !other.IsEmpty && other.Left < Right && Left < other.Right && other.Top < Bottom &&
        Top < other.Bottom;

    /// <summary>Returns the cells both rects share, or <see cref="Empty"/> if they share none.</summary>
    public CellRect Intersect(CellRect other)
    {
        if (!Intersects(other))
            return Empty;

        return FromEdges(
            Math.Max(Left, other.Left),
            Math.Max(Top, other.Top),
            Math.Min(Right, other.Right),
            Math.Min(Bottom, other.Bottom));
    }

    /// <summary>
    /// Returns the smallest rect that contains both rects. An empty rect adds nothing, so the
    /// union with <see cref="Empty"/> is the other rect.
    /// </summary>
    public CellRect Union(CellRect other)
    {
        if (IsEmpty)
            return other.IsEmpty ? Empty : other;

        if (other.IsEmpty)
            return this;

        return FromEdges(
            Math.Min(Left, other.Left),
            Math.Min(Top, other.Top),
            Math.Max(Right, other.Right),
            Math.Max(Bottom, other.Bottom));
    }

    /// <summary>Returns this rect moved by <paramref name="delta"/>.</summary>
    public CellRect Offset(CellPoint delta) => new(X + delta.X, Y + delta.Y, Width, Height);
}
