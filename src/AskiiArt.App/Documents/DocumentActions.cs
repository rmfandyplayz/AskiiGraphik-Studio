// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Documents;

/// <summary>
/// New, Open, Save, Save As and Export as Text, Resize Canvas, Font, and the temporary ASCII Only switch
/// (ADR 0016), for the active document. Resize Canvas, Font and ASCII Only are undoable commands.
/// </summary>
internal sealed class DocumentActions(ActiveDocument activeDocument, DocumentFiles files, IDocumentDialogs dialogs,
    IAppShell shell, bool includeDebugActions) : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var asciiOnlyState = new ActionCheckedState(() => Document.CharacterSet == CharacterSet.Ascii);

        // New brings another document, so the check mark follows whichever document is current.
        var followedDocument = Document;
        followedDocument.Changed += OnDocumentChanged;
        activeDocument.SessionChanged += (_, _) =>
        {
            followedDocument.Changed -= OnDocumentChanged;
            followedDocument = Document;
            followedDocument.Changed += OnDocumentChanged;
            asciiOnlyState.NotifyChanged();
        };

        yield return new AppAction(DocumentActionIds.New, nameof(Strings.ActionNew), new AsyncRelayCommand(NewAsync),
            [new ActionShortcut(Key.N, ShortcutModifiers.Command)]);
        yield return new AppAction(DocumentActionIds.Open, nameof(Strings.ActionOpen),
            new AsyncRelayCommand(files.OpenAsync), [new ActionShortcut(Key.O, ShortcutModifiers.Command)]);
        yield return new AppAction(DocumentActionIds.Save, nameof(Strings.ActionSave),
            new AsyncRelayCommand(files.SaveAsync), [new ActionShortcut(Key.S, ShortcutModifiers.Command)]);
        yield return new AppAction(DocumentActionIds.SaveAs, nameof(Strings.ActionSaveAs),
            new AsyncRelayCommand(files.SaveAsAsync),
            [new ActionShortcut(Key.S, ShortcutModifiers.Command | ShortcutModifiers.Shift)]);

        yield return new AppAction(DocumentActionIds.ExportAsText, nameof(Strings.ActionExportAsText),
            new AsyncRelayCommand(files.ExportAsTextAsync));

        yield return new AppAction(DocumentActionIds.ResizeCanvas, nameof(Strings.ActionResizeCanvas),
            new AsyncRelayCommand(ResizeCanvasAsync));

        yield return new AppAction(DocumentActionIds.Font, nameof(Strings.ActionFont),
            new AsyncRelayCommand(ChooseFontAsync));

        yield return new AppAction(DocumentActionIds.AsciiOnly, nameof(Strings.ActionAsciiOnly),
            new AsyncRelayCommand(() => ToggleAsciiOnlyAsync(asciiOnlyState)), checkedState: asciiOnlyState);

        if (includeDebugActions)
        {
            yield return new AppAction(DocumentActionIds.OpenSampleDocument, nameof(Strings.ActionOpenSampleDocument),
                new AsyncRelayCommand(() => files.ReplaceWithNewAsync(
                    () => Task.FromResult<Document?>(SampleDocument.Create(Strings.DefaultLayerName)))));
        }

        void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            if (e.SettingsChanged)
                asciiOnlyState.NotifyChanged();
        }
    }

    private EditingSession Session => activeDocument.Session;

    private Document Document => Session.Document;

    // Unsaved changes are dealt with before the New dialog, as in other editors.
    private Task NewAsync() => files.ReplaceWithNewAsync(async () =>
        (await dialogs.ShowNewDocumentAsync())?.CreateDocument());

    private async Task ResizeCanvasAsync()
    {
        var session = Session;
        var request = await dialogs.ShowResizeCanvasAsync(session.Document.CanvasSize);
        if (request is null || request.Size == session.Document.CanvasSize)
            return;

        session.History.Execute(new ResizeCanvasCommand(request.Size, request.Anchor));
    }

    private async Task ChooseFontAsync()
    {
        var session = Session;
        var font = await dialogs.ShowFontPickerAsync(session.Document.Font);
        if (font is null || font == session.Document.Font)
            return;

        session.History.Execute(new SetFontCommand(font));
    }

    // Switching to ASCII only removes every other character, so the user confirms first. It's one
    // undo step either way.
    private async Task ToggleAsciiOnlyAsync(ActionCheckedState asciiOnlyState)
    {
        var session = Session;
        if (session.Document.CharacterSet == CharacterSet.Ascii)
        {
            session.History.Execute(new SetCharacterSetCommand(CharacterSet.Unicode));
            return;
        }

        var removedCount = SetCharacterSetCommand.CountCellsToRemove(session.Document, CharacterSet.Ascii);
        if (removedCount > 0)
        {
            var message = string.Format(CultureInfo.CurrentCulture, Strings.AsciiOnlyConfirmMessageFormat,
                removedCount);
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
