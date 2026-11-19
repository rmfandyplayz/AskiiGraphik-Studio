// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Characters;

internal sealed partial class CharactersPanelView : UserControl
{
    public CharactersPanelView()
    {
        InitializeComponent();
    }

    /// <summary>The buttons above the dropdown.</summary>
    public Control? Buttons
    {
        get => ButtonsHost.Content as Control;
        set => ButtonsHost.Content = value;
    }
}
