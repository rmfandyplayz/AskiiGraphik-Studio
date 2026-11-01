// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Documents;

/// <summary>
/// Shown with <c>ShowDialog&lt;SaveChangesChoice&gt;</c>. Save is the default button; Esc and the
/// close button cancel, so nothing is lost by accident.
/// </summary>
internal sealed partial class SaveChangesWindow : Window
{
    public SaveChangesWindow()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChangesChoice.Save);
    }

    private void OnDontSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChangesChoice.DontSave);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(SaveChangesChoice.Cancel);
    }
}
