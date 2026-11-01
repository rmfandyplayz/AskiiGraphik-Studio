// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>
/// Shown with <c>ShowDialog&lt;bool&gt;</c>: true only when the user confirms. Cancel is the
/// default button, and closing the dialog any other way (the close button, Esc) also cancels.
/// </summary>
internal sealed partial class ConfirmWindow : Window
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
