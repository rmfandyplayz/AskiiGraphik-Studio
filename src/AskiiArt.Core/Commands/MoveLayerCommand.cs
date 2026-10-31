// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>Moves a layer up or down in its frame.</summary>
public sealed class MoveLayerCommand : IDocumentCommand
{
    private const long EstimatedSizeInBytes = 64;

    private int _oldIndex;

    /// <summary>Creates a command that moves the layer so it ends up at <paramref name="newIndex"/> (0 is the bottom).</summary>
    public MoveLayerCommand(Guid layerId, int newIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);

        LayerId = layerId;
        NewIndex = newIndex;
    }

    /// <summary>The layer to move.</summary>
    public Guid LayerId { get; }

    /// <summary>The layer's index after the move.</summary>
    public int NewIndex { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => EstimatedSizeInBytes;

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        var frame = FrameOf(document);
        var oldIndex = frame.IndexOf(LayerId);

        frame.MoveLayer(LayerId, NewIndex);
        _oldIndex = oldIndex;
    }

    /// <inheritdoc/>
    public void Revert(Document document) => FrameOf(document).MoveLayer(LayerId, _oldIndex);

    private Frame FrameOf(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.GetLayer(LayerId).Frame!;
    }
}
