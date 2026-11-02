// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Merges a layer into the one below it, which keeps its own name and settings. Both layers
/// together look the same before and after, because a cell is copied down exactly when compositing
/// would have shown it over the lower layer.
/// </summary>
public static class MergeLayerDown
{
    /// <summary>Creates the command, as one undo step. Execute it before anything else changes the layers.</summary>
    /// <param name="document">The document holding the layer.</param>
    /// <param name="layerId">The layer to merge down.</param>
    /// <param name="policy">Decides which of the layer's cells hide what's below them.</param>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    /// <exception cref="InvalidOperationException">The layer is at the bottom, so nothing is below it.</exception>
    public static IDocumentCommand Create(Document document, Guid layerId, CompositingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(policy);

        var upper = document.GetLayer(layerId);
        var frame = upper.Frame!;
        var index = frame.IndexOf(layerId);
        if (index == 0)
            throw new InvalidOperationException("The bottom layer has nothing below it to merge into.");

        var cells = new CellEditBuilder(document, frame.Layers[index - 1].Id);
        for (var y = 0; y < upper.Size.Height; y++)
        {
            for (var x = 0; x < upper.Size.Width; x++)
            {
                var point = new CellPoint(x, y);
                var cell = upper.GetCell(point);
                if (policy.HidesLayersBelow(cell))
                    cells.Set(point, cell);
            }
        }

        var remove = new RemoveLayerCommand(layerId);
        IDocumentCommand[] commands = cells.Build() is { } edit ? [edit, remove] : [remove];
        return new CompositeCommand(commands, UndoStepNames.MergeLayerDown);
    }
}
