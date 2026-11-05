// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A hatched rectangle with a "+", showing where a dragged window would land, as in the owner's mockup. It never catches
/// the pointer. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class WindowPlaceholderView : Control
{
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<WindowPlaceholderView, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> LineProperty =
        AvaloniaProperty.Register<WindowPlaceholderView, IBrush?>(nameof(Line));

    private readonly ShellLayoutMetrics _metrics;

    static WindowPlaceholderView() => AffectsRender<WindowPlaceholderView>(FillProperty, LineProperty);

    /// <param name="metrics">The theme's sizes.</param>
    public WindowPlaceholderView(ShellLayoutMetrics metrics)
    {
        _metrics = metrics;
        IsHitTestVisible = false;
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = metrics.MotionDuration }];
        Bind(FillProperty, this.GetResourceObservable("AppWindowPlaceholderBrush"));
        Bind(LineProperty, this.GetResourceObservable("AppWindowPlaceholderLineBrush"));
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Line
    {
        get => GetValue(LineProperty);
        set => SetValue(LineProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var pen = new Pen(Line, _metrics.WindowPlaceholderLineThickness);

        context.DrawRectangle(Fill, pen, bounds);

        using (context.PushClip(bounds))
        {
            var gap = Math.Max(1, _metrics.WindowPlaceholderHatchGap);
            for (var offset = gap; offset < bounds.Width + bounds.Height; offset += gap)
                context.DrawLine(pen, new Point(offset, 0), new Point(0, offset));
        }

        var arm = _metrics.WindowPlaceholderPlusSize / 2;
        var center = bounds.Center;
        context.DrawLine(pen, center + new Point(-arm, 0), center + new Point(arm, 0));
        context.DrawLine(pen, center + new Point(0, -arm), center + new Point(0, arm));
    }
}
