// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Documents;
using AskiiArt.App.Resources;
using AskiiArt.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Shell;

/// <summary>
/// The main window's title: the document's file name (or "Untitled"), marked while it has
/// unsaved changes, then the app's name.
/// </summary>
internal sealed class WindowTitle : ObservableObject
{
    private readonly ActiveDocument _activeDocument;
    private CommandHistory _followedHistory;

    public WindowTitle(ActiveDocument activeDocument)
    {
        ArgumentNullException.ThrowIfNull(activeDocument);

        _activeDocument = activeDocument;
        _followedHistory = activeDocument.Session.History;
        _followedHistory.StateChanged += OnTitleInputChanged;
        activeDocument.FileChanged += OnTitleInputChanged;
        activeDocument.SessionChanged += OnSessionChanged;
    }

    public string Text
    {
        get
        {
            var format = Strings.WindowTitleFormat;
            if (_followedHistory.HasUnsavedChanges)
                format = Strings.WindowTitleUnsavedFormat;

            return string.Format(CultureInfo.CurrentCulture, format, _activeDocument.DisplayName,
                Strings.AppDisplayName);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _followedHistory.StateChanged -= OnTitleInputChanged;
        _followedHistory = _activeDocument.Session.History;
        _followedHistory.StateChanged += OnTitleInputChanged;
        OnPropertyChanged(nameof(Text));
    }

    private void OnTitleInputChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(Text));
}
