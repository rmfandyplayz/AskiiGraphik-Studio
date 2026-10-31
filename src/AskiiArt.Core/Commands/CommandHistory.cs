// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Commands;

/// <summary>
/// The undo and redo history of one document (ARCHITECTURE §9, ADR 0012). It runs every command,
/// merges commands of the same <see cref="CoalescingGroup"/>, stays within a memory budget, and
/// tracks whether the document has unsaved changes.
/// </summary>
public sealed class CommandHistory
{
    /// <summary>The default memory budget: 256 MB.</summary>
    public const long DefaultMemoryBudgetInBytes = 256L * 1024 * 1024;

    // Undo entries come first, oldest at index 0; entries from _position on are redo entries.
    private readonly List<Entry> _entries = [];
    private int _position;
    private int? _savedPosition = 0;
    private Entry? _mergeTarget;

    /// <summary>Creates an empty history. The document starts out with no unsaved changes.</summary>
    /// <param name="document">The document the commands change.</param>
    /// <param name="memoryBudgetInBytes">
    /// When the history holds more than this, the oldest steps are dropped. The newest step is
    /// always kept, even when it alone is over budget.
    /// </param>
    public CommandHistory(Document document, long memoryBudgetInBytes = DefaultMemoryBudgetInBytes)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentOutOfRangeException.ThrowIfNegative(memoryBudgetInBytes);

        Document = document;
        MemoryBudgetInBytes = memoryBudgetInBytes;
    }

    /// <summary>Raised after an execute, undo, redo or save changes what the history reports.</summary>
    public event EventHandler? StateChanged;

    /// <summary>The document the commands change.</summary>
    public Document Document { get; }

    /// <summary>The memory budget, in bytes.</summary>
    public long MemoryBudgetInBytes { get; }

    /// <summary>The total estimated memory of every step in the history, in bytes.</summary>
    public long MemoryInBytes { get; private set; }

    /// <summary>The number of steps that can be undone.</summary>
    public int UndoCount => _position;

    /// <summary>The number of steps that can be redone.</summary>
    public int RedoCount => _entries.Count - _position;

    /// <summary>True when there's a step to undo.</summary>
    public bool CanUndo => UndoCount > 0;

    /// <summary>True when there's a step to redo.</summary>
    public bool CanRedo => RedoCount > 0;

    /// <summary>
    /// True when the document differs from the last save (or from when the history was created).
    /// Undoing or redoing back to that point makes it false again.
    /// </summary>
    public bool HasUnsavedChanges => _savedPosition != _position;

    /// <summary>
    /// Applies <paramref name="command"/> and records it as a new step, dropping any redo steps.
    /// If <paramref name="group"/> is the group of the previous step, the command may merge into it.
    /// </summary>
    /// <remarks>If the command throws, the history is unchanged and the exception propagates.</remarks>
    public void Execute(IDocumentCommand command, CoalescingGroup? group = null)
    {
        ArgumentNullException.ThrowIfNull(command);

        using (Document.BeginChanges())
            command.Apply(Document);

        DropRedoEntries();

        if (!TryMergeIntoLastEntry(command, group))
        {
            var entry = new Entry(command, group);
            _entries.Add(entry);
            MemoryInBytes += entry.MemoryInBytes;
            _position++;
            _mergeTarget = entry;
        }

        DropOldestEntriesOverBudget();
        RaiseStateChanged();
    }

    /// <summary>Undoes the newest step.</summary>
    /// <exception cref="InvalidOperationException">There's nothing to undo.</exception>
    public void Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("There's nothing to undo.");

        using (Document.BeginChanges())
            _entries[_position - 1].Command.Revert(Document);

        _position--;
        _mergeTarget = null;
        RaiseStateChanged();
    }

    /// <summary>Redoes the most recently undone step.</summary>
    /// <exception cref="InvalidOperationException">There's nothing to redo.</exception>
    public void Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("There's nothing to redo.");

        using (Document.BeginChanges())
            _entries[_position].Command.Apply(Document);

        _position++;
        _mergeTarget = null;
        RaiseStateChanged();
    }

    /// <summary>Records that the document was just saved, so it has no unsaved changes.</summary>
    public void MarkSaved()
    {
        _savedPosition = _position;

        // Merging into the saved step would change the document without making it unsaved.
        _mergeTarget = null;
        RaiseStateChanged();
    }

    private bool TryMergeIntoLastEntry(IDocumentCommand command, CoalescingGroup? group)
    {
        if (group is null || _mergeTarget is null || _position == 0 || _entries[_position - 1] != _mergeTarget)
            return false;

        if (_mergeTarget.Group != group || _mergeTarget.Command is not IMergeableCommand mergeable)
            return false;

        if (!mergeable.TryMergeWith(command, out var merged))
            return false;

        var entry = new Entry(merged, group);
        MemoryInBytes += entry.MemoryInBytes - _mergeTarget.MemoryInBytes;
        _entries[_position - 1] = entry;
        _mergeTarget = entry;
        return true;
    }

    private void DropRedoEntries()
    {
        if (!CanRedo)
            return;

        if (_savedPosition > _position)
            _savedPosition = null;

        for (var index = _position; index < _entries.Count; index++)
            MemoryInBytes -= _entries[index].MemoryInBytes;

        _entries.RemoveRange(_position, _entries.Count - _position);
    }

    private void DropOldestEntriesOverBudget()
    {
        var dropCount = 0;
        var remainingMemory = MemoryInBytes;
        while (remainingMemory > MemoryBudgetInBytes && _entries.Count - dropCount > 1)
        {
            remainingMemory -= _entries[dropCount].MemoryInBytes;
            dropCount++;
        }

        if (dropCount == 0)
            return;

        _entries.RemoveRange(0, dropCount);
        MemoryInBytes = remainingMemory;
        _position -= dropCount;

        // A saved state before the dropped steps can never be reached by undoing again.
        if (_savedPosition is { } savedPosition)
            _savedPosition = savedPosition >= dropCount ? savedPosition - dropCount : null;
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    private sealed class Entry(IDocumentCommand command, CoalescingGroup? group)
    {
        public IDocumentCommand Command { get; } = command;

        public CoalescingGroup? Group { get; } = group;

        // Read once, after the command was applied, so the running total stays consistent.
        public long MemoryInBytes { get; } = command.EstimatedMemoryInBytes;
    }
}
