// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Changes the document's colour mode (ADR 0036). Every colour the new mode doesn't allow, in
/// every layer, is converted: removed for monochrome, the nearest of the 16 for ANSI, and the
/// palette's value for RGB. Undo puts the old colours back.
/// </summary>
public sealed class SetColorModeCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly List<ChangedCell> _changedCells = [];
    private ColorMode _oldColorMode;

    /// <summary>Creates a command that switches the document to <paramref name="newColorMode"/>.</summary>
    public SetColorModeCommand(ColorMode newColorMode)
    {
        if (!Enum.IsDefined(newColorMode))
            throw new ArgumentOutOfRangeException(nameof(newColorMode), newColorMode, null);

        NewColorMode = newColorMode;
    }

    /// <summary>The colour mode after the change.</summary>
    public ColorMode NewColorMode { get; }

    /// <inheritdoc/>
    public string StepNameKey => UndoStepNames.SetColorMode;

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes =>
        FixedOverheadInBytes + ((long)_changedCells.Count * Unsafe.SizeOf<ChangedCell>());

    /// <summary>
    /// How many cells would look different after switching <paramref name="document"/> to
    /// <paramref name="newColorMode"/>, so the user can be warned first. Turning ANSI colours into
    /// RGB changes none.
    /// </summary>
    public static int CountCellsThatLookDifferent(Document document, ColorMode newColorMode)
    {
        ArgumentNullException.ThrowIfNull(document);

        var count = 0;
        foreach (var layer in document.Frames.SelectMany(frame => frame.Layers))
        {
            for (var y = 0; y < layer.Size.Height; y++)
            {
                foreach (var cell in layer.GetRow(y))
                {
                    if (newColorMode.ConversionChangesTheLookOf(cell.Foreground)
                        || newColorMode.ConversionChangesTheLookOf(cell.Background))
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _oldColorMode = document.ColorMode;
        _changedCells.Clear();

        using (document.BeginChanges())
        {
            foreach (var layer in document.Frames.SelectMany(frame => frame.Layers))
                ConvertColors(layer);

            document.SetColorMode(NewColorMode);
        }
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using (document.BeginChanges())
        {
            document.SetColorMode(_oldColorMode);
            foreach (var changed in _changedCells)
                document.GetLayer(changed.LayerId).SetCell(changed.Index, changed.Cell);
        }
    }

    private void ConvertColors(Layer layer)
    {
        for (var index = 0; index < layer.Size.CellCount; index++)
        {
            var cell = layer.GetCell(index);
            var converted = NewColorMode.Convert(cell);
            if (converted == cell)
                continue;

            _changedCells.Add(new ChangedCell(layer.Id, index, cell));
            layer.SetCell(index, converted);
        }
    }

    private readonly record struct ChangedCell(Guid LayerId, int Index, Cell Cell);
}
