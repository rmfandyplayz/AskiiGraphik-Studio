// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Collects the cells a tool wants to change on one layer, and turns them into a
/// <see cref="CellEditCommand"/>. The document isn't touched until the command is executed. A builder made with a
/// clip leaves out every cell outside it, which is how tools keep to the selection (ADR 0065).
/// </summary>
/// <remarks>
/// Each cell's "before" is read from the layer the first time the cell is set, so build and
/// execute the command before anything else changes the layer.
/// </remarks>
public sealed class CellEditBuilder
{
    private readonly Document _document;
    private readonly Layer _layer;
    private readonly Dictionary<int, CellEdit> _editsByIndex = [];
    private readonly SelectionMask? _clip;

    /// <summary>
    /// Starts collecting changes for the layer with <paramref name="layerId"/>, only inside <paramref name="clip"/>
    /// when there is one.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    public CellEditBuilder(Document document, Guid layerId, SelectionMask? clip = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        _document = document;
        _layer = document.GetLayer(layerId);
        _clip = clip;
    }

    /// <summary>The number of distinct cells set so far, including ones set to their current value.</summary>
    public int Count => _editsByIndex.Count;

    /// <summary>
    /// Sets the cell at <paramref name="point"/>. Setting the same cell again replaces the earlier value. A cell
    /// outside the clip is left as it is.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the canvas.</exception>
    /// <exception cref="ArgumentException">
    /// The document's character set doesn't allow the cell's glyph, or its colour mode doesn't allow
    /// the cell's colours.
    /// </exception>
    public void Set(CellPoint point, Cell cell)
    {
        if (!_layer.Size.Contains(point))
            throw new ArgumentOutOfRangeException(nameof(point), point, $"The point is outside the {_layer.Size} canvas.");

        if (_clip is not null && !_clip.Contains(point))
            return;

        if (cell.HasGlyph && !_document.CharacterSet.Allows(cell.Glyph))
        {
            throw new ArgumentException(
                $"The document's {_document.CharacterSet} character set doesn't allow U+{cell.Glyph.Value:X4}.", nameof(cell));
        }

        if (!_document.ColorMode.Allows(cell))
        {
            throw new ArgumentException($"A {_document.ColorMode} document can't hold the colours of {cell}.",
                nameof(cell));
        }

        var index = _layer.IndexOf(point);
        if (_editsByIndex.TryGetValue(index, out var earlier))
            _editsByIndex[index] = earlier with { After = cell };
        else
            _editsByIndex[index] = new CellEdit(index, _layer.GetCell(index), cell);
    }

    /// <summary>Returns the command, or null when no cell would actually change.</summary>
    /// <param name="stepNameKey">Names the step in the history: a key from <see cref="UndoStepNames"/>.</param>
    public CellEditCommand? Build(string stepNameKey = UndoStepNames.EditCells)
    {
        var edits = _editsByIndex.Values
            .Where(edit => edit.Before != edit.After)
            .OrderBy(edit => edit.Index)
            .ToArray();

        return edits.Length == 0 ? null : new CellEditCommand(_layer.Id, edits, stepNameKey);
    }
}
