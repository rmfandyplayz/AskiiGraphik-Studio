// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Runtime.CompilerServices;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Removes a layer. The removed layer, with its cells, is kept so undo can put it back. A frame's
/// last layer can't be removed (ADR 0010).
/// </summary>
public sealed class RemoveLayerCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 128;

    private Layer? _removedLayer;
    private Frame? _frame;
    private int _index;

    /// <summary>Creates a command that removes the layer with <paramref name="layerId"/>.</summary>
    public RemoveLayerCommand(Guid layerId)
    {
        LayerId = layerId;
    }

    /// <summary>The layer to remove.</summary>
    public Guid LayerId { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes =>
        FixedOverheadInBytes + ((long)(_removedLayer?.Size.CellCount ?? 0) * Unsafe.SizeOf<Cell>());

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The layer is its frame's only layer.</exception>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var layer = document.GetLayer(LayerId);
        var frame = layer.Frame!;
        var index = frame.IndexOf(LayerId);

        _removedLayer = frame.RemoveLayer(LayerId);
        _frame = frame;
        _index = index;
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_removedLayer is null || _frame is null)
            throw new InvalidOperationException("The command hasn't been applied.");

        _frame.InsertLayer(_index, _removedLayer);
    }
}
