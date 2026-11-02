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
    private ToolDockControl? _canvasDock;

    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => CanvasView.FocusCanvas();
        PanelDock.LayoutUpdated += (_, _) => TellCanvasWhatWidgetsCover();
        PanelDock.AddHandler(PointerReleasedEvent, OnPointerReleasedOnWidget, RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    /// <summary>The panel layout's host.</summary>
    public DockControl Panels => PanelDock;

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

        if (_canvasDock?.TranslatePoint(default, PanelDock) is not { } origin)
            return;

        var size = _canvasDock.Bounds.Size;
        var host = PanelDock.Bounds.Size;
        CanvasView.SetCoveredByWidgets(new Thickness(origin.X, origin.Y,
            host.Width - origin.X - size.Width, host.Height - origin.Y - size.Height));
    }

    // Typing should keep going to the canvas after clicking a widget, unless the click was to type
    // into the widget itself.
    private void OnPointerReleasedOnWidget(object? sender, PointerReleasedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (FocusManager?.GetFocusedElement() is not TextBox)
                CanvasView.FocusCanvas();
        });
    }
}
