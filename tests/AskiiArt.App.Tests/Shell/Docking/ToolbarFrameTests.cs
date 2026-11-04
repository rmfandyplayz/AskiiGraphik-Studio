// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.Docking;
using AskiiArt.Docking.Layouts;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Shell.Docking;

public sealed class ToolbarFrameTests : IDisposable
{
    private const double GripLength = 14;

    private readonly List<Window> _windows = [];

    public void Dispose()
    {
        foreach (var window in _windows)
            window.Close();

        Dispatcher.UIThread.RunJobs();
    }

    [Theory]
    [InlineData(0, 300, 100, 20, 0, 0.2)]
    [InlineData(100, 300, 100, 20, 0.2, 0.2)]
    [InlineData(200, 300, 100, 20, 0.2, 0)]
    [InlineData(0, 100, 100, 20, 0, 0)]
    [InlineData(0, 50, 100, 20, 0, 0)]
    [InlineData(0, 300, 30, 20, 0, 0.5)]
    [InlineData(0.2, 300, 100, 20, 0, 0.2)]
    [InlineData(199.8, 300, 100, 20, 0.2, 0)]
    [InlineData(0, 300, 0, 20, 0, 0)]
    public void AFade_ShowsOnEachSideWithMoreToScrollTo(double offset, double extent, double viewport, double fadeLength,
        double expectedStart, double expectedEnd)
    {
        var fades = ToolbarFades.For(offset, extent, viewport, fadeLength);

        Assert.Equal(expectedStart, fades.Start, precision: 6);
        Assert.Equal(expectedEnd, fades.End, precision: 6);
    }

    [Fact]
    public void NoFades_MeansNoMask()
    {
        Assert.Null(ToolbarFades.ToMask(default, isHorizontal: true));
    }

    [AvaloniaFact]
    public void AGrip_HoldsItsPlaceAtEachEnd()
    {
        var (frame, bar) = Show(new Size(400, 40));
        var grips = Grips(frame);

        Assert.Equal(2, grips.Count);
        Assert.All(grips, grip => Assert.Equal(GripLength, grip.Bounds.Width));
        Assert.Equal(0, grips[0].Bounds.X);
        Assert.Equal(frame.Bounds.Width, grips[1].Bounds.Right);
        Assert.True(bar.TranslatePoint(default, frame)!.Value.X >= GripLength);
    }

    [AvaloniaFact]
    public void AGrip_IsInvisibleUntilThePointerIsOverTheToolbar()
    {
        var (frame, _) = Show(new Size(400, 40));
        var grip = Grips(frame)[0];
        Assert.Equal(0, grip.DotOpacity);

        var window = TopLevel.GetTopLevel(frame)!;
        window.MouseMove(new Point(200, 20));
        Pump();

        Assert.True(frame.IsPointerOver);
        Assert.True(grip.DotOpacity > 0);
        Assert.True(grip.DotOpacity < 1);

        window.MouseMove(new Point(450, 200));
        Pump();

        Assert.Equal(0, grip.DotOpacity);
    }

    [AvaloniaFact]
    public void TurningAToolbar_TurnsItsButtonsAndGrips_AndScrollsTheOtherWay()
    {
        var (frame, bar) = Show(new Size(40, 400));

        frame.Orientation = ToolbarOrientation.Vertical;
        Pump();

        var scroller = frame.GetVisualDescendants().OfType<ScrollViewer>().Single();
        var grips = Grips(frame);

        Assert.Equal(Orientation.Vertical, bar.Orientation);
        Assert.Equal(ScrollBarVisibility.Hidden, scroller.VerticalScrollBarVisibility);
        Assert.Equal(ScrollBarVisibility.Disabled, scroller.HorizontalScrollBarVisibility);
        Assert.All(grips, grip => Assert.Equal(GripLength, grip.Bounds.Height));
        Assert.Equal(0, grips[0].Bounds.Y);
        Assert.Equal(frame.Bounds.Height, grips[1].Bounds.Bottom);

        frame.Orientation = ToolbarOrientation.Horizontal;
        Pump();

        Assert.Equal(Orientation.Horizontal, bar.Orientation);
        Assert.Equal(ScrollBarVisibility.Hidden, scroller.HorizontalScrollBarVisibility);
    }

    [AvaloniaFact]
    public void AToolbarThatFits_HasNoFade()
    {
        var (frame, _) = Show(new Size(400, 40));

        Assert.Null(frame.GetVisualDescendants().OfType<ScrollViewer>().Single().OpacityMask);
    }

    [AvaloniaFact]
    public void AToolbarThatDoesntFit_FadesOnTheSideWithMore_AndScrollingMovesTheFade()
    {
        var (frame, _) = Show(new Size(120, 40), buttons: 20);
        var scroller = frame.GetVisualDescendants().OfType<ScrollViewer>().Single();

        Assert.NotNull(scroller.OpacityMask);
        Assert.Equal(0, EndOpacityOf(scroller, atStart: false));
        Assert.NotEqual(0, EndOpacityOf(scroller, atStart: true));

        scroller.Offset = scroller.Offset.WithX(scroller.Extent.Width - scroller.Viewport.Width);
        Pump();

        Assert.Equal(0, EndOpacityOf(scroller, atStart: true));
        Assert.NotEqual(0, EndOpacityOf(scroller, atStart: false));
    }

    private static byte EndOpacityOf(ScrollViewer scroller, bool atStart)
    {
        var stops = ((Avalonia.Media.LinearGradientBrush)scroller.OpacityMask!).GradientStops;
        return (atStart ? stops[0] : stops[^1]).Color.A;
    }

    private static List<ToolbarGrip> Grips(ToolbarFrame frame) =>
        [.. frame.GetVisualDescendants().OfType<ToolbarGrip>()];

    private (ToolbarFrame Frame, StackPanel Bar) Show(Size size, int buttons = 3)
    {
        var bar = new StackPanel { Orientation = Orientation.Horizontal };
        for (var button = 0; button < buttons; button++)
            bar.Children.Add(new Border { Width = 30, Height = 30 });

        var frame = new ToolbarFrame
        {
            Bar = bar,
            FadeLength = 20,
            WheelStep = 50,
            Width = size.Width,
            Height = size.Height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var window = new Window { Width = 500, Height = 500, Content = frame };
        _windows.Add(window);
        window.Show();
        Pump();

        return (frame, bar);
    }

    private static void Pump()
    {
        for (var pass = 0; pass < 5; pass++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
