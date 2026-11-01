// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// The canvas control together with the scrollbars bound to it, on Avalonia's headless platform.
/// </summary>
public sealed class CanvasViewTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(200, 100));
    private readonly Window _window;
    private readonly CanvasView _view;

    public CanvasViewTests()
    {
        _view = new CanvasView { DataContext = _canvas.ViewModel };
        _window = new Window { Width = 400, Height = 300, Content = _view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private CanvasControl Control => _view.GetVisualDescendants().OfType<CanvasControl>().Single();

    // From 1.5 testing: zooming out while scrolled far right or down drifted to the bottom right,
    // because the scrollbars clamped their old values to the smaller range and wrote them back.
    [AvaloniaFact]
    public void ZoomingOutWhileScrolledFar_KeepsTheCentreSpot_AndTheScrollbarsAgree()
    {
        Control.ZoomFactor = 4;
        Control.HorizontalOffset = Control.HorizontalMaximum / 2;
        Control.VerticalOffset = Control.VerticalMaximum;
        var before = Control.Viewport!;

        Control.ZoomFactor = 1;

        var after = Control.Viewport!;
        var centre = new Point(before.ViewportSize.Width / 2.0, before.ViewportSize.Height / 2.0);
        Assert.Equal(before.Zoomed(after.CellSize, centre).ScrollOffset, after.ScrollOffset);
        Assert.Equal(after.ScrollOffset.X, Control.HorizontalOffset);
        Assert.Equal(after.ScrollOffset.Y, Control.VerticalOffset);
        Assert.Equal(after.ScrollOffset.X, ScrollBar(Avalonia.Layout.Orientation.Horizontal).Value);
        Assert.Equal(after.ScrollOffset.Y, ScrollBar(Avalonia.Layout.Orientation.Vertical).Value);
    }

    [AvaloniaFact]
    public void ZoomingInFromTheTopLeft_ScrollsAsFarAsTheAnchorNeeds()
    {
        Control.ZoomFactor = 1;
        Control.HorizontalOffset = 0;
        Control.VerticalOffset = 0;
        var before = Control.Viewport!;

        Control.ZoomFactor = 4;

        var after = Control.Viewport!;
        var centre = new Point(before.ViewportSize.Width / 2.0, before.ViewportSize.Height / 2.0);
        Assert.Equal(before.Zoomed(after.CellSize, centre).ScrollOffset, after.ScrollOffset);
        Assert.Equal(after.ScrollOffset.X, ScrollBar(Avalonia.Layout.Orientation.Horizontal).Value);
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    private ScrollBar ScrollBar(Avalonia.Layout.Orientation orientation) =>
        _view.GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Orientation == orientation);
}
