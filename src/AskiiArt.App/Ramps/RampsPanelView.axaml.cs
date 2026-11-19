// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Ramps;

internal sealed partial class RampsPanelView : UserControl
{
    public RampsPanelView()
    {
        InitializeComponent();
    }

    /// <summary>The buttons below the list.</summary>
    public Control? Buttons
    {
        get => ButtonsHost.Content as Control;
        set => ButtonsHost.Content = value;
    }
}
