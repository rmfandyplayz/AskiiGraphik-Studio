// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Collects the cells a tool wants to change on one layer, and turns them into a
/// <see cref="CellEditCommand"/>. The document isn't touched until the command is executed.
/// </summary>
/// <remarks>
/// Each cell's "before" is read from the layer the first time the cell is set, so build and
/// execute the command before anything else changes the layer.
/// </remarks>
public sealed class CellEditBuilder
{
    private readonly Layer _layer;
    private readonly Dictionary<int, CellEdit> _editsByIndex = [];

    /// <summary>Starts collecting changes for the layer with <paramref name="layerId"/>.</summary>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    public CellEditBuilder(Document document, Guid layerId)
    {
        ArgumentNullException.ThrowIfNull(document);

        _layer = document.GetLayer(layerId);
    }

    /// <summary>The number of distinct cells set so far, including ones set to their current value.</summary>
    public int Count => _editsByIndex.Count;

    /// <summary>Sets the cell at <paramref name="point"/>. Setting the same cell again replaces the earlier value.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the canvas.</exception>
    public void Set(CellPoint point, Cell cell)
    {
        if (!_layer.Size.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point), point, $"The point is outside the {_layer.Size} canvas.");

        var index = _layer.IndexOf(point);
        if (_editsByIndex.TryGetValue(index, out var earlier))
            _editsByIndex[index] = earlier with { After = cell };
        else
            _editsByIndex[index] = new CellEdit(index, _layer.GetCell(index), cell);
    }

    /// <summary>Returns the command, or null when no cell would actually change.</summary>
    public CellEditCommand? Build()
    {
        var edits = _editsByIndex.Values
            .Where(edit => edit.Before != edit.After)
            .OrderBy(edit => edit.Index)
            .ToArray();

        return edits.Length == 0 ? null : new CellEditCommand(_layer.Id, edits);
    }
}
