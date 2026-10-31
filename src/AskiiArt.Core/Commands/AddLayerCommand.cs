// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>Adds an empty layer to the active frame.</summary>
public sealed class AddLayerCommand : IDocumentCommand
{
    private const long EstimatedSizeInBytes = 128;

    /// <summary>Creates the command. The new layer's id is chosen now, so later commands can refer to it.</summary>
    /// <param name="index">Where the layer goes, from 0 (the bottom) up to the current layer count (the top).</param>
    /// <param name="properties">The new layer's properties, including its name.</param>
    public AddLayerCommand(int index, LayerProperties properties)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(properties);

        Index = index;
        Properties = properties;
        LayerId = Guid.NewGuid();
    }

    /// <summary>The id the new layer gets. It's the same every time the command is redone.</summary>
    public Guid LayerId { get; }

    /// <summary>Where the layer goes in the active frame.</summary>
    public int Index { get; }

    /// <summary>The new layer's properties.</summary>
    public LayerProperties Properties { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => EstimatedSizeInBytes;

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // A fresh layer each time: anything that was drawn on it was undone before this command was.
        var layer = new Layer(LayerId, Properties, document.CanvasSize);
        document.ActiveFrame.InsertLayer(Index, layer);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.ActiveFrame.RemoveLayer(LayerId);
    }
}
