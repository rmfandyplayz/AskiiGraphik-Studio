// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell;

internal sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => CanvasView.FocusCanvas();
    }

    /// <summary>Puts the toolbar under the menu bar.</summary>
    public void SetToolbar(Control toolbar) => ToolbarHost.Content = toolbar;
}
