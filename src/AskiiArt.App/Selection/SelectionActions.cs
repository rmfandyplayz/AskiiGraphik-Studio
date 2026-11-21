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
/// The Select menu's commands, for the active document's selection (ADR 0055, ADR 0063): Select All, Deselect, Invert,
/// Grow, Shrink, and selecting the cells with glyphs or with a background colour. Each is an undo step. Deselect, Grow
/// and Shrink are available only while something is selected.
/// </summary>
internal sealed class SelectionActions(ActiveDocument activeDocument) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var selectAll = new RelayCommand(() => SelectionEdits.SelectAll(Session));
        var deselect = new RelayCommand(() => SelectionEdits.Deselect(Session), IsAnythingSelected);
        var invert = new RelayCommand(() => SelectionEdits.Invert(Session));
        var grow = new RelayCommand(() => SelectionEdits.Grow(Session), IsAnythingSelected);
        var shrink = new RelayCommand(() => SelectionEdits.Shrink(Session), IsAnythingSelected);
        var selectCellsWithGlyphs = new RelayCommand(() => SelectionEdits.SelectCellsWithGlyphs(Session));
        var selectCellsWithBackground = new RelayCommand(() => SelectionEdits.SelectCellsWithBackground(Session));
        RelayCommand[] needingSelection = [deselect, grow, shrink];

        // New brings a new selection, so the commands that need one follow whichever is current.
        var followedSelection = Session.Selection;
        followedSelection.Changed += OnSelectionChanged;
        activeDocument.SessionChanged += (_, _) =>
        {
            followedSelection.Changed -= OnSelectionChanged;
            followedSelection = Session.Selection;
            followedSelection.Changed += OnSelectionChanged;
            NotifyNeedingSelection();
        };

        yield return new AppAction(SelectionActionIds.SelectAll, nameof(Strings.ActionSelectAll), selectAll,
            [new ActionShortcut(Key.A, ShortcutModifiers.Command)]);
        yield return new AppAction(SelectionActionIds.Deselect, nameof(Strings.ActionDeselect), deselect,
            [new ActionShortcut(Key.D, ShortcutModifiers.Command)]);
        yield return new AppAction(SelectionActionIds.Invert, nameof(Strings.ActionInvertSelection), invert,
            [new ActionShortcut(Key.I, ShortcutModifiers.Command | ShortcutModifiers.Shift)]);
        yield return new AppAction(SelectionActionIds.Grow, nameof(Strings.ActionGrowSelection), grow, []);
        yield return new AppAction(SelectionActionIds.Shrink, nameof(Strings.ActionShrinkSelection), shrink, []);
        yield return new AppAction(SelectionActionIds.SelectCellsWithGlyphs,
            nameof(Strings.ActionSelectCellsWithGlyphs), selectCellsWithGlyphs, []);
        yield return new AppAction(SelectionActionIds.SelectCellsWithBackground,
            nameof(Strings.ActionSelectCellsWithBackground), selectCellsWithBackground, []);

        void OnSelectionChanged(object? sender, SessionSelectionChangedEventArgs e) => NotifyNeedingSelection();

        void NotifyNeedingSelection()
        {
            foreach (var command in needingSelection)
                command.NotifyCanExecuteChanged();
        }
    }

    private bool IsAnythingSelected() => !Session.Selection.IsEmpty;

    private EditingSession Session => activeDocument.Session;
}
