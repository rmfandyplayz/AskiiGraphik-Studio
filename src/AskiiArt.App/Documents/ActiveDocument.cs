// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.Core.Editing;

namespace AskiiArt.App.Documents;

/// <summary>
/// The document the window is editing, with its history and the file it's saved in. New and Open
/// replace it, so everything that works on "the document" (the canvas, Undo, the document
/// actions, the window title) reads <see cref="Session"/> when it needs it and follows
/// <see cref="SessionChanged"/>, rather than keeping one.
/// </summary>
internal sealed class ActiveDocument
{
    public ActiveDocument(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
    }

    /// <summary>Raised after <see cref="Session"/> is replaced (and <see cref="File"/> with it).</summary>
    public event EventHandler? SessionChanged;

    /// <summary>Raised after Save As gives the same document another <see cref="File"/>.</summary>
    public event EventHandler? FileChanged;

    public EditingSession Session { get; private set; }

    /// <summary>Where the document is saved, or null when it's never been saved.</summary>
    public DocumentFile? File { get; private set; }

    /// <summary>The file's name, or "Untitled" for a document that's never been saved.</summary>
    public string DisplayName => File?.FileName ?? Strings.UntitledDocumentName;

    /// <summary>Starts editing another document, with its own history.</summary>
    /// <param name="session">The document to edit.</param>
    /// <param name="file">The file it was opened from, or null for a new document.</param>
    public void Replace(EditingSession session, DocumentFile? file = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (ReferenceEquals(session, Session))
            return;

        Session = session;
        File = file;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Records that the current document is now saved in <paramref name="file"/>.</summary>
    public void SetFile(DocumentFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file == File)
            return;

        File = file;
        FileChanged?.Invoke(this, EventArgs.Empty);
    }
}
