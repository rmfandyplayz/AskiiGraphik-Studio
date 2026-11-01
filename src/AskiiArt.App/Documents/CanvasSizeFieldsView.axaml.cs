// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Documents;

internal sealed partial class CanvasSizeFieldsView : UserControl
{
    public CanvasSizeFieldsView()
    {
        InitializeComponent();
    }

    /// <summary>Puts the cursor in the width box, so the dialog can be used from the keyboard straight away.</summary>
    public void FocusWidth() => WidthBox.Focus();
}
