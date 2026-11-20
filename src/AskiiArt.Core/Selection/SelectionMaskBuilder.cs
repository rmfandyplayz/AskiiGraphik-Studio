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
    {
        _canvasSize = canvasSize;
        _area = (area ?? canvasSize.Bounds).Intersect(canvasSize.Bounds);
        _stride = BitRow.WordsFor(_area.Width);
        _bits = new ulong[_stride * _area.Height];
    }

    /// <summary>Selects the cell at <paramref name="point"/>, if it's in the builder's area.</summary>
    public void Add(CellPoint point) => AddRun(point.Y, point.X, 1);

    /// <summary>
    /// Selects <paramref name="length"/> cells of row <paramref name="row"/> from column <paramref name="column"/>,
    /// leaving out any outside the builder's area.
    /// </summary>
    public void AddRun(int row, int column, int length)
    {
        if (row < _area.Top || row >= _area.Bottom)
            return;

        var start = Math.Max(column, _area.Left);
        var end = Math.Min(column + length, _area.Right);
        if (end <= start)
            return;

        BitRow.SetRange(_bits.AsSpan((row - _area.Top) * _stride, _stride), start - _area.Left, end - start);
    }

    /// <summary>The mask of every cell added so far.</summary>
    public SelectionMask Build() => SelectionMask.FromBits(_canvasSize, _area, [.. _bits], _stride);
}
