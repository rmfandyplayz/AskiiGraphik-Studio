// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Documents;

/// <summary>
/// Shown with <c>ShowDialog&lt;NewDocumentSettings?&gt;</c>: the settings when the user clicks OK,
/// null when they cancel or close the dialog.
/// </summary>
internal sealed partial class NewDocumentWindow : Window
{
    public NewDocumentWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SizeFields.FocusWidth();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close((DataContext as NewDocumentViewModel)?.Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
