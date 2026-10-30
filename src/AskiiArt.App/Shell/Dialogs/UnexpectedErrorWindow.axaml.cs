// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>
/// Shown with <c>ShowDialog&lt;UnexpectedErrorChoice&gt;</c>. Closing it any other way (the title
/// bar's close button, Esc) returns the default, <see cref="UnexpectedErrorChoice.Continue"/>.
/// </summary>
internal sealed partial class UnexpectedErrorWindow : Window
{
    public UnexpectedErrorWindow()
    {
        InitializeComponent();
    }

    private void OnQuitClick(object? sender, RoutedEventArgs e)
    {
        Close(UnexpectedErrorChoice.Quit);
    }

    private void OnContinueClick(object? sender, RoutedEventArgs e)
    {
        Close(UnexpectedErrorChoice.Continue);
    }
}
