// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Numerics;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// Which cells of a canvas are selected (ARCHITECTURE §10): any set of them, not only a rectangle. Immutable, so undo
/// steps share masks rather than copy them. Only the cells inside <see cref="Bounds"/> are stored, a bit each, and a
/// rectangle stores no bits at all, so a small selection on a big canvas stays small (ADR 0055).
/// </summary>
public sealed class SelectionMask : IEquatable<SelectionMask>
{
    private const long FixedOverheadInBytes = 64;

    // A row of bits for each row of Bounds, _stride words each: bit i of a row is the cell in column Bounds.Left + i.
    // Null when every cell in Bounds is selected. Otherwise Bounds is tight (its first and last rows and columns each
    // hold a selected cell), so equal selections have equal bits.
    private readonly ulong[]? _bits;
    private readonly int _stride;
    private CellEdge[]? _edges;

    private SelectionMask(CanvasSize canvasSize, CellRect bounds, ulong[]? bits)
    {
        CanvasSize = canvasSize;
        Bounds = bounds.IsEmpty ? CellRect.Empty : bounds;
        _bits = bits;
        _stride = BitRow.WordsFor(bounds.Width);
    }

    /// <summary>The size of the canvas the mask belongs to.</summary>
    public CanvasSize CanvasSize { get; }

    /// <summary>The smallest rect holding every selected cell, or <see cref="CellRect.Empty"/> when none is.</summary>
    public CellRect Bounds { get; }

    /// <summary>True when no cell is selected.</summary>
    public bool IsEmpty => Bounds.IsEmpty;

    /// <summary>True when the selected cells are exactly those of <see cref="Bounds"/>, and there are some.</summary>
    public bool IsRectangle => !IsEmpty && _bits is null;

    /// <summary>Roughly how much memory the mask holds, in bytes.</summary>
    public long EstimatedMemoryInBytes => FixedOverheadInBytes + (_bits?.LongLength ?? 0) * sizeof(ulong);

    /// <summary>
    /// The outline of the selected cells: every side of a selected cell whose neighbour there isn't selected, joined
    /// into straight runs, in canvas coordinates. A hole in the selection has an outline of its own. Worked out the
    /// first time it's asked for.
    /// </summary>
    public IReadOnlyList<CellEdge> Edges => _edges ??= FindEdges();

    /// <summary>A mask with nothing selected.</summary>
    public static SelectionMask Empty(CanvasSize canvasSize) => new(canvasSize, CellRect.Empty, bits: null);

    /// <summary>A mask with every cell of the canvas selected.</summary>
    public static SelectionMask All(CanvasSize canvasSize) => new(canvasSize, canvasSize.Bounds, bits: null);

    /// <summary>A mask with the cells of <paramref name="rect"/> selected, leaving out any off the canvas.</summary>
    public static SelectionMask Rectangle(CanvasSize canvasSize, CellRect rect) =>
        new(canvasSize, rect.Intersect(canvasSize.Bounds), bits: null);

    /// <summary>True when the cell at <paramref name="point"/> is selected. A point off the canvas never is.</summary>
    public bool Contains(CellPoint point)
    {
        if (!Bounds.Contains(point))
            return false;

        return _bits is null || BitRow.IsSet(Row(point.Y - Bounds.Top), point.X - Bounds.Left);
    }

    /// <summary>
    /// The runs of selected cells in <paramref name="row"/>, left to right, each as the column of its first cell and
    /// how many cells it has. A row with none, or off the canvas, has no runs.
    /// </summary>
    public SelectionRowRuns RunsInRow(int row)
    {
        if (row < Bounds.Top || row >= Bounds.Bottom)
            return default;

        if (_bits is null)
            return new SelectionRowRuns(Bounds.Left, Bounds.Width);

        return new SelectionRowRuns(Bounds.Left, BitRow.Runs(Row(row - Bounds.Top), Bounds.Width));
    }

    /// <summary>
    /// The same cells after the canvas is resized by <paramref name="plan"/>: moved with the art, and without any
    /// that are cropped off (ADR 0055).
    /// </summary>
    /// <exception cref="ArgumentException">The plan resizes a canvas of another size.</exception>
    public SelectionMask Resized(CanvasResizePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (plan.OldSize != CanvasSize)
        {
            throw new ArgumentException($"The plan resizes a {plan.OldSize} canvas, not this {CanvasSize} one.",
                nameof(plan));
        }

        var kept = Bounds.Offset(plan.ContentOffset).Intersect(plan.NewSize.Bounds);
        if (kept.IsEmpty)
            return Empty(plan.NewSize);

        if (_bits is null)
            return new SelectionMask(plan.NewSize, kept, bits: null);

        var builder = new SelectionMaskBuilder(plan.NewSize, kept);
        for (var y = kept.Top; y < kept.Bottom; y++)
        {
            var oldRow = y - plan.ContentOffset.Y - Bounds.Top;
            foreach (var (start, length) in BitRow.Runs(Row(oldRow), Bounds.Width))
                builder.AddRun(y, Bounds.Left + start + plan.ContentOffset.X, length);
        }

        return builder.Build();
    }

