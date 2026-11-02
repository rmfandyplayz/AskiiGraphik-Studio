// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>Copies a layer, with its cells and settings, to just above itself under another name.</summary>
public sealed class DuplicateLayerCommand : IDocumentCommand
{
    private const long EstimatedSizeInBytes = 128;

    /// <summary>Creates the command. The copy's id is chosen now, so later commands can refer to it.</summary>
    /// <param name="sourceLayerId">The layer to copy.</param>
    /// <param name="newName">The copy's name.</param>
    public DuplicateLayerCommand(Guid sourceLayerId, string newName)
    {
        ArgumentNullException.ThrowIfNull(newName);

        SourceLayerId = sourceLayerId;
        NewName = newName;
        LayerId = Guid.NewGuid();
    }

    /// <summary>The layer to copy.</summary>
    public Guid SourceLayerId { get; }

    /// <summary>The copy's name.</summary>
    public string NewName { get; }

    /// <summary>The id the copy gets. It's the same every time the command is redone.</summary>
    public Guid LayerId { get; }

    /// <inheritdoc/>
    public string StepNameKey => UndoStepNames.DuplicateLayer;

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => EstimatedSizeInBytes;

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Copied afresh each time: undo order means the source is just as it was the first time.
        var source = document.GetLayer(SourceLayerId);
        var frame = source.Frame!;
        var copy = source.Duplicate(LayerId, source.Properties with { Name = NewName });
        frame.InsertLayer(frame.IndexOf(SourceLayerId) + 1, copy);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.GetLayer(LayerId).Frame!.RemoveLayer(LayerId);
    }
}
