// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Layers;

/// <summary>What can be done to the active layer. The rows' own checkboxes cover showing and locking.</summary>
internal sealed class LayerActions(LayersPanelViewModel layers) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var delete = new RelayCommand(layers.DeleteActiveLayer, () => layers.CanDeleteActiveLayer);
        var moveUp = new RelayCommand(layers.MoveActiveLayerUp, () => layers.CanMoveActiveLayerUp);
        var moveDown = new RelayCommand(layers.MoveActiveLayerDown, () => layers.CanMoveActiveLayerDown);
        var mergeDown = new RelayCommand(layers.MergeActiveLayerDown, () => layers.CanMergeActiveLayerDown);

        layers.StateChanged += (_, _) =>
        {
            delete.NotifyCanExecuteChanged();
            moveUp.NotifyCanExecuteChanged();
            moveDown.NotifyCanExecuteChanged();
            mergeDown.NotifyCanExecuteChanged();
        };

        yield return new AppAction(LayerActionIds.New, nameof(Strings.ActionLayerNew), new RelayCommand(layers.AddLayer));
        yield return new AppAction(LayerActionIds.Duplicate, nameof(Strings.ActionLayerDuplicate), new RelayCommand(layers.DuplicateActiveLayer));
        yield return new AppAction(LayerActionIds.Delete, nameof(Strings.ActionLayerDelete), delete);
        yield return new AppAction(LayerActionIds.Rename, nameof(Strings.ActionLayerRename), new RelayCommand(layers.RenameActiveLayer));
        yield return new AppAction(LayerActionIds.MoveUp, nameof(Strings.ActionLayerMoveUp), moveUp);
        yield return new AppAction(LayerActionIds.MoveDown, nameof(Strings.ActionLayerMoveDown), moveDown);
        yield return new AppAction(LayerActionIds.MergeDown, nameof(Strings.ActionLayerMergeDown), mergeDown);
    }
}
