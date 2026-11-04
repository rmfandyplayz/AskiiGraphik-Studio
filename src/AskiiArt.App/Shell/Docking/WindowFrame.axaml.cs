// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Shell.Docking;

internal sealed partial class WindowFrame : UserControl
{
    public WindowFrame()
    {
        InitializeComponent();
    }

    /// <summary>The name at the top.</summary>
    public string? Title
    {
        get => TitleText.Text;
        init => TitleText.Text = value;
    }

    /// <summary>The window's view, which doesn't know it's in a frame.</summary>
    public Control? Body
    {
        get => BodyHost.Content as Control;
        init => BodyHost.Content = value;
    }

    /// <summary>Whether only the title shows.</summary>
    public bool IsTitleOnly
    {
        get => !BodyHost.IsVisible;
        set => BodyHost.IsVisible = !value;
    }
}
