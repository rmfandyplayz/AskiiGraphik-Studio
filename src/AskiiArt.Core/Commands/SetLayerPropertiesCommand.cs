// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// Changes a layer's properties: rename, show or hide, lock or unlock. Make the new properties
/// with <c>layer.Properties with { ... }</c>.
/// </summary>
public sealed class SetLayerPropertiesCommand : IDocumentCommand
{
    private const long EstimatedSizeInBytes = 128;

    private LayerProperties? _oldProperties;

    /// <summary>Creates a command that gives the layer with <paramref name="layerId"/> the <paramref name="newProperties"/>.</summary>
    public SetLayerPropertiesCommand(Guid layerId, LayerProperties newProperties)
    {
        ArgumentNullException.ThrowIfNull(newProperties);

        LayerId = layerId;
        NewProperties = newProperties;
    }

    /// <summary>The layer to change.</summary>
    public Guid LayerId { get; }

    /// <summary>The properties after the change.</summary>
    public LayerProperties NewProperties { get; }

    /// <inheritdoc/>
    public long EstimatedMemoryInBytes => EstimatedSizeInBytes;

    /// <inheritdoc/>
    public void Apply(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var layer = document.GetLayer(LayerId);
        _oldProperties = layer.Properties;
        layer.SetProperties(NewProperties);
    }

    /// <inheritdoc/>
    public void Revert(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (_oldProperties is null)
            throw new InvalidOperationException("The command hasn't been applied.");

        document.GetLayer(LayerId).SetProperties(_oldProperties);
    }
}
