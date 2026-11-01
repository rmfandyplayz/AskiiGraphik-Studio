// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Formats;
using AskiiArt.Core.Tests.Commands;

namespace AskiiArt.Core.Tests.Formats;

/// <summary>Saving to and opening from real files in a temporary folder.</summary>
public sealed class DocumentFileSavingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AskiiArtTests", Guid.NewGuid().ToString("N"));

    public DocumentFileSavingTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Theory]
    [InlineData(DocumentFileVariant.Plain)]
    [InlineData(DocumentFileVariant.Compressed)]
    public async Task SaveAsync_ThenOpenAsync_GivesTheDocumentBack(DocumentFileVariant variant)
    {
        var document = DocumentFileFormatTests.CreateSampleDocument();
        var path = Path.Combine(_folder, "art.asciiart");

        await DocumentFileFormat.SaveAsync(document, path, variant, TestContext.Current.CancellationToken);
        var contents = await DocumentFileFormat.OpenAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(TestCommands.Snapshot(document), TestCommands.Snapshot(contents.Document));
        Assert.Equal(variant, contents.Variant);
    }

    [Fact]
    public async Task SaveAsync_OverAnExistingFile_ReplacesIt_AndLeavesNoTemporaryFiles()
    {
        var path = Path.Combine(_folder, "art.asciiart");
        await File.WriteAllTextAsync(path, "old contents", TestContext.Current.CancellationToken);

        await DocumentFileFormat.SaveAsync(TestCells.CreateDocument(2, 2), path, DocumentFileVariant.Plain,
            TestContext.Current.CancellationToken);

        Assert.StartsWith("ASKIIART 1\n", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
        Assert.Equal([path], Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task SaveAsync_ThatFails_LeavesWhatWasThereAndNoTemporaryFiles()
    {
        // A folder can't be replaced by a file, so the final step of the save fails.
        var path = Path.Combine(_folder, "taken");
        Directory.CreateDirectory(path);

        var exception = await Record.ExceptionAsync(() => DocumentFileFormat.SaveAsync(TestCells.CreateDocument(2, 2),
            path, DocumentFileVariant.Plain, TestContext.Current.CancellationToken));

        Assert.True(exception is IOException or UnauthorizedAccessException, $"Threw {exception}");
        Assert.True(Directory.Exists(path));
        Assert.Empty(Directory.GetFiles(_folder));
    }

    [Fact]
    public async Task OpenAsync_MissingFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_folder, "missing.asciiart");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            DocumentFileFormat.OpenAsync(path, TestContext.Current.CancellationToken));
    }
}
