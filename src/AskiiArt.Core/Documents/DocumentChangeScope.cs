// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Ends a group of changes started by <see cref="Document.BeginChanges"/>. Use it with
/// <c>using</c>, so the group ends even if a change throws.
/// </summary>
internal readonly struct DocumentChangeScope : IDisposable
{
    private readonly DocumentChangeTracker _tracker;

    public DocumentChangeScope(DocumentChangeTracker tracker)
    {
        _tracker = tracker;
    }

    public void Dispose() => _tracker.End();
}
