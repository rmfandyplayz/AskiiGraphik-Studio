// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Documents;

/// <summary><see cref="IDocumentDialogs"/> for the real app: modal windows owned by the main window.</summary>
internal sealed class AvaloniaDocumentDialogs(FontCatalog fonts, ILogger<FontPickerViewModel> fontPickerLogger)
    : IDocumentDialogs
{
    private static readonly FilePickerFileType _fontFiles = new(Strings.FontFileTypeName)
    {
        Patterns = ["*.ttf", "*.otf", "*.ttc"],
        AppleUniformTypeIdentifiers = ["public.truetype-ttf-font", "public.opentype-font", "public.truetype-collection-font"],
        MimeTypes = ["font/ttf", "font/otf", "font/collection"]
    };

    private static readonly FilePickerFileType _plainDocuments = new(Strings.DocumentFileTypeName)
    {
        Patterns = ["*" + DocumentFileFormat.PlainExtension]
    };

    private static readonly FilePickerFileType _compressedDocuments = new(Strings.CompressedDocumentFileTypeName)
    {
        Patterns = ["*" + DocumentFileFormat.CompressedExtension]
    };

    // Opening offers both variants together: the header decides how a file is read, not its name.
    private static readonly FilePickerFileType _allDocuments = new(Strings.DocumentFileTypeName)
    {
        Patterns = ["*" + DocumentFileFormat.PlainExtension, "*" + DocumentFileFormat.CompressedExtension]
    };

    public async Task<NewDocumentSettings?> ShowNewDocumentAsync()
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new NewDocumentWindow { DataContext = new NewDocumentViewModel() };
        return await dialog.ShowDialog<NewDocumentSettings?>(owner);
    }

    public async Task<CanvasResizeRequest?> ShowResizeCanvasAsync(CanvasSize currentSize)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new ResizeCanvasWindow { DataContext = new ResizeCanvasViewModel(currentSize) };
        return await dialog.ShowDialog<CanvasResizeRequest?>(owner);
    }

    public async Task<FontReference?> ShowFontPickerAsync(FontReference currentFont)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var dialog = new FontPickerWindow();
        dialog.DataContext = new FontPickerViewModel(fonts, currentFont, () => PickFontFileAsync(dialog),
            fontPickerLogger);
        return await dialog.ShowDialog<FontReference?>(owner);
    }

    public async Task<SaveChangesChoice> AskToSaveChangesAsync(string documentName)
    {
        if (AppWindows.Main is not { } owner)
            return SaveChangesChoice.Cancel;

        var dialog = new SaveChangesWindow { DataContext = new SaveChangesViewModel(documentName) };
        return await dialog.ShowDialog<SaveChangesChoice>(owner);
    }

    public async Task<string?> PickFileToOpenAsync()
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.OpenFilePickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [_allDocuments, FilePickerFileTypes.All]
        });

        if (files.Count == 0)
            return null;

        return files[0].TryGetLocalPath();
    }

    public async Task<DocumentFile?> PickFileToSaveAsync(string suggestedName, DocumentFile? current)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        IStorageFolder? startFolder = null;
        if (Path.GetDirectoryName(current?.Path) is { Length: > 0 } folder)
            startFolder = await owner.StorageProvider.TryGetFolderFromPathAsync(folder);

        // The document's own variant comes first, so Save As keeps it unless the user picks the other.
        var variant = current?.Variant ?? DocumentFileVariant.Plain;
        List<FilePickerFileType> fileTypes = [_plainDocuments, _compressedDocuments];
        if (variant == DocumentFileVariant.Compressed)
            fileTypes.Reverse();

        var result = await owner.StorageProvider.SaveFilePickerWithResultAsync(new FilePickerSaveOptions
        {
            Title = Strings.SaveFilePickerTitle,
            SuggestedFileName = suggestedName,
            SuggestedStartLocation = startFolder,
            DefaultExtension = DocumentFile.ExtensionOf(variant).TrimStart('.'),
            FileTypeChoices = fileTypes,
            ShowOverwritePrompt = true
        });

        if (result.File?.TryGetLocalPath() is not { } path)
            return null;

        var chosenVariant = DocumentFileVariant.Plain;
        if (result.SelectedFileType?.Name == _compressedDocuments.Name)
            chosenVariant = DocumentFileVariant.Compressed;

        return DocumentFile.ForSavePath(path, chosenVariant);
    }

    // The file picker belongs to the font picker, so it opens over it.
    private static async Task<string?> PickFontFileAsync(TopLevel owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.FontFilePickerTitle,
            AllowMultiple = false,
            FileTypeFilter = [_fontFiles]
        });

        if (files.Count == 0)
            return null;

        return files[0].TryGetLocalPath();
    }
}
