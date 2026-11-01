// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Dialogs;

/// <summary>Tells the user something. OK, Enter, Esc and the close button all close it.</summary>
internal sealed partial class MessageWindow : Window
{
    public MessageWindow()
    {
        InitializeComponent();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
