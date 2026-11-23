// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Text;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;

namespace AskiiArt.App.Editing;

/// <summary>
/// The Edit menu's Cut, Copy and Paste (ADR 0067). Copy puts the active layer's selected cells on the clipboard, as text
/// for other apps and in the app's own format; Cut does that and empties them, as one step. Paste floats what's on the
/// clipboard on the active layer with the Move tool, centred on the pointer, or types its text in at the Text tool's
/// caret. Copy needs something selected on a visible layer; Cut and Paste need a layer that isn't locked either.
/// </summary>
internal sealed class ClipboardActions(ActiveDocument activeDocument, CanvasViewModel canvas, IAppShell shell)
    : IActionProvider
{
    public IEnumerable<AppAction> CreateActions()
    {
        var cut = new AsyncRelayCommand(CutAsync, () => SelectionContents.CanClear(Session));
        var copy = new AsyncRelayCommand(CopyAsync, CanCopy);
        var paste = new AsyncRelayCommand(PasteAsync, () => Session.CanEditActiveLayer);
        AsyncRelayCommand[] commands = [cut, copy, paste];

        // New brings a new session, so the commands follow whichever is current: its selection, its active layer, and
        // the document's changes, which lock, unlock, hide and show that layer.
        var followedSession = Session;
        Follow(followedSession);
        activeDocument.SessionChanged += (_, _) =>
        {
            Unfollow(followedSession);
            followedSession = Session;
            Follow(followedSession);
            NotifyAll();
        };

        yield return new AppAction(EditActionIds.Cut, nameof(Strings.ActionCut), cut,
            [new ActionShortcut(Key.X, ShortcutModifiers.Command)]);
        yield return new AppAction(EditActionIds.Copy, nameof(Strings.ActionCopy), copy,
            [new ActionShortcut(Key.C, ShortcutModifiers.Command)]);
        yield return new AppAction(EditActionIds.Paste, nameof(Strings.ActionPaste), paste,
            [new ActionShortcut(Key.V, ShortcutModifiers.Command)]);

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

        void OnSelectionChanged(object? sender, SessionSelectionChangedEventArgs e) => NotifyAll();

        void OnSessionChanged(object? sender, EventArgs e) => NotifyAll();

        void OnDocumentChanged(object? sender, DocumentChangedEventArgs e) => NotifyAll();

        void NotifyAll()
        {
            foreach (var command in commands)
                command.NotifyCanExecuteChanged();
        }
    }

    private EditingSession Session => activeDocument.Session;

    // A hidden layer's cells aren't on screen, so copying them would copy what the user can't see. Only cells on the
    // canvas are copied (ADR 0071).
    private bool CanCopy() => Session.Selection.Mask.HasCellsOnCanvas && Session.ActiveLayer.Properties.IsVisible;

    private Task CopyAsync()
    {
        var document = Session.Document;
        var clip = CellClip.Copy(Session.ActiveLayer, Session.Selection.Mask);

        // With this platform's line breaks, which is what other apps expect from the clipboard (ADR 0022).
        return shell.CopyCellsAsync(clip.ToText(Environment.NewLine),
            CellClipFormat.Write(clip, document.CharacterSet, document.ColorMode));
    }

    private async Task CutAsync()
    {
        // The session can change while the clipboard is written, so the cells emptied are the ones copied.
        var session = Session;
        await CopyAsync();
        SelectionContents.Clear(session, UndoStepNames.Cut);
    }

    private async Task PasteAsync()
    {
        var contents = await shell.ReadClipboardAsync();

        // The clipboard is read first, which takes a moment, and the layer can be locked or hidden meanwhile.
        if (!Session.CanEditActiveLayer)
            return;

        if (canvas.ActiveTool is TextTool { Caret: not null } textTool)
        {
            if (contents.Text is { } text)
                textTool.PasteText(text);

            return;
        }

        if (ClipOf(contents) is not { } clip)
            return;

        var topLeft = PastePlacement.TopLeft(clip, Session.Document.CanvasSize, canvas.PointerCanvasPosition,
            canvas.ViewCentre);
        canvas.SelectTool(MoveTool.Id);
        if (canvas.ActiveTool is MoveTool moveTool && moveTool.Paste(clip, topLeft))
            canvas.NotePasted(Session.Selection.Mask.Bounds);
    }

    // The app's own cells when they're there, since they keep colours, shape and place; else the plain text.
    private static CellClip? ClipOf(ClipboardContents contents)
    {
        if (contents.Cells is { } cells && CellClipFormat.TryRead(cells) is { } clip)
            return clip;

        return contents.Text is { } text ? PastedText.ToClip(text) : null;
    }
}
