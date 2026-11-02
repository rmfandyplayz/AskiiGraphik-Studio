// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Formats;

/// <summary>
/// Writes whole files so that nobody ever sees half of one (ADR 0021). The bytes go to a temporary
/// file in the same folder, which then replaces the target, so a failure leaves any old file as it was.
/// </summary>
public static class AtomicFile
{
    /// <summary>Writes <paramref name="contents"/> to <paramref name="path"/>, replacing any file there.</summary>
    /// <exception cref="IOException">The file couldn't be written, for example because the disk is full.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder or the old file can't be written to.</exception>
    public static async Task WriteAsync(string path, ReadOnlyMemory<byte> contents,
        CancellationToken cancellationToken = default)
    {
        var (fullPath, temporaryPath) = Paths(path);
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, bufferSize: 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(contents, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// The same, without waiting on anything else: for when the app is shutting down and an
    /// unfinished asynchronous write would be lost.
    /// </summary>
    /// <exception cref="IOException">The file couldn't be written, for example because the disk is full.</exception>
    /// <exception cref="UnauthorizedAccessException">The folder or the old file can't be written to.</exception>
    public static void Write(string path, ReadOnlySpan<byte> contents)
    {
        var (fullPath, temporaryPath) = Paths(path);
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(contents);
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

    private static (string FullPath, string TemporaryPath) Paths(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(fullPath)
                     ?? throw new ArgumentException("The path has no folder.", nameof(path));

        // In the same folder, so replacing the old file is a rename on the same disk.
        return (fullPath, Path.Combine(folder, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp"));
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
