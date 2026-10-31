// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Resizes the canvas around an anchor (ARCHITECTURE §5.7, ADR 0011). It stores the occupied
/// cells that get cropped, so undo restores them.
/// </summary>
public sealed class ResizeCanvasCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;
    private const long PerLayerOverheadInBytes = 64;

    private readonly List<CroppedLayerCells> _croppedCells = [];
    private CanvasSize _oldSize;
    private CellPoint _contentOffset;

    /// <summary>Creates a command that resizes the canvas to <paramref name="newSize"/>, keeping <paramref name="anchor"/> in place.</summary>
    public ResizeCanvasCommand(CanvasSize newSize, ResizeAnchor anchor)
    {
        if (!Enum.IsDefined(anchor))
            throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null);

        NewSize = newSize;
        Anchor = anchor;
    }

    /// <summary>The size after the resize.</summary>
    public CanvasSize NewSize { get; }

    /// <summary>The edge, corner or center that stays put.</summary>
    public ResizeAnchor Anchor { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes =>
        FixedOverheadInBytes
        + _croppedCells.Sum(layer => PerLayerOverheadInBytes + ((long)layer.Cells.Length * Unsafe.SizeOf<CroppedCell>()));

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var plan = CanvasResizePlan.Create(document.CanvasSize, NewSize, Anchor);
        _oldSize = plan.OldSize;
        _contentOffset = plan.ContentOffset;

        _croppedCells.Clear();
        foreach (var layer in document.Frames.SelectMany(frame => frame.Layers))
        {
            var cells = CollectOccupiedCells(layer, plan.CroppedRegions);
            if (cells.Length > 0)
                _croppedCells.Add(new CroppedLayerCells(layer.Id, cells));
        }

        document.ResizeCanvas(NewSize, _contentOffset);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.ResizeCanvas(_oldSize, _contentOffset.Negate());
        foreach (var cropped in _croppedCells)
        {
            var layer = document.GetLayer(cropped.LayerId);
            foreach (var cell in cropped.Cells)
                layer.SetCell(cell.Index, cell.Cell);
        }
    }

    private static CroppedCell[] CollectOccupiedCells(Layer layer, IReadOnlyList<CellRect> regions)
    {
        var cells = new List<CroppedCell>();
        foreach (var region in regions)
        {
            for (var y = region.Top; y < region.Bottom; y++)
            {
                for (var x = region.Left; x < region.Right; x++)
                {
                    var index = layer.IndexOf(new CellPoint(x, y));
                    var cell = layer.GetCell(index);
                    if (cell.IsOccupied)
                        cells.Add(new CroppedCell(index, cell));
                }
            }
        }

        return [.. cells];
    }

    /// <param name="Index">The cell's index in the old canvas.</param>
    /// <param name="Cell">The cell's content.</param>
    private readonly record struct CroppedCell(int Index, Cell Cell);

    private sealed record CroppedLayerCells(Guid LayerId, CroppedCell[] Cells);
}
