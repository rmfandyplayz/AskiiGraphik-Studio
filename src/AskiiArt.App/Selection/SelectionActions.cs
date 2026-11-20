// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Selection;

/// <summary>
/// Select All and Deselect, for the active document's selection (ADR 0055). Each is an undo step, and Deselect is
/// available only while something is selected.
/// </summary>
internal sealed class SelectionActions(ActiveDocument activeDocument) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var selectAll = new RelayCommand(() => SelectionEdits.SelectAll(Session));
        var deselect = new RelayCommand(() => SelectionEdits.Deselect(Session), () => !Session.Selection.IsEmpty);

        // New brings a new selection, so Deselect follows whichever is current.
        var followedSelection = Session.Selection;
        followedSelection.Changed += OnSelectionChanged;
        activeDocument.SessionChanged += (_, _) =>
        {
            followedSelection.Changed -= OnSelectionChanged;
            followedSelection = Session.Selection;
            followedSelection.Changed += OnSelectionChanged;
            deselect.NotifyCanExecuteChanged();
        };

        yield return new AppAction(SelectionActionIds.SelectAll, nameof(Strings.ActionSelectAll), selectAll,
            [new ActionShortcut(Key.A, ShortcutModifiers.Command)]);
        yield return new AppAction(SelectionActionIds.Deselect, nameof(Strings.ActionDeselect), deselect,
            [new ActionShortcut(Key.D, ShortcutModifiers.Command)]);

        void OnSelectionChanged(object? sender, SessionSelectionChangedEventArgs e) =>
            deselect.NotifyCanExecuteChanged();
    }

    private EditingSession Session => activeDocument.Session;
}
