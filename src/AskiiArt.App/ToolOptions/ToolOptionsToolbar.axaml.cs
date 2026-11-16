// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.ToolOptions;

/// <summary>The Tool Options toolbar: the chosen tool's options, such as the brush size and shape (ADR 0047).</summary>
internal sealed partial class ToolOptionsToolbar : StackPanel
{
    public ToolOptionsToolbar()
    {
        InitializeComponent();
    }
}
