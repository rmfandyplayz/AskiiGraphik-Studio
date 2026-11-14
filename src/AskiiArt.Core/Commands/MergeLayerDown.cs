// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Merges a layer into the one below it, which keeps its own name and settings. Both layers
/// together look the same before and after, because each cell's glyph and background are merged
/// the way compositing would have shown them (ADR 0036).
/// </summary>
/// <remarks>
/// If the lower layer's spaces are see-through and the upper layer brings spaces that hide things,
/// those would stop hiding. So the merged layer takes the upper layer's space option, and the lower
/// layer's own spaces lose their glyph, keeping any background, which looks the same as they did.
/// </remarks>
public static class MergeLayerDown
{
    /// <summary>Creates the command, as one undo step. Execute it before anything else changes the layers.</summary>
    /// <param name="document">The document holding the layer.</param>
    /// <param name="layerId">The layer to merge down.</param>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    /// <exception cref="InvalidOperationException">The layer is at the bottom, so nothing is below it.</exception>
    public static IDocumentCommand Create(Document document, Guid layerId)
    {
        ArgumentNullException.ThrowIfNull(document);

        var upper = document.GetLayer(layerId);
        var frame = upper.Frame!;
        var index = frame.IndexOf(layerId);
        if (index == 0)
            throw new InvalidOperationException("The bottom layer has nothing below it to merge into.");

        var lower = frame.Layers[index - 1];
        var upperPolicy = CompositingPolicy.For(upper.Properties.SpaceHandling);
        var lowerPolicy = CompositingPolicy.For(lower.Properties.SpaceHandling);
        var cellCount = upper.Size.CellCount;
        var changesLowerOption = false;
        for (var cellIndex = 0; cellIndex < cellCount && !changesLowerOption; cellIndex++)
        {
            var cell = upper.GetCell(cellIndex);
            changesLowerOption = upperPolicy.HidesGlyphsBelow(cell) && !lowerPolicy.HidesGlyphsBelow(cell);
        }

        var cells = new CellEditBuilder(document, lower.Id);
        for (var y = 0; y < upper.Size.Height; y++)
        {
            for (var x = 0; x < upper.Size.Width; x++)
            {
                var point = new CellPoint(x, y);
                var original = lower.GetCell(point);
                var own = original;
                if (changesLowerOption && own.HasGlyph && !lowerPolicy.HidesGlyphsBelow(own))
                    own = own.WithoutGlyph();

                var merged = Compositor.PaintOver(own, upper.GetCell(point), upperPolicy);
                if (merged != original)
                    cells.Set(point, merged);
            }
        }

        var commands = new List<IDocumentCommand>();
        if (cells.Build() is { } edit)
            commands.Add(edit);

        if (changesLowerOption)
        {
            var merged = lower.Properties with { SpaceHandling = upper.Properties.SpaceHandling };
            commands.Add(new SetLayerPropertiesCommand(lower.Id, merged, UndoStepNames.MergeLayerDown));
        }

        commands.Add(new RemoveLayerCommand(layerId));
        return new CompositeCommand(commands, UndoStepNames.MergeLayerDown);
    }
}
