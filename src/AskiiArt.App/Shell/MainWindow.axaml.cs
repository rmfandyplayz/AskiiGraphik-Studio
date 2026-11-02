// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dock.Avalonia.Controls;
using Dock.Model.Core;

namespace AskiiArt.App.Shell;

internal sealed partial class MainWindow : Window
{
    private readonly ShellLayoutMetrics _metrics;
    private ToolDockControl? _canvasDock;

    public MainWindow()
    {
        InitializeComponent();
        _metrics = ShellLayoutMetrics.FromResources(Application.Current!, ActualThemeVariant);

        KeepMinimumSizeOnScreen();
        PositionChanged += (_, _) => KeepMinimumSizeOnScreen();
        ScalingChanged += (_, _) => KeepMinimumSizeOnScreen();
        Opened += (_, _) =>
        {
            KeepMinimumSizeOnScreen();
            CanvasView.FocusCanvas();
        };

        PanelDock.LayoutUpdated += (_, _) => TellCanvasWhatWidgetsCover();
        PanelDock.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
        TopBar.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
        TopBar.AddHandler(MenuBase.ClosedEvent, (_, _) => GiveTypingBackToCanvas());
        ToolbarScroller.PointerWheelChanged += OnWheelOverToolbar;
    }

    /// <summary>The panel layout's host.</summary>
    public DockControl Panels => PanelDock;

    /// <summary>Puts the menu bar in its widget, or removes the widget when there's none (macOS).</summary>
    public void SetMenuBar(Control? menuBar)
    {
        MenuBarWidget.Child = menuBar;
        MenuBarWidget.IsVisible = menuBar is not null;
    }

    /// <summary>Puts the toolbar in its widget.</summary>
    public void SetToolbar(Control toolbar) => ToolbarWidget.Child = toolbar;

    private void KeepMinimumSizeOnScreen()
    {
        var wanted = new Size(_metrics.WindowMinWidth, _metrics.WindowMinHeight);
        var minimum = wanted;

        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            var frame = FrameSize is { } outer
                ? new Size(outer.Width - ClientSize.Width, outer.Height - ClientSize.Height)
                : default;
            minimum = WindowMinimumSize.Fit(wanted, screen.WorkingArea.Size, screen.Scaling, frame);
        }

        MinWidth = minimum.Width;
        MinHeight = minimum.Height;
    }

    // What's around the empty canvas dock is what widgets cover. Floating widgets don't count: they
    // can be dragged out of the way.
    private void TellCanvasWhatWidgetsCover()
    {
        // Replacing the layout replaces the dock's control too.
        if (TopLevel.GetTopLevel(_canvasDock) is null)
        {
            _canvasDock = PanelDock.GetVisualDescendants().OfType<ToolDockControl>()
                .FirstOrDefault(dock => dock.DataContext is IDockable { Id: ShellDockIds.CanvasDock });
        }

        if (_canvasDock?.TranslatePoint(default, CanvasView) is not { } origin)
            return;

        var size = _canvasDock.Bounds.Size;
        var whole = CanvasView.Bounds.Size;
        CanvasView.SetCoveredByWidgets(new Thickness(origin.X, origin.Y,
            whole.Width - origin.X - size.Width, whole.Height - origin.Y - size.Height));
    }

    // Typing should keep going to the canvas after clicking a widget, unless the click was to type
    // into the widget itself.
    private void OnPointerReleasedOnWidget(object? sender, PointerReleasedEventArgs e) => GiveTypingBackToCanvas();

    private void GiveTypingBackToCanvas()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (FocusManager?.GetFocusedElement() is not TextBox)
                CanvasView.FocusCanvas();
        });
    }

    // A mouse's wheel only turns one way, and this row only scrolls sideways. The scroller handles a
    // sideways wheel or trackpad itself, so this only sees the rest.
    private void OnWheelOverToolbar(object? sender, PointerWheelEventArgs e)
    {
        var offset = ToolbarScroller.Offset;
        ToolbarScroller.Offset = offset.WithX(offset.X - e.Delta.Y * _metrics.ToolbarWheelStep);
        e.Handled = true;
    }
}
