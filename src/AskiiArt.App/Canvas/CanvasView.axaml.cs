// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
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
    private CanvasViewModel? _followedCanvas;

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

    // A paste partly out of view comes to the middle, if it fits (ADR 0072).
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_followedCanvas is not null)
            _followedCanvas.Pasted -= OnPasted;

        _followedCanvas = DataContext as CanvasViewModel;
        if (_followedCanvas is not null)
            _followedCanvas.Pasted += OnPasted;
    }

    /// <summary>Gives the canvas keyboard focus, so Space + drag works without clicking first.</summary>
    public void FocusCanvas() => Canvas.Focus();

    /// <summary>Tells the canvas how much of each edge windows and toolbars cover, in logical pixels (ADR 0035).</summary>
    public void SetCoveredByWidgets(Thickness covered)
    {
        if (covered == _coveredByWidgets)
            return;

        _coveredByWidgets = covered;
        UpdateCanvasSafeArea();
    }

    // Here rather than as action shortcuts, which would fire while typing in any text box. It runs
    // only for keys the canvas itself didn't use, so Esc during a stroke only cancels the stroke, and
    // Delete and Backspace at the Text tool's caret edit the text rather than the selection.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || e.KeyModifiers != KeyModifiers.None || DataContext is not CanvasViewModel canvas)
            return;

        if (e.Key == Key.Escape)
            e.Handled = canvas.TrySwitchToHandTool();
        else if (e.Key is Key.Delete or Key.Back)
            e.Handled = canvas.TryDeleteSelectedCells();
        else if (e.KeySymbol is { Length: 1 } letter)
            e.Handled = canvas.TryUseShortcutLetter(letter[0]);
    }

    // Typed text rather than keys, so a number key works on keyboards where the digit needs Shift. It runs only for
    // text the canvas's tool didn't take, so typing a number with the Text tool types it.
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!e.Handled && e.Text is { } text && DataContext is CanvasViewModel canvas)
            e.Handled = canvas.TryUseNumberKey(text);
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

    private void OnPasted(object? sender, CellRect cells) => Canvas.CentreOnIfPartlyHidden(cells);

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
