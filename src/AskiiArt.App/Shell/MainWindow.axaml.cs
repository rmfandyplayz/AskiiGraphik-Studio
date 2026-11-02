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
        PanelDock.AddHandler(PanelEvents.DoneTypingEvent, (_, _) => GiveTypingBackToCanvas());
        MenuBarStrip.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
        MenuBarStrip.AddHandler(MenuBase.ClosedEvent, (_, _) => GiveTypingBackToCanvas());
    }

    /// <summary>The panel layout's host.</summary>
    public DockControl Panels => PanelDock;

    /// <summary>Puts the menu bar in its strip, or removes the strip when there's none (macOS).</summary>
    public void SetMenuBar(Control? menuBar)
    {
        MenuBarStrip.Child = menuBar;
        MenuBarStrip.IsVisible = menuBar is not null;
    }

    internal void KeepMinimumSizeOnScreen()
    {
        // macOS can still report a closed window's position, for example when the displays change, and
        // a closed window has no screen to ask about.
        if (PlatformImpl is null)
            return;

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
}
