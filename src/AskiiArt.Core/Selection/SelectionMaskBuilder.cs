// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// Collects cells, or runs of cells along a row, into a <see cref="SelectionMask"/>. Cells outside the builder's area
/// are left out, so a shape can be added whole and come out cut to the canvas.
/// </summary>
public sealed class SelectionMaskBuilder
{
    private readonly CanvasSize _canvasSize;
    private readonly CellRect _area;
    private readonly int _stride;
    private readonly ulong[] _bits;

    /// <summary>Starts an empty mask for a canvas of <paramref name="canvasSize"/>.</summary>
    /// <param name="canvasSize">The size of the canvas the mask belongs to.</param>
    /// <param name="area">
    /// Where cells may be added, which bounds the memory the builder takes. Defaults to the whole canvas.
    /// </param>
    public SelectionMaskBuilder(CanvasSize canvasSize, CellRect? area = null)
        : this(canvasSize, area ?? canvasSize.Bounds, cutsToCanvas: true)
    {
    }

    private SelectionMaskBuilder(CanvasSize canvasSize, CellRect area, bool cutsToCanvas)
    {
        _canvasSize = canvasSize;
        _area = cutsToCanvas ? area.Intersect(canvasSize.Bounds) : area;
        _stride = BitRow.WordsFor(_area.Width);
        _bits = new ulong[_stride * _area.Height];
    }

    /// <summary>
    /// Starts an empty mask whose cells may lie anywhere in <paramref name="area"/>, also past the canvas's edges, as a
    /// moved selection's can (ADR 0071).
    /// </summary>
    internal static SelectionMaskBuilder Within(CanvasSize canvasSize, CellRect area) =>
        new(canvasSize, area, cutsToCanvas: false);

    /// <summary>Selects the cell at <paramref name="point"/>, if it's in the builder's area.</summary>
    public void Add(CellPoint point) => AddRun(point.Y, point.X, 1);

    /// <summary>
    /// Selects <paramref name="length"/> cells of row <paramref name="row"/> from column <paramref name="column"/>,
    /// leaving out any outside the builder's area.
    /// </summary>
    public void AddRun(int row, int column, int length) => ChangeRun(row, column, length, add: true);

    /// <summary>
    /// Leaves out <paramref name="length"/> cells of row <paramref name="row"/> from column <paramref name="column"/>,
    /// whether or not they were added.
    /// </summary>
    public void RemoveRun(int row, int column, int length) => ChangeRun(row, column, length, add: false);

    /// <summary>Keeps only the cells <paramref name="other"/> has too. Both must cover the same area.</summary>
    /// <exception cref="ArgumentException">The builders cover different areas.</exception>
    public void IntersectWith(SelectionMaskBuilder other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other._area != _area || other._canvasSize != _canvasSize)
            throw new ArgumentException("The builders cover different areas.", nameof(other));

        for (var index = 0; index < _bits.Length; index++)
            _bits[index] &= other._bits[index];
    }

    /// <summary>The mask of every cell added so far.</summary>
    public SelectionMask Build() => SelectionMask.FromBits(_canvasSize, _area, [.. _bits], _stride);

    private void ChangeRun(int row, int column, int length, bool add)
    {
        if (row < _area.Top || row >= _area.Bottom)
            return;

        var start = Math.Max(column, _area.Left);
        var end = Math.Min(column + length, _area.Right);
        if (end <= start)
            return;

        var bits = _bits.AsSpan((row - _area.Top) * _stride, _stride);
        if (add)
            BitRow.SetRange(bits, start - _area.Left, end - start);
        else
            BitRow.ClearRange(bits, start - _area.Left, end - start);
    }
}
