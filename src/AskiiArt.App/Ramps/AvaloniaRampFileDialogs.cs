// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Libraries;
using AskiiArt.App.Resources;
using AskiiArt.Core.Ramps;

namespace AskiiArt.App.Ramps;

/// <summary><see cref="IRampFileDialogs"/> for the real app: the system's file pickers, over the main window.</summary>
internal sealed class AvaloniaRampFileDialogs : IRampFileDialogs
{
    private readonly LibraryFilePickers _pickers = new(Strings.RampFileTypeName, RampFile.Extension,
        Strings.ImportRampsFilePickerTitle, Strings.ExportRampFilePickerTitle);

    public Task<IReadOnlyList<string>> PickFilesToImportAsync() => _pickers.PickFilesToImportAsync();

    public Task<string?> PickFileToExportAsync(string suggestedName) => _pickers.PickFileToExportAsync(suggestedName);
}
