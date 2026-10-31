// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// A grid of cells the size of the canvas, plus its <see cref="Properties"/> (ARCHITECTURE §5.5).
/// Commands refer to a layer by its <see cref="Id"/>, never by its index.
/// </summary>
/// <remarks>
/// Reading is public. Changing cells or properties is internal to Core, because every change to
/// a document goes through a command (ARCHITECTURE §2, principle 3; ADR 0010).
/// </remarks>
public sealed class Layer
{
    private CellGrid _cells;

    internal Layer(Guid id, LayerProperties properties, CanvasSize size)
    {
        ArgumentNullException.ThrowIfNull(properties);

        Id = id;
        Properties = properties;
        _cells = new CellGrid(size);
    }

    /// <summary>The layer's stable id. It never changes, and it's unique within a document.</summary>
    public Guid Id { get; }

    /// <summary>The layer's name, visibility, lock and reserved settings.</summary>
    public LayerProperties Properties { get; private set; }

    /// <summary>The size of the layer's grid. Always equal to its document's canvas size.</summary>
    public CanvasSize Size => _cells.Size;

    /// <summary>The frame the layer belongs to, or null while it isn't part of a document.</summary>
    internal Frame? Frame { get; set; }

    /// <summary>Returns the cell at <paramref name="point"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the canvas.</exception>
    public Cell GetCell(CellPoint point)
    {
        ThrowIfOutside(point);
        return _cells[_cells.IndexOf(point)];
    }

    /// <summary>
    /// Copies the cells of <paramref name="region"/> into <paramref name="destination"/>, row by
    /// row, so the cell at (x, y) lands at index <c>(y - region.Y) * region.Width + (x - region.X)</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The region isn't inside the canvas.</exception>
    /// <exception cref="ArgumentException">The destination is shorter than the region's area.</exception>
    public void CopyCells(CellRect region, Span<Cell> destination)
    {
        if (!Size.Bounds.Contains(region))
            throw new ArgumentOutOfRangeException(nameof(region), region, $"The region isn't inside the {Size} canvas.");

        if (destination.Length < region.Area)
            throw new ArgumentException("The destination is shorter than the region's area.", nameof(destination));

        _cells.CopyTo(region, destination);
    }

    internal int IndexOf(CellPoint point) => _cells.IndexOf(point);

    internal CellPoint PointAt(int index) => _cells.PointAt(index);

    internal Cell GetCell(int index) => _cells[index];

    internal ReadOnlySpan<Cell> GetRow(int y) => _cells.GetRow(y);

    /// <summary>Stores <paramref name="cell"/> and reports the change, unless the cell already holds it.</summary>
    internal void SetCell(CellPoint point, Cell cell)
    {
        ThrowIfOutside(point);
        SetCell(_cells.IndexOf(point), cell);
    }

    /// <inheritdoc cref="SetCell(CellPoint, Cell)"/>
    internal void SetCell(int index, Cell cell)
    {
        if (_cells[index] == cell)
            return;

        _cells[index] = cell;
        Frame?.Document.ChangeTracker.RecordCells(Id, CellRect.FromPoint(_cells.PointAt(index)));
    }

    internal void SetProperties(LayerProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (properties == Properties)
            return;

        Properties = properties;
        Frame?.Document.ChangeTracker.RecordPropertiesChanged(Id);
    }

    /// <summary>Resizes the grid without reporting a change; the document reports the resize as a whole.</summary>
    internal void Resize(CanvasSize newSize, CellPoint contentOffset)
    {
        _cells = _cells.Resized(newSize, contentOffset);
    }

    private void ThrowIfOutside(CellPoint point)
    {
        if (!Size.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point), point, $"The point is outside the {Size} canvas.");
    }
}
