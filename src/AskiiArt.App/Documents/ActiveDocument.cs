// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Editing;

namespace AskiiArt.App.Documents;

/// <summary>
/// The document the window is editing, with its history. New replaces it, so everything that
/// works on "the document" (the canvas, Undo, the document actions) reads <see cref="Session"/>
/// when it needs it and follows <see cref="SessionChanged"/>, rather than keeping one.
/// </summary>
internal sealed class ActiveDocument
{
    public ActiveDocument(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
    }

    /// <summary>Raised after <see cref="Session"/> is replaced.</summary>
    public event EventHandler? SessionChanged;

    public EditingSession Session { get; private set; }

    /// <summary>Starts editing another document, with its own history.</summary>
    public void Replace(EditingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (ReferenceEquals(session, Session))
            return;

        Session = session;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
