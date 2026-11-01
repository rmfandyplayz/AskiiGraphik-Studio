// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Documents;

/// <summary>
/// The dialogs the document actions ask the user through. Each returns null when the user
/// cancels. Tests replace this with a fake that answers straight away.
/// </summary>
internal interface IDocumentDialogs
{
    Task<NewDocumentSettings?> ShowNewDocumentAsync();

    Task<CanvasResizeRequest?> ShowResizeCanvasAsync(CanvasSize currentSize);

    Task<FontReference?> ShowFontPickerAsync(FontReference currentFont);

    /// <summary>Asks whether to save the changes to <paramref name="documentName"/> before they're lost.</summary>
    Task<SaveChangesChoice> AskToSaveChangesAsync(string documentName);

    /// <summary>Asks for a document file to open; returns its path.</summary>
    Task<string?> PickFileToOpenAsync();

    /// <summary>Asks where to save the document, starting from <paramref name="current"/> if it has a file.</summary>
    /// <param name="suggestedName">The file name to suggest, without an extension.</param>
    /// <param name="current">The document's file, or null if it's never been saved.</param>
    Task<DocumentFile?> PickFileToSaveAsync(string suggestedName, DocumentFile? current);
}

/// <summary>The answer to "save the changes?".</summary>
internal enum SaveChangesChoice
{
    /// <summary>Don't go ahead. Also the answer when the dialog is closed any other way.</summary>
    Cancel = 0,
    Save = 1,
    DontSave = 2
}

/// <summary>What the New dialog asks for.</summary>
internal sealed record NewDocumentSettings(CanvasSize Size, CharacterSet CharacterSet)
{
    /// <summary>What the New dialog starts with, and the document the app opens with.</summary>
    public static NewDocumentSettings Default { get; } = new(new CanvasSize(80, 25), CharacterSet.Ascii);

    public Document CreateDocument() => Document.Create(Size, Strings.DefaultLayerName, CharacterSet);
}

/// <summary>What the Resize Canvas dialog asks for.</summary>
internal sealed record CanvasResizeRequest(CanvasSize Size, ResizeAnchor Anchor);
