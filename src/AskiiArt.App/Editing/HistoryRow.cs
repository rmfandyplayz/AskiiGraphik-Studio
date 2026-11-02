// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Editing;

/// <summary>One row of the History panel.</summary>
internal sealed class HistoryRow(string name) : ObservableObject
{
    private string _name = name;
    private bool _isUndone;

    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    /// <summary>True for a step that has been undone and can be redone.</summary>
    public bool IsUndone
    {
        get => _isUndone;
        set => SetProperty(ref _isUndone, value);
    }

    public void Update(string name, bool isUndone)
    {
        Name = name;
        IsUndone = isUndone;
    }
}
