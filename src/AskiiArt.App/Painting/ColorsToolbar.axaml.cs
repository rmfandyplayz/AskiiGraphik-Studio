// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Painting;

/// <summary>The Colors toolbar: the FG and BG colours, and which of them the colour windows change.</summary>
internal sealed partial class ColorsToolbar : StackPanel
{
    public ColorsToolbar()
    {
        InitializeComponent();
    }
}
