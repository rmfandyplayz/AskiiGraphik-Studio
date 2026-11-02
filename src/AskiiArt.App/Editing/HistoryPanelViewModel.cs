// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Collections.ObjectModel;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Editing;

/// <summary>
/// The History panel's content: a row for the start of the history, then one per undo step, oldest
/// first. The current row is where the document stands, and choosing another one jumps there. It
/// lives as long as the app, so reopening or floating the panel shows the same state, and it follows
/// the active document's history.
/// </summary>
internal sealed class HistoryPanelViewModel : ObservableObject
{
    private readonly ActiveDocument _activeDocument;
    private CommandHistory _followedHistory;

    public HistoryPanelViewModel(ActiveDocument activeDocument)
    {
        ArgumentNullException.ThrowIfNull(activeDocument);

        _activeDocument = activeDocument;
        _followedHistory = activeDocument.Session.History;
        _followedHistory.StateChanged += OnHistoryChanged;
        activeDocument.SessionChanged += OnSessionChanged;

        Rows.Add(new HistoryRow(Strings.HistoryStart));
        SyncRows();
    }

    public ObservableCollection<HistoryRow> Rows { get; } = [];

    /// <summary>
    /// The row where the document stands. Setting it jumps there. A list briefly reports no selection
    /// (-1) while its rows change, which isn't a request to go anywhere.
    /// </summary>
    public int CurrentRow
    {
        get => _followedHistory.UndoCount;
        set
        {
            if (value >= 0 && value <= _followedHistory.StepCount)
                _followedHistory.JumpTo(value);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _followedHistory.StateChanged -= OnHistoryChanged;
        _followedHistory = _activeDocument.Session.History;
        _followedHistory.StateChanged += OnHistoryChanged;
        SyncRows();
    }

    private void OnHistoryChanged(object? sender, EventArgs e) => SyncRows();

    // Rows are reused rather than replaced, because most changes only touch the end of the list and
    // replacing a row would lose the list's selection and scroll position.
    private void SyncRows()
    {
        var stepCount = _followedHistory.StepCount;

        while (Rows.Count > stepCount + 1)
            Rows.RemoveAt(Rows.Count - 1);

        for (var step = 0; step < stepCount; step++)
        {
            var name = UiText.Get(_followedHistory.GetStepNameKey(step));
            var isUndone = step >= _followedHistory.UndoCount;

            if (step + 1 == Rows.Count)
                Rows.Add(new HistoryRow(name) { IsUndone = isUndone });
            else
                Rows[step + 1].Update(name, isUndone);
        }

        OnPropertyChanged(nameof(CurrentRow));
    }
}
