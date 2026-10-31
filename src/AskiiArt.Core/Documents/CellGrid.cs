// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The storage behind a layer's cells (ARCHITECTURE §5.8): one dense row-major array. Only Core
/// sees this type, so it can become tiled storage later without changing the public API.
/// </summary>
internal sealed class CellGrid
{
    private readonly Cell[] _cells;

    public CellGrid(CanvasSize size)
    {
        Size = size;
        _cells = new Cell[size.CellCount];
    }

    public CanvasSize Size { get; }

    public Cell this[int index]
    {
        get => _cells[index];
        set => _cells[index] = value;
    }

    public int IndexOf(CellPoint point) => (point.Y * Size.Width) + point.X;

    public CellPoint PointAt(int index) => new(index % Size.Width, index / Size.Width);

    public ReadOnlySpan<Cell> GetRow(int y) => _cells.AsSpan(y * Size.Width, Size.Width);

    /// <summary>Copies a region inside the grid to <paramref name="destination"/>, row by row.</summary>
    public void CopyTo(CellRect region, Span<Cell> destination)
    {
        for (var row = 0; row < region.Height; row++)
        {
            var source = _cells.AsSpan(IndexOf(new CellPoint(region.X, region.Y + row)), region.Width);
            source.CopyTo(destination.Slice(row * region.Width, region.Width));
        }
    }

    /// <summary>
    /// Returns a grid of <paramref name="newSize"/> holding this grid's cells moved by
    /// <paramref name="contentOffset"/>. Cells that land outside are dropped, and new cells are empty.
    /// </summary>
    public CellGrid Resized(CanvasSize newSize, CellPoint contentOffset)
    {
        var resized = new CellGrid(newSize);
        var keptInNew = newSize.Bounds.Intersect(Size.Bounds.Offset(contentOffset));

        for (var row = keptInNew.Top; row < keptInNew.Bottom; row++)
        {
            var sourceStart = IndexOf(new CellPoint(keptInNew.Left - contentOffset.X, row - contentOffset.Y));
            var destinationStart = resized.IndexOf(new CellPoint(keptInNew.Left, row));
            _cells.AsSpan(sourceStart, keptInNew.Width).CopyTo(resized._cells.AsSpan(destinationStart));
        }

        return resized;
    }
}
