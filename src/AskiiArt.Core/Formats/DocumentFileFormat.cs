// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Formats;

/// <summary>A document read from a file, with how the file stored it.</summary>
/// <param name="Document">The document.</param>
/// <param name="Variant">Plain or compressed, from the file's header (not its name).</param>
public sealed record DocumentFileContents(Document Document, DocumentFileVariant Variant);

/// <summary>
/// The document file format (ARCHITECTURE §13, ADR 0021). Both variants hold the same JSON:
/// <list type="bullet">
/// <item><b>plain:</b> <c>ASKIIART &lt;version&gt;\n</c>, then UTF-8 JSON;</item>
/// <item><b>compressed:</b> 8 magic bytes, the version as a little-endian 32-bit number, then
/// Brotli-compressed UTF-8 JSON.</item>
/// </list>
/// Opening sniffs the header, so a file opens whatever it's called. Saving is atomic: the new file
/// is written next to the old one and then replaces it, so a failed save never leaves half a file.
/// </summary>
public static class DocumentFileFormat
{
    /// <summary>The format version this app writes, and the newest it can read.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The plain variant's file extension. It may change with the product name.</summary>
    public const string PlainExtension = ".asciiart";

    /// <summary>The compressed variant's file extension, like <c>.svgz</c> for <c>.svg</c>.</summary>
    public const string CompressedExtension = ".asciiartz";

    // Bigger files are refused rather than read into memory: the largest canvas with a few
    // layers is far smaller, so it's damage or something that isn't a document.
    internal const int MaximumJsonBytes = 1 << 30;

    private const string PlainHeaderStart = "ASKIIART ";
    private const int MaximumVersionDigits = 9;

    // Like PNG's: the first byte isn't text, and the line breaks show up damage from tools that
    // convert line endings.
    private static ReadOnlySpan<byte> CompressedMagic =>
        [0x89, (byte)'A', (byte)'S', (byte)'K', (byte)'Z', 0x0D, 0x0A, 0x1A];

    private static ReadOnlySpan<byte> Utf8ByteOrderMark => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// The variant a file name asks for: compressed for <see cref="CompressedExtension"/>, otherwise plain.
    /// </summary>
    public static DocumentFileVariant VariantForPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (Path.GetExtension(path).Equals(CompressedExtension, StringComparison.OrdinalIgnoreCase))
            return DocumentFileVariant.Compressed;

