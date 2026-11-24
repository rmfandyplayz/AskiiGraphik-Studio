// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Lines;

/// <summary>
/// The path a line or a shape's outline follows across the canvas: straight pieces from point to point, in canvas
/// coordinates, and joined back to the first point when closed. Immutable.
/// </summary>
public sealed class LinePath
{
    // An ellipse is drawn as straight pieces this many to a cell of its outline, or more, so each piece is short next
    // to the parts a cell is measured in.
    private const int EllipsePiecesPerCell = 4;
    private const int FewestEllipsePieces = 16;

    private LinePath(IReadOnlyList<CanvasPoint> points, bool isClosed)
    {
        Points = points;
        IsClosed = isClosed;
    }

    /// <summary>The points the path goes through, in order.</summary>
    public IReadOnlyList<CanvasPoint> Points { get; }

    /// <summary>Whether the last point joins back to the first.</summary>
    public bool IsClosed { get; }

    /// <summary>How many straight pieces the path has.</summary>
    public int PieceCount => IsClosed ? Points.Count : Math.Max(Points.Count - 1, 0);

    /// <summary>A straight line from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static LinePath Straight(CanvasPoint from, CanvasPoint to) => new([from, to], isClosed: false);

    /// <summary>
    /// A path through <paramref name="points"/>, joined back to the first when <paramref name="closed"/>.
    /// </summary>
    /// <exception cref="ArgumentException">There are no points.</exception>
    public static LinePath Through(IEnumerable<CanvasPoint> points, bool closed)
    {
        ArgumentNullException.ThrowIfNull(points);
        CanvasPoint[] copy = [.. points];
        if (copy.Length == 0)
            throw new ArgumentException("A path needs at least one point.", nameof(points));

        return new LinePath(copy, closed);
    }

    /// <summary>
    /// The outline of <paramref name="rect"/>, running through the middles of its outermost cells.
    /// </summary>
    public static LinePath Rectangle(CellRect rect)
    {
        var left = rect.Left + 0.5;
        var top = rect.Top + 0.5;
        var right = rect.Right - 0.5;
        var bottom = rect.Bottom - 0.5;
        return new LinePath([new(left, top), new(right, top), new(right, bottom), new(left, bottom)], isClosed: true);
    }

    /// <summary>
    /// The ellipse that fits <paramref name="rect"/>, running through the middles of its outermost cells: a circle when
    /// the rect is square in cells.
    /// </summary>
    public static LinePath Ellipse(CellRect rect)
    {
        var centreX = rect.Left + rect.Width / 2.0;
        var centreY = rect.Top + rect.Height / 2.0;
        var radiusX = Math.Max(rect.Width - 1, 0) / 2.0;
        var radiusY = Math.Max(rect.Height - 1, 0) / 2.0;

        // Ramanujan's estimate of the outline's length, in cells.
        var perimeter = Math.PI * (3 * (radiusX + radiusY) -
                                   Math.Sqrt((3 * radiusX + radiusY) * (radiusX + 3 * radiusY)));
        var pieces = Math.Max(FewestEllipsePieces, (int)Math.Ceiling(perimeter * EllipsePiecesPerCell));
        var points = new CanvasPoint[pieces];
        for (var index = 0; index < pieces; index++)
        {
            var angle = 2 * Math.PI * index / pieces;
            points[index] = new CanvasPoint(centreX + radiusX * Math.Cos(angle), centreY + radiusY * Math.Sin(angle));
        }

        return new LinePath(points, isClosed: true);
    }

    /// <summary>The straight piece at <paramref name="index"/>, from its start to its end.</summary>
    public (CanvasPoint From, CanvasPoint To) Piece(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, PieceCount);
        return (Points[index], Points[(index + 1) % Points.Count]);
    }
}
