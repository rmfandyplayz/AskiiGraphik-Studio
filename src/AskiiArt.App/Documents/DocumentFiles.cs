// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Formats;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Documents;

/// <summary>
/// Saving, opening and exporting the active document, and the unsaved-changes question that guards
/// anything that would replace or close it (ADRs 0021 and 0022). Failures are explained to the user
/// and logged; they never lose the document on screen.
/// </summary>
internal sealed partial class DocumentFiles(ActiveDocument activeDocument, IDocumentDialogs dialogs, IAppShell shell,
    FontCatalog fonts, TextExporter textExporter, ILogger<DocumentFiles> logger)
{
    public bool HasUnsavedChanges => activeDocument.Session.History.HasUnsavedChanges;

    /// <summary>
    /// Before the document is replaced or closed: when it has unsaved changes, asks whether to
    /// save them. True to go ahead, which after "Save" means the save worked.
    /// </summary>
    public async Task<bool> ConfirmDiscardingChangesAsync()
    {
        if (!HasUnsavedChanges)
            return true;

        return await dialogs.AskToSaveChangesAsync(activeDocument.DisplayName) switch
        {
            SaveChangesChoice.Save => await SaveAsync(),
            SaveChangesChoice.DontSave => true,
            _ => false
        };
    }

    /// <summary>
    /// Replaces the document with a new, unsaved one, once any unsaved changes are dealt with.
    /// <paramref name="createDocument"/> returns null when the user cancels (the New dialog).
    /// </summary>
    public async Task ReplaceWithNewAsync(Func<Task<Document?>> createDocument)
    {
        ArgumentNullException.ThrowIfNull(createDocument);

        if (!await ConfirmDiscardingChangesAsync())
            return;

        if (await createDocument() is { } document)
            activeDocument.Replace(new EditingSession(document));
    }

    /// <summary>Asks for a file and opens it in place of the document.</summary>
    public async Task OpenAsync()
    {
        if (!await ConfirmDiscardingChangesAsync())
            return;

        if (await dialogs.PickFileToOpenAsync() is not { } path)
            return;

        DocumentFileContents contents;
        try
        {
            contents = await DocumentFileFormat.OpenAsync(path);
        }
        catch (Exception exception) when (exception is DocumentFileException or IOException
                                              or UnauthorizedAccessException)
        {
            LogCouldNotOpen(exception, path);
            await shell.ShowMessageAsync(Strings.OpenFailedTitle, DescribeOpenFailure(exception, path));
            return;
        }

        activeDocument.Replace(new EditingSession(contents.Document), new DocumentFile(path, contents.Variant));

        // The document keeps asking for its own font (ARCHITECTURE §5.10), so saving doesn't lose it.
        var font = contents.Document.Font;
        if (!fonts.IsAvailable(font))
        {
            await shell.ShowMessageAsync(Strings.FontMissingTitle, Format(Strings.FontMissingMessageFormat,
                font.FamilyName, fonts.DefaultFont.FamilyName));
        }
    }

    /// <summary>Saves to the document's file, or asks for one first. True when it was saved.</summary>
    public async Task<bool> SaveAsync()
    {
        if (activeDocument.File is not { } file)
            return await SaveAsAsync();

        return await SaveToAsync(file);
    }

    /// <summary>Asks for a file and saves the document there. True when it was saved.</summary>
    public async Task<bool> SaveAsAsync()
    {
        if (await dialogs.PickFileToSaveAsync(SuggestedFileName(), activeDocument.File) is not { } file)
            return false;

        return await SaveToAsync(file);
    }

    /// <summary>
    /// Asks for a file and writes the document there as text. The document itself is unchanged: it
    /// keeps its own file and any unsaved changes.
    /// </summary>
    public async Task ExportAsTextAsync()
    {
        var folder = Path.GetDirectoryName(activeDocument.File?.Path);
        if (await dialogs.PickTextFileToExportAsync(SuggestedFileName(), folder) is not { } path)
            return;

        if (!Path.HasExtension(path))
            path += textExporter.FileExtension;

        // Read now, on the UI thread; only the writing happens in the background.
        var text = textExporter.Export(activeDocument.Session.Document);
        try
        {
            await AtomicFile.WriteAsync(path, text);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogCouldNotExport(exception, path);
            await shell.ShowMessageAsync(Strings.ExportFailedTitle,
                Format(Strings.ExportFailedFormat, Path.GetFileName(path), exception.Message));
        }
    }

    // The document's own name, so Save As and Export as Text suggest the name it already has.
    private string SuggestedFileName()
    {
        if (activeDocument.File is { } file)
            return Path.GetFileNameWithoutExtension(file.Path);

        return Strings.UntitledDocumentName;
    }

    private async Task<bool> SaveToAsync(DocumentFile file)
    {
        var session = activeDocument.Session;

        // The document is read before SaveAsync first awaits, so this is exactly the state saved;
        // anything typed while the file is written stays unsaved.
        var savePoint = session.History.BeginSave();
        try
        {
            await DocumentFileFormat.SaveAsync(session.Document, file.Path, file.Variant);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogCouldNotSave(exception, file.Path);
            await shell.ShowMessageAsync(Strings.SaveFailedTitle,
                Format(Strings.SaveFailedFormat, file.FileName, exception.Message));
            return false;
        }

        session.History.MarkSaved(savePoint);
        if (ReferenceEquals(activeDocument.Session, session))
            activeDocument.SetFile(file);

        return true;
    }

    private static string DescribeOpenFailure(Exception exception, string path)
    {
        var fileName = Path.GetFileName(path);
        return exception switch
        {
            DocumentFileException { Problem: DocumentFileProblem.NotADocument } =>
                Format(Strings.OpenFailedNotADocumentFormat, fileName),
            DocumentFileException { Problem: DocumentFileProblem.NewerVersion } newer =>
                Format(Strings.OpenFailedNewerVersionFormat, fileName, newer.FileVersion),
            DocumentFileException => Format(Strings.OpenFailedDamagedFormat, fileName),
            _ => Format(Strings.OpenFailedUnreadableFormat, fileName, exception.Message)
        };
    }

    private static string Format(string format, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, format, arguments);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not open {FilePath}")]
    private partial void LogCouldNotOpen(Exception exception, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not save {FilePath}")]
    private partial void LogCouldNotSave(Exception exception, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not export {FilePath}")]
    private partial void LogCouldNotExport(Exception exception, string filePath);
}