    /// <summary>
    /// The selection after combining <paramref name="shape"/> with this one by <paramref name="mode"/>: the shape
    /// alone, both, this without the shape, or only what both have.
    /// </summary>
    /// <exception cref="ArgumentException">The shape belongs to a canvas of another size.</exception>
    public SelectionMask CombinedWith(SelectionMask shape, SelectionCombineMode mode)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (shape.CanvasSize != CanvasSize)
        {
            throw new ArgumentException($"The shape belongs to a {shape.CanvasSize} canvas, not this {CanvasSize} one.",
                nameof(shape));
        }

        switch (mode)
        {
            case SelectionCombineMode.Replace:
                return shape;
            case SelectionCombineMode.Add:
                if (IsEmpty)
                    return shape;

                if (shape.IsEmpty || (IsRectangle && Bounds.Contains(shape.Bounds)))
                    return this;

                var union = new SelectionMaskBuilder(CanvasSize, Bounds.Union(shape.Bounds));
                AddTo(union);
                shape.AddTo(union);
                return union.Build();
            case SelectionCombineMode.Subtract:
                if (!Bounds.Intersects(shape.Bounds))
                    return this;

                var difference = new SelectionMaskBuilder(CanvasSize, Bounds);
                AddTo(difference);
                shape.RemoveFrom(difference);
                return difference.Build();
            case SelectionCombineMode.Intersect:
                var shared = Bounds.Intersect(shape.Bounds);
                if (shared.IsEmpty)
                    return Empty(CanvasSize);

                var mine = new SelectionMaskBuilder(CanvasSize, shared);
                AddTo(mine);
                var theirs = new SelectionMaskBuilder(CanvasSize, shared);
                shape.AddTo(theirs);
                mine.IntersectWith(theirs);
                return mine.Build();
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    /// <inheritdoc/>
    public bool Equals(SelectionMask? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || other.CanvasSize != CanvasSize || other.Bounds != Bounds)
            return false;

        if (_bits is null || other._bits is null)
            return _bits is null && other._bits is null;

        return _bits.AsSpan().SequenceEqual(other._bits);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SelectionMask);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(CanvasSize, Bounds, _bits is null);

    /// <inheritdoc/>
    public override string ToString() =>
        IsEmpty ? $"No selection on {CanvasSize}" : $"Selection in {Bounds} on {CanvasSize}";

    /// <summary>
    /// Makes a mask from bits that cover <paramref name="area"/>, a row of <paramref name="stride"/> words for each of
    /// its rows. Trims the bounds to the selected cells, and stores no bits for a rectangle.
    /// </summary>
    internal static SelectionMask FromBits(CanvasSize canvasSize, CellRect area, ulong[] bits, int stride)
    {
        var bounds = TightBounds(area, bits, stride);
        if (bounds.IsEmpty)
            return Empty(canvasSize);

        if (BitRow.CountSetBits(bits) == bounds.Area)
            return new SelectionMask(canvasSize, bounds, bits: null);

        if (bounds == area)
            return new SelectionMask(canvasSize, bounds, bits);

        // Copy the bits into a tight block, so equal selections always store equal bits.
        var tightStride = BitRow.WordsFor(bounds.Width);
        var tight = new ulong[tightStride * bounds.Height];
        var columnShift = bounds.Left - area.Left;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            var source = bits.AsSpan((y - area.Top) * stride, stride);
            var target = tight.AsSpan((y - bounds.Top) * tightStride, tightStride);
            foreach (var (start, length) in BitRow.Runs(source, area.Width))
                BitRow.SetRange(target, start - columnShift, length);
        }

