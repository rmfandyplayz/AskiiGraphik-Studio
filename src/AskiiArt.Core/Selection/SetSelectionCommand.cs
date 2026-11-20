// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Selection;

/// <summary>
/// Makes a mask the session's selection, as an undo step that leaves the document alone, so it never makes the
/// document unsaved (ADR 0055).
/// </summary>
public sealed class SetSelectionCommand : IDocumentCommand
{
    private const long FixedOverheadInBytes = 64;

    private readonly SessionSelection _selection;
    private SelectionMask? _before;

    /// <summary>Creates a command that selects the cells of <paramref name="mask"/>.</summary>
    /// <param name="selection">The session's selection.</param>
    /// <param name="mask">The new selection.</param>
    /// <param name="stepNameKey">Names the step in the history: a key from <see cref="UndoStepNames"/>.</param>
    public SetSelectionCommand(SessionSelection selection, SelectionMask mask, string stepNameKey)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(mask);
        ArgumentException.ThrowIfNullOrEmpty(stepNameKey);

        _selection = selection;
        Mask = mask;
        StepNameKey = stepNameKey;
    }

    /// <summary>The selection the command makes.</summary>
    public SelectionMask Mask { get; }

    /// <inheritdoc/>
    public string StepNameKey { get; }

    /// <summary>
    /// The new mask only: the one it replaces is the step before's, or what the session started with, so it's
    /// counted there.
    /// </summary>
    public long EstimatedMemoryInBytes => FixedOverheadInBytes + Mask.EstimatedMemoryInBytes;

    /// <summary>False: a selection isn't part of the document.</summary>
    public bool ChangesDocument => false;

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">The mask belongs to a canvas of another size.</exception>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (Mask.CanvasSize != document.CanvasSize)
        {
            throw new ArgumentException(
                $"The selection is for a {Mask.CanvasSize} canvas, but the document's is {document.CanvasSize}.",
                nameof(document));
        }

        _before = _selection.Mask;
        _selection.Set(Mask);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        if (_before is null)
            throw new InvalidOperationException("The command can only be reverted after it's applied.");

        _selection.Set(_before);
    }
}
