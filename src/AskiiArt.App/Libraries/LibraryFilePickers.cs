// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell;
using Avalonia.Platform.Storage;

namespace AskiiArt.App.Libraries;

/// <summary>
/// The system's file pickers, over the main window, for importing and exporting the files of one kind of library item,
/// such as ramps or character sets (ADR 0053).
/// </summary>
/// <param name="fileTypeName">What the picker calls this kind of file.</param>
/// <param name="extension">The files' extension, with its dot.</param>
/// <param name="importTitle">The import picker's title.</param>
/// <param name="exportTitle">The export picker's title.</param>
internal sealed class LibraryFilePickers(string fileTypeName, string extension, string importTitle,
    string exportTitle)
{
    // There's no registered type for these files, so the extension alone describes them.
    private readonly FilePickerFileType _files = new(fileTypeName) { Patterns = ["*" + extension] };

    /// <summary>The files the user picked to import, none when they cancelled.</summary>
    public async Task<IReadOnlyList<string>> PickFilesToImportAsync()
    {
        if (AppWindows.Main is not { } owner)
            return [];

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = importTitle,
            AllowMultiple = true,
            FileTypeFilter = [_files, FilePickerFileTypes.All]
        });

        return [.. files.Select(file => file.TryGetLocalPath()).OfType<string>()];
    }

    /// <summary>Where the user chose to export to, or null when they cancelled.</summary>
    /// <param name="suggestedName">The file name to suggest, without the extension.</param>
    public async Task<string?> PickFileToExportAsync(string suggestedName)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = exportTitle,
            SuggestedFileName = suggestedName,
            DefaultExtension = extension.TrimStart('.'),
            FileTypeChoices = [_files],
            ShowOverwritePrompt = true
        });

        return file?.TryGetLocalPath();
    }
}
