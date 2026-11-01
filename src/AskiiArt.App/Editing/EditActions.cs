// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Exporting;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Editing;

/// <summary>Undo and Redo, for the active document's history, and Copy as Text.</summary>
internal sealed class EditActions(ActiveDocument activeDocument, TextExporter textExporter, IAppShell shell)
    : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var undo = new RelayCommand(() => History.Undo(), () => History.CanUndo);
        var redo = new RelayCommand(() => History.Redo(), () => History.CanRedo);

        // New brings a new history, so the commands follow whichever history is current.
        var followedHistory = History;
        followedHistory.StateChanged += OnHistoryChanged;
        activeDocument.SessionChanged += (_, _) =>
        {
            followedHistory.StateChanged -= OnHistoryChanged;
            followedHistory = History;
            followedHistory.StateChanged += OnHistoryChanged;
            OnHistoryChanged(this, EventArgs.Empty);
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

        yield return new AppAction(EditActionIds.CopyAsText, nameof(Strings.ActionCopyAsText),
            new AsyncRelayCommand(CopyAsTextAsync),
            [new ActionShortcut(Key.C, ShortcutModifiers.Command | ShortcutModifiers.Shift)]);

        void OnHistoryChanged(object? sender, EventArgs e)
        {
            undo.NotifyCanExecuteChanged();
            redo.NotifyCanExecuteChanged();
        }
    }

    private CommandHistory History => activeDocument.Session.History;

    // With this platform's line breaks, which is what other apps expect from the clipboard (ADR 0022).
    private Task CopyAsTextAsync() =>
        shell.CopyTextAsync(textExporter.ToText(activeDocument.Session.Document, Environment.NewLine));
}
