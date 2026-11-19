// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Characters;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>Answers the character set file pickers straight away with whatever the test set.</summary>
internal sealed class FakeGlyphSetFileDialogs : IGlyphSetFileDialogs
{
    /// <summary>The paths the import picker returns; none means the user cancelled.</summary>
    public List<string> FilesToImport { get; } = [];

    /// <summary>The path the export picker returns; null means the user cancelled.</summary>
    public string? FileToExport { get; set; }

    /// <summary>The name each export picker suggested.</summary>
    public List<string> ExportSuggestedNames { get; } = [];

    public Task<IReadOnlyList<string>> PickFilesToImportAsync() =>
        Task.FromResult<IReadOnlyList<string>>([.. FilesToImport]);

    public Task<string?> PickFileToExportAsync(string suggestedName)
    {
        ExportSuggestedNames.Add(suggestedName);
        return Task.FromResult(FileToExport);
    }
}
