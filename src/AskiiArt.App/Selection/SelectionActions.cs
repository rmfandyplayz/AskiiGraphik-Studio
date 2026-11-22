// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Selection;

/// <summary>
/// The Select menu's commands, for the active document's selection (ADR 0055, ADR 0063): Select All, Deselect, Invert,
/// Grow, Shrink, and selecting the cells with glyphs or with a background colour, plus the Edit menu's Delete, which
/// empties the selected cells of the active layer (ADR 0065). Each is an undo step. Deselect is available only while
/// something is selected; Grow, Shrink and Delete only while some selected cell is on the canvas (ADR 0071), and Delete
/// only while the active layer can be changed too.
/// </summary>
internal sealed class SelectionActions(ActiveDocument activeDocument) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var selectAll = new RelayCommand(() => SelectionEdits.SelectAll(Session));
        var deselect = new RelayCommand(() => SelectionEdits.Deselect(Session), IsAnythingSelected);
        var invert = new RelayCommand(() => SelectionEdits.Invert(Session));
        var grow = new RelayCommand(() => SelectionEdits.Grow(Session), IsAnythingSelectedOnCanvas);
        var shrink = new RelayCommand(() => SelectionEdits.Shrink(Session), IsAnythingSelectedOnCanvas);
        var selectCellsWithGlyphs = new RelayCommand(() => SelectionEdits.SelectCellsWithGlyphs(Session));
        var selectCellsWithBackground = new RelayCommand(() => SelectionEdits.SelectCellsWithBackground(Session));
        var deleteSelectedCells = new RelayCommand(() => SelectionContents.Clear(Session),
            () => SelectionContents.CanClear(Session));
        RelayCommand[] needingSelection = [deselect, grow, shrink, deleteSelectedCells];

        // New brings a new session, so the commands that need a selection follow whichever is current. Delete also
        // follows the active layer, and the document's changes, which lock, unlock, hide and show it.
        var followedSession = Session;
        Follow(followedSession);
        activeDocument.SessionChanged += (_, _) =>
        {
            Unfollow(followedSession);
            followedSession = Session;
            Follow(followedSession);
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

        // The Delete and Backspace keys reach it through the canvas rather than as shortcuts, so they keep editing
        // text in text boxes and at the Text tool's caret (CanvasView).
        yield return new AppAction(SelectionActionIds.DeleteSelectedCells, nameof(Strings.ActionDeleteSelectedCells),
            deleteSelectedCells, []);

        void Follow(EditingSession session)
        {
            session.Selection.Changed += OnSelectionChanged;
            session.ActiveLayerChanged += OnSessionChanged;
            session.Document.Changed += OnDocumentChanged;
        }

        void Unfollow(EditingSession session)
        {
            session.Selection.Changed -= OnSelectionChanged;
            session.ActiveLayerChanged -= OnSessionChanged;
            session.Document.Changed -= OnDocumentChanged;
        }

        void OnSelectionChanged(object? sender, SessionSelectionChangedEventArgs e) => NotifyNeedingSelection();

        void OnSessionChanged(object? sender, EventArgs e) => NotifyNeedingSelection();

        void OnDocumentChanged(object? sender, DocumentChangedEventArgs e) =>
            deleteSelectedCells.NotifyCanExecuteChanged();

        void NotifyNeedingSelection()
        {
            foreach (var command in needingSelection)
                command.NotifyCanExecuteChanged();
        }
    }

    private bool IsAnythingSelected() => !Session.Selection.IsEmpty;

    private bool IsAnythingSelectedOnCanvas() => Session.Selection.Mask.HasCellsOnCanvas;

    private EditingSession Session => activeDocument.Session;
}
