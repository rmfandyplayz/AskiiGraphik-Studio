// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Editing;

/// <summary>
/// Undo and Redo, for the active document's history, and Copy as Text and Copy as ANSI, which flatten the visible layers:
/// the bounds of the selection's cells on the canvas while anything is selected, else the whole canvas (ADR 0067). While
/// the selection lies all past the canvas's edges they have nothing to copy, so they're greyed out (ADR 0071).
/// </summary>
internal sealed class EditActions(
    ActiveDocument activeDocument, TextExporter textExporter, AnsiExporter ansiExporter, IAppShell shell)
    : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var undo = new RelayCommand(() => History.Undo(), () => History.CanUndo);
        var redo = new RelayCommand(() => History.Redo(), () => History.CanRedo);
        var copyAsText = new AsyncRelayCommand(CopyAsTextAsync, HasSomethingToCopyAsText);
        var copyAsAnsi = new AsyncRelayCommand(CopyAsAnsiAsync, HasSomethingToCopyAsText);

        // New brings a new session and history, so the commands follow whichever are current.
        var followedSession = activeDocument.Session;
        Follow(followedSession);
        activeDocument.SessionChanged += (_, _) =>
        {
            Unfollow(followedSession);
            followedSession = activeDocument.Session;
            Follow(followedSession);
            OnHistoryChanged(this, EventArgs.Empty);
            OnSelectionChanged(this, EventArgs.Empty);
        };

        yield return new AppAction(EditActionIds.Undo, nameof(Strings.ActionUndo), undo,
            [new ActionShortcut(Key.Z, ShortcutModifiers.Command)]);

        // Cmd+Shift+Z first: it's the macOS shortcut, and the one the menu shows. Ctrl+Y is the
        // usual Windows one, so it works too.
        yield return new AppAction(EditActionIds.Redo, nameof(Strings.ActionRedo), redo,
        [
            new ActionShortcut(Key.Z, ShortcutModifiers.Command | ShortcutModifiers.Shift),
            new ActionShortcut(Key.Y, ShortcutModifiers.Command)
        ]);

        yield return new AppAction(EditActionIds.CopyAsText, nameof(Strings.ActionCopyAsText), copyAsText,
            [new ActionShortcut(Key.C, ShortcutModifiers.Command | ShortcutModifiers.Shift)]);
        yield return new AppAction(EditActionIds.CopyAsAnsi, nameof(Strings.ActionCopyAsAnsi), copyAsAnsi);

        void Follow(EditingSession session)
        {
            session.History.StateChanged += OnHistoryChanged;
            session.Selection.Changed += OnSelectionChanged;
        }

        void Unfollow(EditingSession session)
        {
            session.History.StateChanged -= OnHistoryChanged;
            session.Selection.Changed -= OnSelectionChanged;
        }

        void OnHistoryChanged(object? sender, EventArgs e)
        {
            undo.NotifyCanExecuteChanged();
            redo.NotifyCanExecuteChanged();
        }

        void OnSelectionChanged(object? sender, EventArgs e)
        {
            copyAsText.NotifyCanExecuteChanged();
            copyAsAnsi.NotifyCanExecuteChanged();
        }
    }

    // With nothing selected they copy the whole canvas; with a selection, its cells on the canvas.
    private bool HasSomethingToCopyAsText() => Session.Selection.IsEmpty || Session.Selection.Mask.HasCellsOnCanvas;

    private CommandHistory History => activeDocument.Session.History;

    // With this platform's line breaks, which is what other apps expect from the clipboard (ADR 0022).
    private Task CopyAsTextAsync() => shell.CopyTextAsync(
        textExporter.ToText(Session.Document, Environment.NewLine, Session.Selection.Mask));

    private Task CopyAsAnsiAsync() => shell.CopyTextAsync(
        ansiExporter.ToText(Session.Document, Environment.NewLine, Session.Selection.Mask));

    private EditingSession Session => activeDocument.Session;
}
