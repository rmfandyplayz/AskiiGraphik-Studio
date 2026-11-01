// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Editing;

/// <summary>
/// Undo, Redo, and the temporary ASCII Only switch (ADR 0016), for the document being edited.
/// </summary>
internal sealed class EditActions(EditingSession session, IAppShell shell) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var history = session.History;
        var undo = new RelayCommand(history.Undo, () => history.CanUndo);
        var redo = new RelayCommand(history.Redo, () => history.CanRedo);
        var asciiOnlyState = new ActionCheckedState(() => session.Document.CharacterSet == CharacterSet.Ascii);

        history.StateChanged += (_, _) =>
        {
            undo.NotifyCanExecuteChanged();
            redo.NotifyCanExecuteChanged();
        };
        session.Document.Changed += (_, e) =>
        {
            if (e.SettingsChanged)
                asciiOnlyState.NotifyChanged();
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

        yield return new AppAction(EditActionIds.AsciiOnly, nameof(Strings.ActionAsciiOnly),
            new AsyncRelayCommand(() => ToggleAsciiOnlyAsync(asciiOnlyState)), checkedState: asciiOnlyState);
    }

    // Switching to ASCII only removes every other character, so the user confirms first. It's one
    // undo step either way.
    private async Task ToggleAsciiOnlyAsync(ActionCheckedState asciiOnlyState)
    {
        var document = session.Document;
        if (document.CharacterSet == CharacterSet.Ascii)
        {
            session.History.Execute(new SetCharacterSetCommand(CharacterSet.Unicode));
            return;
        }

        var removedCount = SetCharacterSetCommand.CountCellsToRemove(document, CharacterSet.Ascii);
        if (removedCount > 0)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.AsciiOnlyConfirmMessageFormat, removedCount);
            if (!await shell.ConfirmAsync(Strings.AsciiOnlyConfirmTitle, message, Strings.AsciiOnlyConfirmRemove))
            {
                // A menu may already show the item ticked; put it back.
                asciiOnlyState.NotifyChanged();
                return;
            }
        }

        session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));
    }
}