        return new SelectionMask(canvasSize, bounds, tight);
    }

    private ReadOnlySpan<ulong> Row(int row) => _bits.AsSpan(row * _stride, _stride);

    // Adds every selected cell to the builder, a run at a time.
    private void AddTo(SelectionMaskBuilder builder)
    {
        for (var row = 0; row < Bounds.Height; row++)
        {
            if (_bits is null)
            {
                builder.AddRun(Bounds.Top + row, Bounds.Left, Bounds.Width);
                continue;
            }

            foreach (var (start, length) in BitRow.Runs(Row(row), Bounds.Width))
                builder.AddRun(Bounds.Top + row, Bounds.Left + start, length);
        }
    }

    // Removes every selected cell from the builder, a run at a time.
    private void RemoveFrom(SelectionMaskBuilder builder)
    {
        for (var row = 0; row < Bounds.Height; row++)
        {
            if (_bits is null)
            {
                builder.RemoveRun(Bounds.Top + row, Bounds.Left, Bounds.Width);
                continue;
            }

            foreach (var (start, length) in BitRow.Runs(Row(row), Bounds.Width))
                builder.RemoveRun(Bounds.Top + row, Bounds.Left + start, length);
        }
    }

    private static CellRect TightBounds(CellRect area, ulong[] bits, int stride)
    {
        int top = int.MaxValue, bottom = int.MinValue, left = int.MaxValue, right = int.MinValue;
        for (var row = 0; row < area.Height; row++)
        {
            var span = bits.AsSpan(row * stride, stride);
            var first = BitRow.NextSetBit(span, 0, area.Width);
            if (first >= area.Width)
                continue;

            top = Math.Min(top, row);
            bottom = row + 1;
            left = Math.Min(left, first);
            right = Math.Max(right, LastSetBit(span, area.Width) + 1);
        }

        if (bottom < 0)
            return CellRect.Empty;

        return CellRect.FromEdges(area.Left + left, area.Top + top, area.Left + right, area.Top + bottom);
    }

    private static int LastSetBit(ReadOnlySpan<ulong> row, int length)
    {
        for (var wordIndex = BitRow.WordsFor(length) - 1; wordIndex >= 0; wordIndex--)
        {
            if (row[wordIndex] != 0)
                return wordIndex * BitRow.BitsPerWord + 63 - BitOperations.LeadingZeroCount(row[wordIndex]);
        }

        return -1;
    }

    private CellEdge[] FindEdges()
    {
        if (IsEmpty)
            return [];

        if (_bits is null)
        {
            return
            [
                new CellEdge(CellEdgeSide.Top, Bounds.Left, Bounds.Top, Bounds.Width),
                new CellEdge(CellEdgeSide.Bottom, Bounds.Left, Bounds.Bottom - 1, Bounds.Width),
                new CellEdge(CellEdgeSide.Left, Bounds.Left, Bounds.Top, Bounds.Height),
                new CellEdge(CellEdgeSide.Right, Bounds.Right - 1, Bounds.Top, Bounds.Height)
            ];
        }

        var edges = new List<CellEdge>();
        var scratch = new ulong[_stride];
        for (var row = 0; row < Bounds.Height; row++)
        {
            AddRowEdges(edges, CellEdgeSide.Top, row, row - 1, scratch);
            AddRowEdges(edges, CellEdgeSide.Bottom, row, row + 1, scratch);
        }

        AddColumnEdges(edges, CellEdgeSide.Left);
        AddColumnEdges(edges, CellEdgeSide.Right);
        return [.. edges];
    }

    // The runs of selected cells in a row whose neighbour in neighbourRow (above or below) isn't selected.
    private void AddRowEdges(List<CellEdge> edges, CellEdgeSide side, int row, int neighbourRow, ulong[] scratch)
    {
        var current = Row(row);
        var hasNeighbourRow = neighbourRow >= 0 && neighbourRow < Bounds.Height;
        var neighbour = hasNeighbourRow ? Row(neighbourRow) : default;
        for (var index = 0; index < _stride; index++)
            scratch[index] = current[index] & ~(hasNeighbourRow ? neighbour[index] : 0);

        foreach (var (start, length) in BitRow.Runs(scratch, Bounds.Width))
            edges.Add(new CellEdge(side, Bounds.Left + start, Bounds.Top + row, length));
    }

    // The selected cells whose left (or right) neighbour isn't selected, found a row at a time and joined down each
    // column: a run opens in the first row where a column has an edge and closes in the first where it hasn't.
    private void AddColumnEdges(List<CellEdge> edges, CellEdgeSide side)
    {
        var open = new ulong[_stride];
        var current = new ulong[_stride];
        var runStarts = new int[Bounds.Width];
        for (var row = 0; row <= Bounds.Height; row++)
        {
            if (row < Bounds.Height)
                FindSideEdges(Row(row), side, current);
            else
                Array.Clear(current);

            for (var index = 0; index < _stride; index++)
            {
                var closed = open[index] & ~current[index];
                while (closed != 0)
                {
                    var column = index * BitRow.BitsPerWord + BitOperations.TrailingZeroCount(closed);
                    edges.Add(new CellEdge(side, Bounds.Left + column, Bounds.Top + runStarts[column],
                        row - runStarts[column]));
                    closed &= closed - 1;
                }

                var opened = current[index] & ~open[index];
                while (opened != 0)
                {
                    var column = index * BitRow.BitsPerWord + BitOperations.TrailingZeroCount(opened);
                    runStarts[column] = row;
                    opened &= opened - 1;
                }

                open[index] = current[index];
            }
        }
    }

    // The cells of a row whose neighbour on the given side isn't selected: each cell compared with its neighbour by
    // shifting the whole row a bit, carrying bits between words.
    private void FindSideEdges(ReadOnlySpan<ulong> row, CellEdgeSide side, ulong[] edges)
    {
        for (var index = 0; index < _stride; index++)
        {
            ulong neighbours;
            if (side == CellEdgeSide.Left)
                neighbours = (row[index] << 1) | (index > 0 ? row[index - 1] >> 63 : 0);
            else
                neighbours = (row[index] >> 1) | (index + 1 < _stride ? row[index + 1] << 63 : 0);

            edges[index] = row[index] & ~neighbours;
        }
    }
}
