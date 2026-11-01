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

namespace AskiiArt.App.Documents;

/// <summary>
/// New, Resize Canvas, Font, and the temporary ASCII Only switch (ADR 0016), for the active
/// document. Everything but New is an undoable command.
/// </summary>
internal sealed class DocumentActions(ActiveDocument activeDocument, IDocumentDialogs dialogs, IAppShell shell)
    : IActionProvider
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

        yield return new AppAction(DocumentActionIds.ResizeCanvas, nameof(Strings.ActionResizeCanvas),
            new AsyncRelayCommand(ResizeCanvasAsync));

        yield return new AppAction(DocumentActionIds.Font, nameof(Strings.ActionFont),
            new AsyncRelayCommand(ChooseFontAsync));

        yield return new AppAction(DocumentActionIds.AsciiOnly, nameof(Strings.ActionAsciiOnly),
            new AsyncRelayCommand(() => ToggleAsciiOnlyAsync(asciiOnlyState)), checkedState: asciiOnlyState);

        void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            if (e.SettingsChanged)
                asciiOnlyState.NotifyChanged();
        }
    }

    private EditingSession Session => activeDocument.Session;

    private Document Document => Session.Document;

    // There's nothing to save yet, so New doesn't ask about unsaved changes (that comes in 1.7).
    private async Task NewAsync()
    {
        if (await dialogs.ShowNewDocumentAsync() is not { } settings)
            return;

        var document = Document.Create(settings.Size, Strings.DefaultLayerName, settings.CharacterSet);
        activeDocument.Replace(new EditingSession(document));
    }

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
