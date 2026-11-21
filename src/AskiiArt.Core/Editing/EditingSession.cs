// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Editing;

/// <summary>
/// Editing one document: the document, its undo history, the layer tools draw on, the selection, and
/// the clock tools use to notice a pause in typing. Tools work through a session, never on the
/// document alone.
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
        Selection = new SessionSelection(document.CanvasSize);
        _activeLayerId = TopLayerId();
    }

    /// <summary>The document being edited.</summary>
    public Document Document { get; }

    /// <summary>The document's undo history. Every change goes through it.</summary>
    public CommandHistory History { get; }

    /// <summary>The time source for timing rules, such as when a pause ends a typing burst.</summary>
    public TimeProvider Clock { get; }

    /// <summary>
    /// The selected cells (ARCHITECTURE §10). Like the active layer, it belongs to the session rather than the
    /// document and isn't saved, but changing it is an undo step (ADR 0055).
    /// </summary>
    public SessionSelection Selection { get; }

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

    /// <summary>
    /// Starts collecting a tool's changes to the layer with <paramref name="layerId"/>, kept to the selected cells
    /// while anything is selected (ARCHITECTURE §10, ADR 0065).
    /// </summary>
    /// <exception cref="KeyNotFoundException">The document has no such layer.</exception>
    public CellEditBuilder EditCellsInSelection(Guid layerId) =>
        new(Document, layerId, Selection.IsEmpty ? null : Selection.Mask);

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

    /// <summary>
    /// Resizes the canvas to <paramref name="newSize"/>, keeping <paramref name="anchor"/> in place, as one undo
    /// step. The selection moves with the art, and loses any cells that are cropped (ADR 0055).
    /// </summary>
    public void ResizeCanvas(CanvasSize newSize, ResizeAnchor anchor)
    {
        var plan = CanvasResizePlan.Create(Document.CanvasSize, newSize, anchor);
        History.Execute(new CompositeCommand(
        [
            new ResizeCanvasCommand(newSize, anchor),
            new SetSelectionCommand(Selection, Selection.Mask.Resized(plan), UndoStepNames.ResizeCanvas)
        ]));
    }

    private Guid TopLayerId() => Document.ActiveFrame.Layers[^1].Id;
}