        return DocumentFileVariant.Plain;
    }

    /// <summary>The whole file for <paramref name="document"/>.</summary>
    public static byte[] Encode(Document document, DocumentFileVariant variant)
    {
        ArgumentNullException.ThrowIfNull(document);

        return EncodeJson(DocumentJson.Write(document), variant);
    }

    /// <summary>Reads a whole file, sniffing its variant, and migrates older format versions.</summary>
    /// <exception cref="DocumentFileException">The file isn't a document this app can open.</exception>
    public static DocumentFileContents Decode(ReadOnlySpan<byte> file) =>
        Decode(file, CurrentVersion, FormatMigrations.All);

    /// <summary>
    /// Saves <paramref name="document"/> to <paramref name="path"/>, replacing any file there only
    /// once the new one is complete. Call it on the thread that changes the document (the UI
    /// thread): the document is read before the first await, and compressing and writing happen
    /// in the background.
    /// </summary>
    public static async Task SaveAsync(Document document, string path, DocumentFileVariant variant,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var json = DocumentJson.Write(document);
        var file = await Task.Run(() => EncodeJson(json, variant), cancellationToken).ConfigureAwait(false);
        await WriteAtomicallyAsync(path, file, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Opens the document file at <paramref name="path"/>.</summary>
    /// <exception cref="DocumentFileException">The file isn't a document this app can open.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The app isn't allowed to read the file.</exception>
    public static async Task<DocumentFileContents> OpenAsync(string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (new FileInfo(path) is { Exists: true, Length: > MaximumJsonBytes })
        {
            throw new DocumentFileException(DocumentFileProblem.NotADocument,
                "The file is far too big to be a document.");
        }

        var file = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return await Task.Run(() => Decode(file), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// <see cref="Decode(ReadOnlySpan{byte})"/> with a different current version and migrations,
    /// so tests can try a format change before one exists.
    /// </summary>
    internal static DocumentFileContents Decode(ReadOnlySpan<byte> file, int currentVersion,
        IReadOnlyList<IFormatMigration> migrations)
    {
        var (variant, version, json) = Unwrap(file);
        if (version > currentVersion)
        {
            throw new DocumentFileException(DocumentFileProblem.NewerVersion,
                $"The file uses format version {version}; this app knows up to {currentVersion}.", version);
        }

        var root = DocumentJson.Parse(json);
        FormatMigrations.Apply(root, version, currentVersion, migrations);
        return new DocumentFileContents(DocumentJson.Read(root), variant);
    }

    private static byte[] EncodeJson(byte[] json, DocumentFileVariant variant)
    {
        using var file = new MemoryStream();
        if (variant == DocumentFileVariant.Plain)
        {
            file.Write(Encoding.ASCII.GetBytes($"{PlainHeaderStart}{CurrentVersion}\n"));
            file.Write(json);
            return file.ToArray();
        }

        if (variant != DocumentFileVariant.Compressed)
            throw new ArgumentOutOfRangeException(nameof(variant), variant, null);

        file.Write(CompressedMagic);
        Span<byte> version = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(version, CurrentVersion);
        file.Write(version);
        using (var brotli = new BrotliStream(file, CompressionLevel.Optimal, leaveOpen: true))
            brotli.Write(json);

        return file.ToArray();
    }

    private static (DocumentFileVariant Variant, int Version, byte[] Json) Unwrap(ReadOnlySpan<byte> file)
    {
        if (file.StartsWith(CompressedMagic))
        {
            var afterMagic = file[CompressedMagic.Length..];
            if (afterMagic.Length < sizeof(int))
                throw new DocumentFileException("The compressed header is cut short.");

            var version = BinaryPrimitives.ReadInt32LittleEndian(afterMagic);
            ThrowIfImpossible(version);
            return (DocumentFileVariant.Compressed, version, Decompress(afterMagic[sizeof(int)..]));
        }

        // A text editor may have added a byte-order mark when the plain file was edited.
        if (file.StartsWith(Utf8ByteOrderMark))
            file = file[Utf8ByteOrderMark.Length..];

        if (!file.StartsWith(Encoding.ASCII.GetBytes(PlainHeaderStart)))
        {
            throw new DocumentFileException(DocumentFileProblem.NotADocument,
                "The file doesn't start with a document header.");
        }

        var (plainVersion, headerLength) = ReadPlainVersion(file[PlainHeaderStart.Length..]);
        return (DocumentFileVariant.Plain, plainVersion, file[(PlainHeaderStart.Length + headerLength)..].ToArray());
    }

    // The version digits and the line break after them (a text editor may have made it \r\n).
    private static (int Version, int Length) ReadPlainVersion(ReadOnlySpan<byte> afterHeaderStart)
    {
        var digits = 0;
        var version = 0;
        while (digits < Math.Min(afterHeaderStart.Length, MaximumVersionDigits)
               && char.IsAsciiDigit((char)afterHeaderStart[digits]))
        {
            version = (version * 10) + (afterHeaderStart[digits] - '0');
            digits++;
        }

        var lineBreakLength = LineBreakLength(afterHeaderStart[digits..]);
        if (digits == 0 || lineBreakLength == 0)
            throw new DocumentFileException("The header's version is missing or broken.");

        ThrowIfImpossible(version);
        return (version, digits + lineBreakLength);
    }

    private static int LineBreakLength(ReadOnlySpan<byte> text)
    {
        if (text.StartsWith("\r\n"u8))
            return 2;

        if (text.StartsWith("\n"u8))
            return 1;

        return 0;
    }

    private static void ThrowIfImpossible(int version)
    {
        if (version < 1)
            throw new DocumentFileException($"The header claims format version {version}, which never existed.");
    }

    private static byte[] Decompress(ReadOnlySpan<byte> compressed)
    {
        try
        {
            using var input = new MemoryStream(compressed.ToArray());
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = brotli.Read(chunk)) > 0)
            {
                // A small damaged or hostile file could otherwise expand to fill all memory.
                if (output.Length + read > MaximumJsonBytes)
                    throw new DocumentFileException("The compressed content is far too big.");

                output.Write(chunk, 0, read);
            }

            return output.ToArray();
        }
        // Brotli reports some damage as invalid data and some as an invalid operation.
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException)
        {
            throw new DocumentFileException("The compressed content is broken.", exception);
        }
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] file, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(fullPath)
                     ?? throw new ArgumentException("The path has no folder.", nameof(path));

        // In the same folder, so replacing the old file is a rename on the same disk.
        var temporaryPath = Path.Combine(folder, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, bufferSize: 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(file, cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Left behind; it's hidden on macOS and Linux and harmless everywhere.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
