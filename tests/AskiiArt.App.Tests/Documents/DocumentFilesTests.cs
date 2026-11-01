// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;

namespace AskiiArt.App.Tests.Documents;

/// <summary>Saving and opening through fake dialogs, with real files in a temporary folder.</summary>
public sealed class DocumentFilesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AskiiArtTests", Guid.NewGuid().ToString("N"));
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 2));
    private readonly FakeAppShell _shell = new();
    private readonly FakeDocumentDialogs _dialogs = new();
    private readonly DocumentFiles _files;

    public DocumentFilesTests()
    {
        Directory.CreateDirectory(_folder);
        _files = _canvas.CreateDocumentFiles(_dialogs, _shell);
    }

    private ActiveDocument ActiveDocument => _canvas.ActiveDocument;

    private CommandHistory History => _canvas.Session.History;

    public void Dispose()
    {
        _canvas.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Save_UntitledDocument_AsksWhere_ThenSavesThere()
    {
        MakeAChange();
        var file = FileIn("art.asciiart", DocumentFileVariant.Plain);
        _dialogs.FileToSave = file;

        Assert.True(await _files.SaveAsync());

        Assert.Equal([("Untitled", null)], _dialogs.SavePickerShownWith);
        Assert.Equal(file, ActiveDocument.File);
        Assert.False(History.HasUnsavedChanges);
        Assert.StartsWith("ASKIIART ", await File.ReadAllTextAsync(file.Path, TestContext.Current.CancellationToken),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Save_DocumentWithAFile_SavesThereWithoutAsking()
    {
        var file = FileIn("art.asciiartz", DocumentFileVariant.Compressed);
        _dialogs.FileToSave = file;
        await _files.SaveAsAsync();
        MakeAChange();

        Assert.True(await _files.SaveAsync());

        Assert.Single(_dialogs.SavePickerShownWith);
        Assert.False(History.HasUnsavedChanges);
        var reopened = await DocumentFileFormat.OpenAsync(file.Path, TestContext.Current.CancellationToken);
        Assert.Equal(DocumentFileVariant.Compressed, reopened.Variant);
        Assert.True(reopened.Document.ActiveFrame.Layers[0].GetCell(new CellPoint(0, 0)).IsOccupied);
    }

    [Fact]
    public async Task SaveAs_SuggestsTheCurrentFileName()
    {
        var file = FileIn("my art.asciiart", DocumentFileVariant.Plain);
        _dialogs.FileToSave = file;
        await _files.SaveAsAsync();

        await _files.SaveAsAsync();

        Assert.Equal(("my art", file), _dialogs.SavePickerShownWith[^1]);
    }

    [Fact]
    public async Task Save_Cancelled_KeepsTheChangesUnsaved()
    {
        MakeAChange();

        Assert.False(await _files.SaveAsync());

        Assert.True(History.HasUnsavedChanges);
        Assert.Null(ActiveDocument.File);
    }

    [Fact]
    public async Task Save_ThatFails_SaysWhy_AndKeepsTheChangesUnsaved()
    {
        MakeAChange();
        _dialogs.FileToSave = new DocumentFile(Path.Combine(_folder, "missing folder", "art.asciiart"),
            DocumentFileVariant.Plain);

        Assert.False(await _files.SaveAsync());

        Assert.Equal(Strings.SaveFailedTitle, Assert.Single(_shell.Messages).Title);
        Assert.True(History.HasUnsavedChanges);
        Assert.Null(ActiveDocument.File);
    }

    [Fact]
    public async Task Open_ReplacesTheDocument_AndRemembersItsFileAndVariant()
    {
        var path = await SaveSomewhereAsync(DocumentFileVariant.Compressed, "Fira Code");
        _dialogs.FileToOpen = path;

        await _files.OpenAsync();

        Assert.Equal(new DocumentFile(path, DocumentFileVariant.Compressed), ActiveDocument.File);
        Assert.Equal(new FontReference("Fira Code"), _canvas.Document.Font);
        Assert.False(History.HasUnsavedChanges);
        Assert.Empty(_shell.Messages);
    }

    [Fact]
    public async Task Open_WithUnsavedChanges_CancelledAtTheQuestion_DoesNothing()
    {
        MakeAChange();
        var before = _canvas.Session;

        await _files.OpenAsync();

        Assert.Equal(["Untitled"], _dialogs.SaveChangesAskedFor);
        Assert.Equal(0, _dialogs.OpenPickerShownCount);
        Assert.Same(before, _canvas.Session);
    }

    [Theory]
    [InlineData("hello", nameof(Strings.OpenFailedNotADocumentFormat))]
    [InlineData("ASKIIART 1\n{", nameof(Strings.OpenFailedDamagedFormat))]
    [InlineData("ASKIIART 99\n{}", nameof(Strings.OpenFailedNewerVersionFormat))]
    public async Task Open_FileThatCantBeOpened_SaysWhy_AndKeepsTheDocument(string contents, string messageKey)
    {
        var path = Path.Combine(_folder, "bad.asciiart");
        await File.WriteAllTextAsync(path, contents, Encoding.UTF8, TestContext.Current.CancellationToken);
        _dialogs.FileToOpen = path;
        var before = _canvas.Session;

        await _files.OpenAsync();

        // Each message names the file; the newer-version one also gives the version.
        var format = Strings.ResourceManager.GetString(messageKey, Strings.Culture)!;
        var expected = string.Format(CultureInfo.CurrentCulture, format, "bad.asciiart", 99);
        Assert.Equal((Strings.OpenFailedTitle, expected), Assert.Single(_shell.Messages));
        Assert.Same(before, _canvas.Session);
    }

    [Fact]
    public async Task Open_MissingFile_SaysWhy()
    {
        _dialogs.FileToOpen = Path.Combine(_folder, "gone.asciiart");

        await _files.OpenAsync();

        Assert.Contains("gone.asciiart", Assert.Single(_shell.Messages).Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Open_DocumentWhoseFontIsMissing_SaysSo_AndKeepsAskingForIt()
    {
        _dialogs.FileToOpen = await SaveSomewhereAsync(DocumentFileVariant.Plain, "A Font Nobody Has");

        await _files.OpenAsync();

        Assert.Equal(Strings.FontMissingTitle, Assert.Single(_shell.Messages).Title);
        Assert.Equal(new FontReference("A Font Nobody Has"), _canvas.Document.Font);
        Assert.Equal(_canvas.Font, _canvas.ViewModel.Font);
        Assert.Contains("A Font Nobody Has", _canvas.ViewModel.FontText, StringComparison.Ordinal);
        Assert.NotEqual("A Font Nobody Has", _canvas.ViewModel.FontText);
    }

    [Fact]
    public async Task ConfirmDiscardingChanges_DontSave_GoesAheadWithoutSaving()
    {
        MakeAChange();
        _dialogs.SaveChangesAnswer = SaveChangesChoice.DontSave;

        Assert.True(await _files.ConfirmDiscardingChangesAsync());
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public async Task ConfirmDiscardingChanges_Cancel_StopsIt()
    {
        MakeAChange();
        _dialogs.SaveChangesAnswer = SaveChangesChoice.Cancel;

        Assert.False(await _files.ConfirmDiscardingChangesAsync());
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public async Task ConfirmDiscardingChanges_Save_GoesAheadOnlyOnceSaved()
    {
        MakeAChange();
        _dialogs.SaveChangesAnswer = SaveChangesChoice.Save;

        // Cancelling the save picker cancels the whole thing.
        Assert.False(await _files.ConfirmDiscardingChangesAsync());

        _dialogs.FileToSave = FileIn("art.asciiart", DocumentFileVariant.Plain);
        Assert.True(await _files.ConfirmDiscardingChangesAsync());
        Assert.False(History.HasUnsavedChanges);
    }

    [Fact]
    public async Task ConfirmDiscardingChanges_WithoutChanges_DoesntAsk()
    {
        Assert.True(await _files.ConfirmDiscardingChangesAsync());

        Assert.Empty(_dialogs.SaveChangesAskedFor);
    }

    [Fact]
    public async Task ExportAsText_WritesTheArt_AndLeavesTheDocumentAsItWas()
    {
        var file = FileIn("art.asciiart", DocumentFileVariant.Plain);
        _dialogs.FileToSave = file;
        await _files.SaveAsAsync();
        MakeAChange();
        var path = Path.Combine(_folder, "art.txt");
        _dialogs.FileToExport = path;

        await _files.ExportAsTextAsync();

        Assert.Equal([("art", _folder)], _dialogs.ExportPickerShownWith);
        Assert.Equal("x   \n    \n", await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        Assert.Equal(file, ActiveDocument.File);
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public async Task ExportAsText_AddsTheExtension_WhenTheNameHasNone()
    {
        _dialogs.FileToExport = Path.Combine(_folder, "art");

        await _files.ExportAsTextAsync();

        Assert.Equal([("Untitled", null)], _dialogs.ExportPickerShownWith);
        Assert.True(File.Exists(Path.Combine(_folder, "art.txt")));
    }

    [Fact]
    public async Task ExportAsText_Cancelled_WritesNothing()
    {
        await _files.ExportAsTextAsync();

        Assert.Empty(Directory.GetFiles(_folder));
        Assert.Empty(_shell.Messages);
    }

    [Fact]
    public async Task ExportAsText_ThatCantBeWritten_SaysSo()
    {
        _dialogs.FileToExport = Path.Combine(_folder, "missing folder", "art.txt");

        await _files.ExportAsTextAsync();

        var message = Assert.Single(_shell.Messages);
        Assert.Equal(Strings.ExportFailedTitle, message.Title);
        Assert.Contains("art.txt", message.Message, StringComparison.Ordinal);
    }

    private void MakeAChange()
    {
        var edit = new CellEditBuilder(_canvas.Document, _canvas.Document.ActiveFrame.Layers[0].Id);
        edit.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('x')));
        History.Execute(edit.Build()!);
    }

    private DocumentFile FileIn(string name, DocumentFileVariant variant) => new(Path.Combine(_folder, name), variant);

    private async Task<string> SaveSomewhereAsync(DocumentFileVariant variant, string fontName)
    {
        var document = Document.Create(new CanvasSize(3, 2), "Layer", CharacterSet.Unicode, new FontReference(fontName));
        var path = Path.Combine(_folder, $"saved{DocumentFile.ExtensionOf(variant)}");
        await DocumentFileFormat.SaveAsync(document, path, variant, TestContext.Current.CancellationToken);
        return path;
    }
}
