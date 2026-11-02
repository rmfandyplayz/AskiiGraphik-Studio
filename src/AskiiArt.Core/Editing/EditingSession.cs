// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Editing;

/// <summary>
/// Editing one document: the document, its undo history, the layer tools draw on, and the clock
/// tools use to notice a pause in typing. Tools work through a session, never on the document
/// alone. Selection joins it in M4 (ARCHITECTURE §10).
/// </summary>
public sealed class EditingSession
{
    private Guid _activeLayerId;

    /// <summary>Starts editing <paramref name="document"/> with an empty history.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="clock">The time source; tests pass a fake one. Defaults to the system clock.</param>
    public EditingSession(Document document, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        Document = document;
        History = new CommandHistory(document);
        Clock = clock ?? TimeProvider.System;
        _activeLayerId = TopLayerId();
    }

    /// <summary>The document being edited.</summary>
    public Document Document { get; }

    /// <summary>The document's undo history. Every change goes through it.</summary>
    public CommandHistory History { get; }

    /// <summary>The time source for timing rules, such as when a pause ends a typing burst.</summary>
    public TimeProvider Clock { get; }

    /// <summary>Raised when <see cref="SetActiveLayer"/> chooses another layer.</summary>
    public event EventHandler? ActiveLayerChanged;

    /// <summary>
    /// The layer tools draw on. It starts as the top layer of the active frame. If the layer is
    /// removed, the top layer takes over. Which layer is active isn't part of the document, so it
    /// isn't undone and choosing one isn't a change, though a saved file remembers it (ADR 0031).
    /// </summary>
    public Layer ActiveLayer
    {
        get
        {
            if (Document.TryGetLayer(_activeLayerId, out var layer))
                return layer;

            _activeLayerId = TopLayerId();
            return Document.GetLayer(_activeLayerId);
        }
    }

    /// <summary>False when tools must leave the active layer alone: it's locked, or hidden, where a change couldn't be seen.</summary>
    public bool CanEditActiveLayer => ActiveLayer.Properties is { IsLocked: false, IsVisible: true };

    /// <summary>Makes the layer with <paramref name="layerId"/> the one tools draw on.</summary>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    public void SetActiveLayer(Guid layerId)
    {
        var layer = Document.GetLayer(layerId);
        if (layer.Id == ActiveLayer.Id)
            return;

        _activeLayerId = layerId;
        ActiveLayerChanged?.Invoke(this, EventArgs.Empty);
    }

    private Guid TopLayerId() => Document.ActiveFrame.Layers[^1].Id;
}
