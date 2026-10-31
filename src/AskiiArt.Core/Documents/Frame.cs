// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;

namespace AskiiArt.Core.Documents;

/// <summary>
/// One frame of a document: its ordered layers, with index 0 at the bottom (ARCHITECTURE §5.1).
/// A v1 document has exactly one frame; the list of frames is the hook for animation (§18).
/// </summary>
/// <remarks>A frame always has at least one layer (ADR 0010).</remarks>
public sealed class Frame
{
    private readonly List<Layer> _layers = [];

    internal Frame(Document document)
    {
        Document = document;
        Layers = new ReadOnlyCollection<Layer>(_layers);
    }

    /// <summary>The document the frame belongs to.</summary>
    public Document Document { get; }

    /// <summary>The layers from bottom (index 0) to top.</summary>
    public IReadOnlyList<Layer> Layers { get; }

    /// <summary>Returns the index of the layer with <paramref name="layerId"/>, or -1 if this frame doesn't have it.</summary>
    public int IndexOf(Guid layerId) => _layers.FindIndex(layer => layer.Id == layerId);

    /// <summary>Adds a layer that isn't part of any document, at <paramref name="index"/>.</summary>
    internal void InsertLayer(int index, Layer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _layers.Count);

        if (layer.Frame is not null)
            throw new InvalidOperationException($"Layer {layer.Id} already belongs to a frame.");

        if (layer.Size != Document.CanvasSize)
            throw new ArgumentException($"The layer is {layer.Size}, but the canvas is {Document.CanvasSize}.", nameof(layer));

        if (Document.TryGetLayer(layer.Id, out _))
            throw new ArgumentException($"The document already has a layer with id {layer.Id}.", nameof(layer));

        _layers.Insert(index, layer);
        layer.Frame = this;
        Document.ChangeTracker.RecordLayerListChanged();
    }

    /// <summary>Removes the layer and returns it, detached and with its cells intact.</summary>
    /// <exception cref="InvalidOperationException">It's the frame's only layer.</exception>
    internal Layer RemoveLayer(Guid layerId)
    {
        var index = RequireIndexOf(layerId);
        if (_layers.Count == 1)
            throw new InvalidOperationException("A frame can't lose its last layer.");

        var layer = _layers[index];
        _layers.RemoveAt(index);
        layer.Frame = null;
        Document.ChangeTracker.RecordLayerListChanged();
        return layer;
    }

    /// <summary>Moves the layer so that it ends up at <paramref name="newIndex"/>.</summary>
    internal void MoveLayer(Guid layerId, int newIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, _layers.Count);

        var index = RequireIndexOf(layerId);
        if (index == newIndex)
            return;

        var layer = _layers[index];
        _layers.RemoveAt(index);
        _layers.Insert(newIndex, layer);
        Document.ChangeTracker.RecordLayerListChanged();
    }

    private int RequireIndexOf(Guid layerId)
    {
        var index = IndexOf(layerId);
        if (index < 0)
            throw new KeyNotFoundException($"This frame has no layer with id {layerId}.");

        return index;
    }
}
