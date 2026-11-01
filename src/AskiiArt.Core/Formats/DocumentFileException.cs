// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Formats;

/// <summary>Why a file couldn't be opened as a document.</summary>
public enum DocumentFileProblem
{
    /// <summary>The file has some damage: a bad header, broken JSON, or values that don't fit together.</summary>
    Damaged,

    /// <summary>The file doesn't start like a document file, so it's something else entirely.</summary>
    NotADocument,

    /// <summary>A newer version of the app wrote the file, in a format version this one doesn't know.</summary>
    NewerVersion
}

/// <summary>
/// Thrown when a file can't be opened as a document. Reading the file itself can also throw the
/// usual I/O exceptions.
/// </summary>
public sealed class DocumentFileException : Exception
{
    /// <summary>Creates the exception for a damaged file.</summary>
    public DocumentFileException()
        : this(DocumentFileProblem.Damaged, "The document file is damaged.")
    {
    }

    /// <summary>Creates the exception for a damaged file, with <paramref name="message"/>.</summary>
    public DocumentFileException(string message)
        : this(DocumentFileProblem.Damaged, message)
    {
    }

    /// <summary>Creates the exception for a damaged file, with <paramref name="message"/> and its cause.</summary>
    public DocumentFileException(string message, Exception innerException)
        : base(message, innerException)
    {
        Problem = DocumentFileProblem.Damaged;
    }

    /// <summary>Creates the exception for <paramref name="problem"/>.</summary>
    /// <param name="problem">What's wrong with the file.</param>
    /// <param name="message">The details, in English, for the log.</param>
    /// <param name="fileVersion">The format version the file claims, when it has one.</param>
    public DocumentFileException(DocumentFileProblem problem, string message, int? fileVersion = null)
        : base(message)
    {
        Problem = problem;
        FileVersion = fileVersion;
    }

    /// <summary>What's wrong with the file.</summary>
    public DocumentFileProblem Problem { get; }

    /// <summary>The format version the file claims, or null when it doesn't get that far.</summary>
    public int? FileVersion { get; }
}
