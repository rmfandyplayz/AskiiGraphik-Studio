// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Documents;

/// <summary>
/// Shown with <c>ShowDialog&lt;CanvasResizeRequest?&gt;</c>: the resize when the user clicks OK,
/// null when they cancel or close the dialog.
/// </summary>
internal sealed partial class ResizeCanvasWindow : Window
{
    public ResizeCanvasWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SizeFields.FocusWidth();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close((DataContext as ResizeCanvasViewModel)?.Result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
