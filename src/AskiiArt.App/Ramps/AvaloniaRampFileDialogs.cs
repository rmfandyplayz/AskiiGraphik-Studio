// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Ramps;
using Avalonia.Platform.Storage;

namespace AskiiArt.App.Ramps;

/// <summary><see cref="IRampFileDialogs"/> for the real app: the system's file pickers, over the main window.</summary>
internal sealed class AvaloniaRampFileDialogs : IRampFileDialogs
{
    // There's no registered type for ramp files, so the extension alone describes them.
    private static readonly FilePickerFileType _rampFiles = new(Strings.RampFileTypeName)
    {
        Patterns = ["*" + RampFile.Extension]
    };

    public async Task<IReadOnlyList<string>> PickFilesToImportAsync()
    {
        if (AppWindows.Main is not { } owner)
            return [];

        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.ImportRampsFilePickerTitle,
            AllowMultiple = true,
            FileTypeFilter = [_rampFiles, FilePickerFileTypes.All]
        });

        return [.. files.Select(file => file.TryGetLocalPath()).OfType<string>()];
    }

    public async Task<string?> PickFileToExportAsync(string suggestedName)
    {
        if (AppWindows.Main is not { } owner)
            return null;

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.ExportRampFilePickerTitle,
            SuggestedFileName = suggestedName,
            DefaultExtension = RampFile.Extension.TrimStart('.'),
            FileTypeChoices = [_rampFiles],
            ShowOverwritePrompt = true
        });

        return file?.TryGetLocalPath();
    }
}
