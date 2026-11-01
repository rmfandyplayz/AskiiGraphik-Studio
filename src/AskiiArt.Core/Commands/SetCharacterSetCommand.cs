// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Changes the document's character set (ADR 0016). Every cell, in every layer, holding a glyph
/// the new set doesn't allow becomes empty, and undo puts those glyphs back.
/// </summary>
public sealed class SetCharacterSetCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly List<RemovedCell> _removedCells = [];
    private CharacterSet _oldCharacterSet;

    /// <summary>Creates a command that switches the document to <paramref name="newCharacterSet"/>.</summary>
    public SetCharacterSetCommand(CharacterSet newCharacterSet)
    {
        if (!Enum.IsDefined(newCharacterSet))
            throw new ArgumentOutOfRangeException(nameof(newCharacterSet), newCharacterSet, null);

        NewCharacterSet = newCharacterSet;
    }

    /// <summary>The character set after the change.</summary>
    public CharacterSet NewCharacterSet { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes =>
        FixedOverheadInBytes + ((long)_removedCells.Count * Unsafe.SizeOf<RemovedCell>());

    /// <summary>
    /// How many cells switching <paramref name="document"/> to <paramref name="newCharacterSet"/>
    /// would empty, so the user can be warned first.
    /// </summary>
    public static int CountCellsToRemove(Document document, CharacterSet newCharacterSet)
    {
        ArgumentNullException.ThrowIfNull(document);

        var count = 0;
        foreach (var layer in document.Frames.SelectMany(frame => frame.Layers))
        {
            for (var y = 0; y < layer.Size.Height; y++)
            {
                foreach (var cell in layer.GetRow(y))
                {
                    if (cell.IsOccupied && !newCharacterSet.Allows(cell.Glyph))
                        count++;
                }
            }
        }

        return count;
    }

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldCharacterSet = document.CharacterSet;
        _removedCells.Clear();

        using (document.BeginChanges())
        {
            foreach (var layer in document.Frames.SelectMany(frame => frame.Layers))
                RemoveDisallowedGlyphs(layer);

            document.SetCharacterSet(NewCharacterSet);
        }
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using (document.BeginChanges())
        {
            // The old set first, so the document never holds a glyph its set doesn't allow.
            document.SetCharacterSet(_oldCharacterSet);
            foreach (var removed in _removedCells)
                document.GetLayer(removed.LayerId).SetCell(removed.Index, removed.Cell);
        }
    }

    private void RemoveDisallowedGlyphs(Layer layer)
    {
        for (var index = 0; index < layer.Size.CellCount; index++)
        {
            var cell = layer.GetCell(index);
            if (!cell.IsOccupied || NewCharacterSet.Allows(cell.Glyph))
                continue;

            _removedCells.Add(new RemovedCell(layer.Id, index, cell));
            layer.SetCell(index, Cell.Empty);
        }
    }

    private readonly record struct RemovedCell(Guid LayerId, int Index, Cell Cell);
}
