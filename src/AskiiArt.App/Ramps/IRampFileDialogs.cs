// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Ramps;

/// <summary>The file pickers for importing and exporting ramp files. Tests replace them with a fake.</summary>
internal interface IRampFileDialogs
{
    /// <summary>The ramp files the user picked to import, none when they cancelled.</summary>
    Task<IReadOnlyList<string>> PickFilesToImportAsync();

    /// <summary>Where the user chose to export a ramp, or null when they cancelled.</summary>
    /// <param name="suggestedName">The file name to suggest, without the extension.</param>
    Task<string?> PickFileToExportAsync(string suggestedName);
}
