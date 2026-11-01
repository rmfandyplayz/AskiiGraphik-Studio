// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Documents;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Answers the document dialogs straight away with whatever the test set, and records what they were shown.</summary>
internal sealed class FakeDocumentDialogs : IDocumentDialogs
{
    /// <summary>What the New dialog returns; null means the user cancelled.</summary>
    public NewDocumentSettings? NewDocumentAnswer { get; set; }

    public CanvasResizeRequest? ResizeCanvasAnswer { get; set; }

    public FontReference? FontPickerAnswer { get; set; }

    /// <summary>The answer to "save the changes?"; Cancel unless the test says otherwise.</summary>
    public SaveChangesChoice SaveChangesAnswer { get; set; }

    /// <summary>The path the open picker returns; null means the user cancelled.</summary>
    public string? FileToOpen { get; set; }

    /// <summary>The file the save picker returns; null means the user cancelled.</summary>
    public DocumentFile? FileToSave { get; set; }

    public int NewDocumentShownCount { get; private set; }

    /// <summary>The canvas size each Resize Canvas dialog was opened with.</summary>
    public List<CanvasSize> ResizeCanvasShownWith { get; } = [];

    /// <summary>The current font each font picker was opened with.</summary>
    public List<FontReference> FontPickerShownWith { get; } = [];

    /// <summary>The document name each "save the changes?" question named.</summary>
    public List<string> SaveChangesAskedFor { get; } = [];

    public int OpenPickerShownCount { get; private set; }

    /// <summary>The suggested name and current file each save picker was opened with.</summary>
    public List<(string SuggestedName, DocumentFile? Current)> SavePickerShownWith { get; } = [];

    public Task<NewDocumentSettings?> ShowNewDocumentAsync()
    {
        NewDocumentShownCount++;
        return Task.FromResult(NewDocumentAnswer);
    }

    public Task<CanvasResizeRequest?> ShowResizeCanvasAsync(CanvasSize currentSize)
    {
        ResizeCanvasShownWith.Add(currentSize);
        return Task.FromResult(ResizeCanvasAnswer);
    }

    public Task<FontReference?> ShowFontPickerAsync(FontReference currentFont)
    {
        FontPickerShownWith.Add(currentFont);
        return Task.FromResult(FontPickerAnswer);
    }

    public Task<SaveChangesChoice> AskToSaveChangesAsync(string documentName)
    {
        SaveChangesAskedFor.Add(documentName);
        return Task.FromResult(SaveChangesAnswer);
    }

    public Task<string?> PickFileToOpenAsync()
    {
        OpenPickerShownCount++;
        return Task.FromResult(FileToOpen);
    }

    public Task<DocumentFile?> PickFileToSaveAsync(string suggestedName, DocumentFile? current)
    {
        SavePickerShownWith.Add((suggestedName, current));
        return Task.FromResult(FileToSave);
    }
}
