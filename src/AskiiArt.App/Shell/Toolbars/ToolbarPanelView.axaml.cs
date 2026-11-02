// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Input;

namespace AskiiArt.App.Shell.Toolbars;

internal sealed partial class ToolbarPanelView : UserControl
{
    public ToolbarPanelView()
    {
        InitializeComponent();
        Scroller.PointerWheelChanged += OnWheel;
    }

    /// <summary>The buttons.</summary>
    public Control? Bar
    {
        get => Scroller.Content as Control;
        set => Scroller.Content = value;
    }

    /// <summary>How far one wheel notch scrolls.</summary>
    public double WheelStep { get; init; }

    // A mouse's wheel only turns one way, and this bar only scrolls sideways. The scroller handles a
    // sideways wheel or trackpad itself, so this only sees the rest.
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        var offset = Scroller.Offset;
        Scroller.Offset = offset.WithX(offset.X - e.Delta.Y * WheelStep);
        e.Handled = true;
    }
}
