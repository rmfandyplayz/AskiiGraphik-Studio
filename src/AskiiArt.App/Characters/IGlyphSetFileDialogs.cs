// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Characters;

/// <summary>The file pickers for importing and exporting character set files. Tests replace them with a fake.</summary>
internal interface IGlyphSetFileDialogs
{
    /// <summary>The character set files the user picked to import, none when they cancelled.</summary>
    Task<IReadOnlyList<string>> PickFilesToImportAsync();

    /// <summary>Where the user chose to export a character set, or null when they cancelled.</summary>
    /// <param name="suggestedName">The file name to suggest, without the extension.</param>
    Task<string?> PickFileToExportAsync(string suggestedName);
}
