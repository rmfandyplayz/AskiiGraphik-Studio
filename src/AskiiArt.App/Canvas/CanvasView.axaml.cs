// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;

namespace AskiiArt.App.Canvas;

internal sealed partial class CanvasView : UserControl
{
    public CanvasView()
    {
        InitializeComponent();
    }

    /// <summary>Gives the canvas keyboard focus, so Space + drag works without clicking first.</summary>
    public void FocusCanvas() => Canvas.Focus();
}
