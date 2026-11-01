// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Formats;

namespace AskiiArt.App.Documents;

/// <summary>Where a document is saved, and which variant of the file format it uses.</summary>
internal sealed record DocumentFile(string Path, DocumentFileVariant Variant)
{
    /// <summary>The file's name with its extension, as the window title and dialogs show it.</summary>
    public string FileName => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// The file a save dialog's answer means. A name ending in one of the two extensions picks
    /// that variant whatever file type was chosen; any other name uses the chosen type, and a name
    /// with no extension gets that type's extension (some Linux dialogs don't add it).
    /// </summary>
    public static DocumentFile ForSavePath(string path, DocumentFileVariant chosenVariant)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var extension = System.IO.Path.GetExtension(path);
        if (extension.Equals(DocumentFileFormat.PlainExtension, StringComparison.OrdinalIgnoreCase))
            return new DocumentFile(path, DocumentFileVariant.Plain);

        if (extension.Equals(DocumentFileFormat.CompressedExtension, StringComparison.OrdinalIgnoreCase))
            return new DocumentFile(path, DocumentFileVariant.Compressed);

        if (extension.Length == 0)
            path += ExtensionOf(chosenVariant);

        return new DocumentFile(path, chosenVariant);
    }

    /// <summary>The file extension for <paramref name="variant"/>, with its dot.</summary>
    public static string ExtensionOf(DocumentFileVariant variant) => variant switch
    {
        DocumentFileVariant.Plain => DocumentFileFormat.PlainExtension,
        DocumentFileVariant.Compressed => DocumentFileFormat.CompressedExtension,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
    };
}
