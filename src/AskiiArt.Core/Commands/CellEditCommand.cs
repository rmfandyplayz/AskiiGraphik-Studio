// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Changes cells of one layer, storing each changed cell's before and after (ARCHITECTURE §9).
/// Build one with <see cref="CellEditBuilder"/>.
/// </summary>
/// <remarks>
/// The command doesn't check the layer's lock: tools respect the lock (§8.1), and undo must
/// always be able to restore cells.
/// </remarks>
public sealed class CellEditCommand : IMergeableCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly CellEdit[] _edits;

    internal CellEditCommand(Guid layerId, CellEdit[] edits)
    {
        LayerId = layerId;
        _edits = edits;
    }

    /// <summary>The layer whose cells change.</summary>
    public Guid LayerId { get; }

    /// <summary>The changes, ordered by cell index, at most one per cell.</summary>
    public IReadOnlyList<CellEdit> Edits => _edits;

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => FixedOverheadInBytes + ((long)_edits.Length * Unsafe.SizeOf<CellEdit>());

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        var layer = RequireLayer(document);
        foreach (var edit in _edits)
            layer.SetCell(edit.Index, edit.After);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        var layer = RequireLayer(document);
        foreach (var edit in _edits)
            layer.SetCell(edit.Index, edit.Before);
    }

    /// <summary>
    /// Merges with a later cell edit of the same layer. Each cell keeps its first "before" and its
    /// last "after"; cells that end up unchanged are dropped, so the merged command may be empty.
    /// </summary>
    public bool TryMergeWith(IDocumentCommand following, [NotNullWhen(true)] out IDocumentCommand? merged)
    {
        if (following is not CellEditCommand followingEdit || followingEdit.LayerId != LayerId)
        {
            merged = null;
            return false;
        }

        var editsByIndex = _edits.ToDictionary(edit => edit.Index);
        foreach (var edit in followingEdit._edits)
        {
            if (editsByIndex.TryGetValue(edit.Index, out var earlier))
                editsByIndex[edit.Index] = earlier with { After = edit.After };
            else
                editsByIndex[edit.Index] = edit;
        }

        var combined = editsByIndex.Values
            .Where(edit => edit.Before != edit.After)
            .OrderBy(edit => edit.Index)
            .ToArray();
        merged = new CellEditCommand(LayerId, combined);
        return true;
    }

    private Layer RequireLayer(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var layer = document.GetLayer(LayerId);
        if (_edits.Length > 0 && _edits[^1].Index >= layer.Size.CellCount)
            throw new InvalidOperationException($"The edits don't fit the layer's {layer.Size} grid.");

        return layer;
    }
}
