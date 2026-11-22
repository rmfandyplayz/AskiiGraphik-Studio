// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Clipboard;

/// <summary>
/// Cells on their way through the clipboard (ADR 0067): a rectangle of cells, the shape within it that was copied, and
/// where on the canvas it was copied from. Copy takes one from the active layer's selection, and text from other apps
/// becomes one as well. Cells outside the shape are always empty.
/// </summary>
public sealed class CellClip
{
    private readonly Cell[] _cells;

    internal CellClip(CanvasSize size, Cell[] cells, SelectionMask shape, CellPoint? origin)
    {
        if (cells.Length != size.CellCount)
            throw new ArgumentException($"A {size} clip needs {size.CellCount} cells.", nameof(cells));

        if (shape.CanvasSize != size)
            throw new ArgumentException($"The shape isn't the clip's size, {size}.", nameof(shape));

        Size = size;
        _cells = cells;
        Shape = shape;
        Origin = origin;
    }

    /// <summary>The size of the rectangle the clip's cells fill.</summary>
    public CanvasSize Size { get; }

    /// <summary>Which of the rectangle's cells were copied: the selection's shape, or all of them for text.</summary>
    public SelectionMask Shape { get; }

    /// <summary>Where the rectangle's top-left cell was on the canvas it was copied from, or null for text.</summary>
    public CellPoint? Origin { get; }

    /// <summary>True when any of the clip's cells holds something.</summary>
    public bool HasContent => Array.Exists(_cells, cell => !cell.IsEmpty);

    /// <summary>
    /// Copies the cells of <paramref name="layer"/> in <paramref name="selection"/>, leaving out any part of it past
    /// the canvas's edges.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Nothing on the canvas is selected, or the selection isn't the layer's size.
    /// </exception>
    public static CellClip Copy(Layer layer, SelectionMask selection)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(selection);
        selection = selection.CutToCanvas();
        if (selection.IsEmpty)
            throw new ArgumentException("There's nothing on the canvas selected to copy.", nameof(selection));

        if (selection.CanvasSize != layer.Size)
            throw new ArgumentException($"The selection isn't the layer's size, {layer.Size}.", nameof(selection));

        var bounds = selection.Bounds;
        var size = new CanvasSize(bounds.Width, bounds.Height);
        var cells = new Cell[size.CellCount];
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            foreach (var (column, length) in selection.RunsInRow(y))
            {
                for (var x = column; x < column + length; x++)
                    cells[((y - bounds.Top) * size.Width) + x - bounds.Left] = layer.GetCell(new CellPoint(x, y));
            }
        }

        var topLeft = new CellPoint(bounds.Left, bounds.Top);
        return new CellClip(size, cells, selection.MovedOnto(size, new CellPoint(-topLeft.X, -topLeft.Y)), topLeft);
    }

    /// <summary>Returns the cell at <paramref name="point"/> of the clip's rectangle.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the rectangle.</exception>
    public Cell GetCell(CellPoint point)
    {
        if (!Size.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point), point, $"The point is outside the {Size} clip.");

        return _cells[(point.Y * Size.Width) + point.X];
    }

    /// <summary>
    /// The clip without what a document using <paramref name="characterSet"/> and <paramref name="colorMode"/> can't
    /// hold: a glyph its character set doesn't allow goes, leaving the cell's background, as switching the character
    /// set does, and colours are converted as switching the colour mode converts them.
    /// </summary>
    public CellClip FittedTo(CharacterSet characterSet, ColorMode colorMode)
    {
        Cell[]? fitted = null;
        for (var index = 0; index < _cells.Length; index++)
        {
            var cell = _cells[index];
            var fit = cell.HasGlyph && !characterSet.Allows(cell.Glyph) ? cell.WithoutGlyph() : cell;
            fit = colorMode.Convert(fit);
            if (fit == cell)
                continue;

            fitted ??= (Cell[])_cells.Clone();
            fitted[index] = fit;
        }

        return fitted is null ? this : new CellClip(Size, fitted, Shape, Origin);
    }

    /// <summary>
    /// The clip as plain text for other apps, as Copy as Text writes the canvas (ADR 0022): a line per row, each the
    /// rectangle's full width, with cells that hold no glyph as spaces, and <paramref name="lineBreak"/> between rows
    /// but not after the last.
    /// </summary>
    public string ToText(string lineBreak)
    {
        ArgumentException.ThrowIfNullOrEmpty(lineBreak);

        var text = new StringBuilder((Size.Width + lineBreak.Length) * Size.Height);
        for (var y = 0; y < Size.Height; y++)
        {
            if (y > 0)
                text.Append(lineBreak);

            PlainTextRows.Append(text, _cells.AsSpan(y * Size.Width, Size.Width));
        }

        return text.ToString();
    }

    // The cells in row order, for writing the clip in the app's own clipboard format.
    internal ReadOnlySpan<Cell> Cells => _cells;
}
