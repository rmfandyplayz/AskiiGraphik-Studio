// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Moving;

/// <summary>
/// The cells of a move at one place (ADR 0066): each moving cell is emptied where it was, unless the move is a copy,
/// and written where it lands, replacing what was there. It works on plain arrays rather than a
/// <see cref="CellEditBuilder"/>, and reports the cells it changes together as the rect that holds them, because a
/// move of a whole 500 × 500 layer makes a new one every time the pointer reaches another cell.
/// </summary>
internal sealed class MoveCellsCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly Guid _layerId;
    private readonly MovingCell[] _cells;
    private readonly int[] _landings;
    private readonly bool _isCopy;
    private readonly Cell[] _covered;
    private readonly string _stepNameKey;

    /// <param name="layerId">The layer the cells are on.</param>
    /// <param name="cells">The cells that move, with something in each, where they were. They aren't changed.</param>
    /// <param name="landings">Where each cell lands, as a cell index, or -1 when it lands off the canvas.</param>
    /// <param name="isCopy">True to leave the cells where they were as well, and for cells that come from no cell.</param>
    /// <param name="stepNameKey">The step's name: a Move, or a Paste.</param>
    public MoveCellsCommand(Guid layerId, MovingCell[] cells, int[] landings, bool isCopy, string stepNameKey)
    {
        _layerId = layerId;
        _cells = cells;
        _landings = landings;
        _isCopy = isCopy;
        _covered = new Cell[cells.Length];
        _stepNameKey = stepNameKey;
    }

    /// <inheritdoc/>
    public string StepNameKey => _stepNameKey;

    /// <summary>The cells, where they land, and what they cover there.</summary>
    public long EstimatedMemoryInBytes =>
        FixedOverheadInBytes + (long)_cells.Length * (Unsafe.SizeOf<MovingCell>() + sizeof(int) + Unsafe.SizeOf<Cell>());

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var layer = document.GetLayer(_layerId);
        var changed = new ChangedCells(layer.Size.Width);
        if (!_isCopy)
        {
            foreach (var cell in _cells)
                changed.Set(layer, cell.Index, Cell.Empty);
        }

        for (var index = 0; index < _cells.Length; index++)
        {
            var landing = _landings[index];
            if (landing < 0)
                continue;

            _covered[index] = layer.GetCell(landing);
            changed.Set(layer, landing, _cells[index].Cell);
        }

        changed.Report(layer);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // What each cell covered is put back first, since a cell can land where another one was.
        var layer = document.GetLayer(_layerId);
        var changed = new ChangedCells(layer.Size.Width);
        for (var index = 0; index < _cells.Length; index++)
        {
            var landing = _landings[index];
            if (landing >= 0)
                changed.Set(layer, landing, _covered[index]);
        }

        if (!_isCopy)
        {
            foreach (var cell in _cells)
                changed.Set(layer, cell.Index, cell.Cell);
        }

        changed.Report(layer);
    }

    // Sets cells without reporting each, and keeps the rect that holds the ones that changed, to report once.
    private struct ChangedCells(int width)
    {
        private int _left = int.MaxValue;
        private int _top = int.MaxValue;
        private int _right = int.MinValue;
        private int _bottom = int.MinValue;

        public void Set(Layer layer, int index, Cell cell)
        {
            if (!layer.SetCellUnreported(index, cell))
                return;

            var (y, x) = Math.DivRem(index, width);
            _left = Math.Min(_left, x);
            _top = Math.Min(_top, y);
            _right = Math.Max(_right, x + 1);
            _bottom = Math.Max(_bottom, y + 1);
        }

        public readonly void Report(Layer layer)
        {
            if (_left <= _right)
                layer.ReportChangedCells(CellRect.FromEdges(_left, _top, _right, _bottom));
        }
    }
}

/// <summary>
/// A cell that moves, its index on the layer where it was, or <see cref="NoIndex"/> for a pasted cell, and where it
/// was, or where a pasted one lands at first.
/// </summary>
internal readonly record struct MovingCell(int Index, CellPoint Point, Cell Cell)
{
    /// <summary>The index of a pasted cell, which comes from no cell of the layer.</summary>
    public const int NoIndex = -1;
}
