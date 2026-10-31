// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace AskiiArt.Core.Documents;

/// <summary>
/// A text-art document: a canvas size and its frames of layers (ARCHITECTURE §5.1).
/// </summary>
/// <remarks>
/// <para>
/// Everything that reads the document works on <see cref="ActiveFrame"/> and never on
/// <c>Frames[0]</c>; that one indirection is the hook for animation (§18).
/// </para>
/// <para>
/// Reading is public, and changing is internal to Core: every change goes through a command
/// (ADR 0010). Each change raises <see cref="Changed"/> with the affected regions (§5.9). The
/// document belongs to the UI thread and isn't thread-safe (§20).
/// </para>
/// </remarks>
public sealed class Document
{
    private readonly List<Frame> _frames = [];

    private Document(CanvasSize canvasSize)
    {
        CanvasSize = canvasSize;
        Frames = new ReadOnlyCollection<Frame>(_frames);
        ChangeTracker = new DocumentChangeTracker(this);
    }

    /// <summary>
    /// Raised after the document changes, with the regions that changed. Changes made together
    /// (for example by one command) raise a single event.
    /// </summary>
    public event EventHandler<DocumentChangedEventArgs>? Changed;

    /// <summary>The canvas size, shared by every layer of every frame.</summary>
    public CanvasSize CanvasSize { get; private set; }

    /// <summary>The frames. A v1 document always has exactly one.</summary>
    public IReadOnlyList<Frame> Frames { get; }

    /// <summary>The frame that tools, commands, the renderer and exporters work on.</summary>
    public Frame ActiveFrame => _frames[0];

    internal DocumentChangeTracker ChangeTracker { get; }

    /// <summary>Creates a document with one frame holding one empty, visible layer.</summary>
    /// <param name="canvasSize">The size of the canvas.</param>
    /// <param name="firstLayerName">The first layer's name. Core has no UI text, so the caller supplies it.</param>
    public static Document Create(CanvasSize canvasSize, string firstLayerName)
    {
        ArgumentNullException.ThrowIfNull(firstLayerName);

        var document = new Document(canvasSize);
        var frame = new Frame(document);
        document._frames.Add(frame);
        frame.InsertLayer(0, new Layer(Guid.NewGuid(), new LayerProperties(firstLayerName), canvasSize));
        return document;
    }

    /// <summary>Returns the layer with <paramref name="layerId"/>, in any frame.</summary>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    public Layer GetLayer(Guid layerId)
    {
        if (TryGetLayer(layerId, out var layer))
            return layer;

        throw new KeyNotFoundException($"The document has no layer with id {layerId}.");
    }

    /// <summary>Finds the layer with <paramref name="layerId"/>, in any frame.</summary>
    public bool TryGetLayer(Guid layerId, [NotNullWhen(true)] out Layer? layer)
    {
        foreach (var frame in _frames)
        {
            var index = frame.IndexOf(layerId);
            if (index >= 0)
            {
                layer = frame.Layers[index];
                return true;
            }
        }

        layer = null;
        return false;
    }

    /// <summary>
    /// Starts a group of changes that raises a single <see cref="Changed"/> event when the
    /// returned scope is disposed. Groups can nest; the event is raised when the outermost ends.
    /// </summary>
    internal DocumentChangeScope BeginChanges()
    {
        ChangeTracker.Begin();
        return new DocumentChangeScope(ChangeTracker);
    }

    /// <summary>
    /// Resizes every layer of every frame. Each cell moves by <paramref name="contentOffset"/>;
    /// cells that land outside the new canvas are dropped, and new cells are empty.
    /// </summary>
    /// <remarks>Use <see cref="CanvasResizePlan"/> to get the offset for an anchor.</remarks>
    internal void ResizeCanvas(CanvasSize newSize, CellPoint contentOffset)
    {
        var oldSize = CanvasSize;
        foreach (var frame in _frames)
        {
            foreach (var layer in frame.Layers)
                layer.Resize(newSize, contentOffset);
        }

        CanvasSize = newSize;
        ChangeTracker.RecordCanvasResized(oldSize, newSize, contentOffset);
    }

    internal void RaiseChanged(DocumentChangedEventArgs change) => Changed?.Invoke(this, change);
}
