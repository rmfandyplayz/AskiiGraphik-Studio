// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Docking.Layouts;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AskiiArt.App.Shell.Docking;

internal sealed partial class ToolbarFrame : UserControl
{
    private ToolbarOrientation _orientation = ToolbarOrientation.Horizontal;
    private ToolbarFadeAmounts _fades;

    public ToolbarFrame()
    {
        InitializeComponent();
        Scroller.PointerWheelChanged += OnWheel;
        Scroller.ScrollChanged += (_, _) => UpdateFades();
        Scroller.SizeChanged += (_, _) => UpdateFades();
    }

    /// <summary>The buttons. A <see cref="StackPanel"/> is turned to run the toolbar's way.</summary>
    public Control? Bar
    {
        get => Scroller.Content as Control;
        set
        {
            Scroller.Content = value;
            TurnBar();
        }
    }

    /// <summary>How far one wheel notch scrolls.</summary>
    public double WheelStep { get; init; }

    /// <summary>How long the fade is on a side with more to scroll to.</summary>
    public double FadeLength { get; init; }

    /// <summary>What starts a drag.</summary>
    public IReadOnlyList<Control> Grips => [StartGrip, EndGrip];

    /// <summary>Whether it's following the pointer, which keeps its grips showing.</summary>
    public bool IsDragged
    {
        get => Classes.Contains("dragged");
        set => Classes.Set("dragged", value);
    }

    /// <summary>Which way the buttons run and scroll.</summary>
    public ToolbarOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value)
                return;

            _orientation = value;
            Turn();
        }
    }

    private bool IsHorizontal => _orientation == ToolbarOrientation.Horizontal;

    private void Turn()
    {
        Scroller.HorizontalScrollBarVisibility =
            IsHorizontal ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
        Scroller.VerticalScrollBarVisibility = IsHorizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden;
        Scroller.Offset = default;

        DockPanel.SetDock(StartGrip, IsHorizontal ? Dock.Left : Dock.Top);
        DockPanel.SetDock(EndGrip, IsHorizontal ? Dock.Right : Dock.Bottom);
        StartGrip.ToolbarDirection = _orientation;
        EndGrip.ToolbarDirection = _orientation;
        TurnBar();

        _fades = default;
        Scroller.OpacityMask = null;
        UpdateFades();

        // Turning changes the size of what's inside, but those parts still count as measured until their turn later
        // in the layout pass. The host asks for this frame's size before that, and would get the one it had the
        // other way round. A frame that's always asked with the same limits is the one that shows it: a dragged one.
        foreach (var part in this.GetVisualDescendants().OfType<Layoutable>())
            part.InvalidateMeasure();

        InvalidateMeasure();
    }

    private void TurnBar()
    {
        var direction = IsHorizontal ? Avalonia.Layout.Orientation.Horizontal : Avalonia.Layout.Orientation.Vertical;
        if (Bar is StackPanel stack)
            stack.Orientation = direction;
    }

    private void UpdateFades()
    {
        var fades = IsHorizontal
            ? ToolbarFades.For(Scroller.Offset.X, Scroller.Extent.Width, Scroller.Viewport.Width, FadeLength)
            : ToolbarFades.For(Scroller.Offset.Y, Scroller.Extent.Height, Scroller.Viewport.Height, FadeLength);

        if (fades == _fades)
            return;

        _fades = fades;
        Scroller.OpacityMask = ToolbarFades.ToMask(fades, IsHorizontal);
    }

    // A mouse's wheel only turns one way, so on a horizontal toolbar it has to scroll sideways. The scroller
    // handles a sideways wheel or trackpad itself, and any wheel on a vertical toolbar, so this only sees the rest.
    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!IsHorizontal)
            return;

        var offset = Scroller.Offset;
        Scroller.Offset = offset.WithX(offset.X - e.Delta.Y * WheelStep);
        e.Handled = true;
    }
}
