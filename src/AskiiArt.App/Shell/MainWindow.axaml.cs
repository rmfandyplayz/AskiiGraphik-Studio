// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Shell;

internal sealed partial class MainWindow : Window
{
    private readonly ShellLayoutMetrics _metrics;
    private bool _mayBeAnySize;

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

        DockingLayer.LayoutUpdated += (_, _) => TellCanvasWhatWidgetsCover();
        DockingLayer.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
        DockingLayer.AddHandler(PanelEvents.DoneTypingEvent, (_, _) => GiveTypingBackToCanvas());
        MenuBarStrip.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
        MenuBarStrip.AddHandler(MenuBase.ClosedEvent, (_, _) => GiveTypingBackToCanvas());
    }

    /// <summary>The host of the windows and toolbars.</summary>
    public DockingHost Panels => DockingLayer;

    /// <summary>Puts the menu bar in its strip, or removes the strip when there's none (macOS).</summary>
    public void SetMenuBar(Control? menuBar)
    {
        MenuBarStrip.Child = menuBar;
        MenuBarStrip.IsVisible = menuBar is not null;
    }

    /// <summary>Lifts the minimum size until the window closes, to test how the layout copes with little room.</summary>
    public void AllowAnySize()
    {
        _mayBeAnySize = true;
        KeepMinimumSizeOnScreen();
    }

    internal void KeepMinimumSizeOnScreen()
    {
        // macOS can still report a closed window's position, for example when the displays change, and
        // a closed window has no screen to ask about.
        if (PlatformImpl is null)
            return;

        if (_mayBeAnySize)
        {
            MinWidth = 0;
            MinHeight = 0;
            return;
        }

        var wanted = new Size(_metrics.MainWindowMinWidth, _metrics.MainWindowMinHeight);
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

    private void TellCanvasWhatWidgetsCover()
    {
        var covered = DockingLayer.SafeArea;
        CanvasView.SetCoveredByWidgets(new Thickness(covered.Left, covered.Top, covered.Right, covered.Bottom));
    }

    // Typing should keep going to the canvas after clicking a widget, unless the click was to type
    // into the widget itself. A right-click doesn't move the keyboard, and it may have opened a menu that's
    // about to use it. The menu hands typing back when it closes.
    private void OnPointerReleasedOnWidget(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Right)
            GiveTypingBackToCanvas();
    }

    // Not while a menu has the keyboard, such as one a button just opened, or Esc and the arrow keys stop working in it.
    private void GiveTypingBackToCanvas()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var focused = FocusManager?.GetFocusedElement();
            if (focused is TextBox || IsInAMenu(focused))
                return;

            CanvasView.FocusCanvas();
        });
    }

    private static bool IsInAMenu(IInputElement? element) =>
        element is Visual visual && visual.FindAncestorOfType<MenuBase>(includeSelf: true) is { IsOpen: true };
}
