// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Characters;

/// <summary>What the Characters window's buttons do to its chosen character set (ADR 0053).</summary>
internal sealed class CharacterActions(CharactersPanelViewModel characters) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var duplicate = new RelayCommand(characters.DuplicateSelected, () => characters.CanDuplicate);
        var delete = new AsyncRelayCommand(characters.DeleteSelectedAsync, () => characters.CanDelete);
        var export = new AsyncRelayCommand(characters.ExportSelectedAsync, () => characters.CanExport);

        characters.StateChanged += (_, _) =>
        {
            duplicate.NotifyCanExecuteChanged();
            delete.NotifyCanExecuteChanged();
            export.NotifyCanExecuteChanged();
        };

        yield return new AppAction(CharacterActionIds.Duplicate, nameof(Strings.ActionCharacterSetDuplicate),
            duplicate);
        yield return new AppAction(CharacterActionIds.Delete, nameof(Strings.ActionCharacterSetDelete), delete);
        yield return new AppAction(CharacterActionIds.Import, nameof(Strings.ActionCharacterSetImport),
            new AsyncRelayCommand(characters.ImportAsync));
        yield return new AppAction(CharacterActionIds.Export, nameof(Strings.ActionCharacterSetExport), export);
    }
}
