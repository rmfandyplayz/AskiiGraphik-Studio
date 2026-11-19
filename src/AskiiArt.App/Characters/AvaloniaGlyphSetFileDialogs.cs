// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Libraries;
using AskiiArt.App.Resources;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.App.Characters;

/// <summary>
/// <see cref="IGlyphSetFileDialogs"/> for the real app: the system's file pickers, over the main window.
/// </summary>
internal sealed class AvaloniaGlyphSetFileDialogs : IGlyphSetFileDialogs
{
    private readonly LibraryFilePickers _pickers = new(Strings.CharacterSetFileTypeName, GlyphSetFile.Extension,
        Strings.ImportCharacterSetsFilePickerTitle, Strings.ExportCharacterSetFilePickerTitle);

    public Task<IReadOnlyList<string>> PickFilesToImportAsync() => _pickers.PickFilesToImportAsync();

    public Task<string?> PickFileToExportAsync(string suggestedName) => _pickers.PickFileToExportAsync(suggestedName);
}
