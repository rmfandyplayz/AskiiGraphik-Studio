// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace AskiiArt.App.Canvas;

internal sealed partial class CanvasView : UserControl
{
    /// <summary>Whether the scrollbars show. They're off for now (see docs/TEMPORARY-UI.md).</summary>
    public static readonly StyledProperty<bool> ShowScrollBarsProperty =
        AvaloniaProperty.Register<CanvasView, bool>(nameof(ShowScrollBars));

    private Thickness _coveredByWidgets;

    public CanvasView()
    {
        InitializeComponent();
        LayoutUpdated += (_, _) => UpdateCanvasSafeArea();
        UpdateScrollBarVisibility();
    }

    public bool ShowScrollBars
    {
        get => GetValue(ShowScrollBarsProperty);
        set => SetValue(ShowScrollBarsProperty, value);
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

    // Here rather than as action shortcuts, which would fire while typing in any text box. It runs
    // only for keys the canvas itself didn't use.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.KeyModifiers == KeyModifiers.None && e.KeySymbol is { Length: 1 } letter &&
            DataContext is CanvasViewModel canvas && canvas.TrySelectToolByLetter(letter[0]))
        {
            e.Handled = true;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShowScrollBarsProperty)
        {
            UpdateScrollBarVisibility();
            UpdateCanvasSafeArea();
        }
    }

    // A ScrollBar sets its own IsVisible from this property, so binding IsVisible wouldn't hold.
    private void UpdateScrollBarVisibility()
    {
        var visibility = ShowScrollBars ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
        VerticalBar.Visibility = visibility;
        HorizontalBar.Visibility = visibility;
    }

    // The scrollbars sit just inside the widgets, and the canvas keeps clear of them as well.
    private void UpdateCanvasSafeArea()
    {
        var covered = _coveredByWidgets;
        var barWidth = ShowScrollBars ? VerticalBar.Bounds.Width : 0;
        var barHeight = ShowScrollBars ? HorizontalBar.Bounds.Height : 0;

        VerticalBar.Margin = new Thickness(0, covered.Top, covered.Right, covered.Bottom + barHeight);
        HorizontalBar.Margin = new Thickness(covered.Left, 0, covered.Right + barWidth, covered.Bottom);
        Canvas.SafeArea =
            new Thickness(covered.Left, covered.Top, covered.Right + barWidth, covered.Bottom + barHeight);
    }
}
