// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;

namespace AskiiArt.App.Canvas;

internal sealed partial class CanvasView : UserControl
{
    private Thickness _coveredByWidgets;

    public CanvasView()
    {
        InitializeComponent();
        LayoutUpdated += (_, _) => UpdateCanvasSafeArea();
    }

    /// <summary>Gives the canvas keyboard focus, so Space + drag works without clicking first.</summary>
    public void FocusCanvas() => Canvas.Focus();

    /// <summary>Tells the canvas how much of each edge docked widgets cover, in logical pixels (ADR 0025).</summary>
    public void SetCoveredByWidgets(Thickness covered)
    {
        if (covered == _coveredByWidgets)
            return;

        _coveredByWidgets = covered;
        UpdateCanvasSafeArea();
    }

    // The scrollbars sit just inside the widgets, and the canvas keeps clear of them as well.
    private void UpdateCanvasSafeArea()
    {
        var covered = _coveredByWidgets;
        var barWidth = VerticalBar.Bounds.Width;
        var barHeight = HorizontalBar.Bounds.Height;

        VerticalBar.Margin = new Thickness(0, covered.Top, covered.Right, covered.Bottom + barHeight);
        HorizontalBar.Margin = new Thickness(covered.Left, 0, covered.Right + barWidth, covered.Bottom);
        Canvas.SafeArea =
            new Thickness(covered.Left, covered.Top, covered.Right + barWidth, covered.Bottom + barHeight);
    }
}
