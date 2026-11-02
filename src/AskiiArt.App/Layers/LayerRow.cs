// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Layers;

/// <summary>
/// One layer in the Layers panel. Ticking a checkbox asks the panel to change the layer, and the row
/// shows the result when the document reports it, so undo and the checkbox can't disagree.
/// </summary>
internal sealed class LayerRow(Guid layerId, LayersPanelViewModel panel) : ObservableObject
{
    private LayerProperties? _properties;
    private bool _isRenaming;
    private string _nameBeingEdited = string.Empty;

    public Guid LayerId { get; } = layerId;

    public string Name => _properties?.Name ?? string.Empty;

    public bool IsShown
    {
        get => _properties?.IsVisible ?? false;
        set => panel.SetLayerShown(this, value);
    }

    public bool IsLocked
    {
        get => _properties?.IsLocked ?? false;
        set => panel.SetLayerLocked(this, value);
    }

    /// <summary>True while the name is being edited in place.</summary>
    public bool IsRenaming
    {
        get => _isRenaming;
        set => SetProperty(ref _isRenaming, value);
    }

    /// <summary>The text in the name box while <see cref="IsRenaming"/> is true.</summary>
    public string NameBeingEdited
    {
        get => _nameBeingEdited;
        set => SetProperty(ref _nameBeingEdited, value);
    }

    /// <summary>Opens the name for editing, starting from the current name.</summary>
    public void BeginRename()
    {
        NameBeingEdited = Name;
        IsRenaming = true;
    }

    /// <summary>Shows the layer's current settings.</summary>
    public void Update(LayerProperties properties)
    {
        _properties = properties;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsShown));
        OnPropertyChanged(nameof(IsLocked));
    }
}
