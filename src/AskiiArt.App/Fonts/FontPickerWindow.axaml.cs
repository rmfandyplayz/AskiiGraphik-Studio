// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Fonts;

/// <summary>
/// Shown with <c>ShowDialog&lt;FontReference?&gt;</c>: the picked font when the user clicks OK,
/// null when they cancel or close the dialog.
/// </summary>
internal sealed partial class FontPickerWindow : Window
{
    public FontPickerWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SearchBox.Focus();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close((DataContext as FontPickerViewModel)?.Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
