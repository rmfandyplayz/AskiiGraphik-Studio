// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.Docking.Dragging;
using AskiiArt.Docking.Layouts;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// What a dragged toolbar would look like where it would land: a real copy of it, a little see-through, inside a
/// dashed outline. It never catches the pointer. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class ToolbarLandingPreview : Panel
{
    /// <param name="toolbar">The copy of the toolbar, which this owns.</param>
    /// <param name="metrics">The theme's sizes.</param>
    public ToolbarLandingPreview(ToolbarFrame toolbar, ShellLayoutMetrics metrics)
    {
        Toolbar = toolbar;
        Outline = new ToolbarLandingOutline(metrics);
        IsHitTestVisible = false;

        toolbar.Bind(OpacityProperty, toolbar.GetResourceObservable("AppLandingToolbarOpacity"));
        Children.Add(toolbar);
        Children.Add(Outline);
    }

    /// <summary>The copy of the toolbar.</summary>
    public ToolbarFrame Toolbar { get; }

    /// <summary>The outline around it.</summary>
    public ToolbarLandingOutline Outline { get; }

    /// <summary>
    /// The size the toolbar wants each way round. The real toolbar could be asked instead, but turning it would lose
    /// its scroll position.
    /// </summary>
    public ToolbarShapes MeasureBothWays() =>
        new(WantedSize(ToolbarOrientation.Horizontal), WantedSize(ToolbarOrientation.Vertical));

    private LayoutSize WantedSize(ToolbarOrientation orientation)
    {
        Toolbar.Orientation = orientation;
        Toolbar.Measure(Size.Infinity);
        return new LayoutSize(Toolbar.DesiredSize.Width, Toolbar.DesiredSize.Height);
    }
}

/// <summary>A dashed rectangle that slowly moves out from its bounds and back, for as long as it's showing.</summary>
internal sealed class ToolbarLandingOutline : Control
{
    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<ToolbarLandingOutline, IBrush?>(nameof(OutlineBrush));

    private readonly ShellLayoutMetrics _metrics;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _isAttached;

    static ToolbarLandingOutline() => AffectsRender<ToolbarLandingOutline>(OutlineBrushProperty);

    public ToolbarLandingOutline(ShellLayoutMetrics metrics)
    {
        _metrics = metrics;
        Bind(OutlineBrushProperty, this.GetResourceObservable("AppLandingOutlineBrush"));
    }

    public IBrush? OutlineBrush
    {
        get => GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    /// <summary>How far out from its bounds it is right now.</summary>
    public double Pulse { get; private set; }

    /// <summary>How far out it is at a moment: at its bounds to start with, furthest out halfway through a period.</summary>
    public static double PulseAt(TimeSpan elapsed, TimeSpan period, double length) =>
        period > TimeSpan.Zero ? length * (1 - Math.Cos(2 * Math.PI * (elapsed / period))) / 2 : 0;

    public override void Render(DrawingContext context)
    {
        var dashes = new DashStyle([_metrics.LandingOutlineDash, _metrics.LandingOutlineDashGap], 0);
        var pen = new Pen(OutlineBrush, _metrics.LandingOutlineThickness, dashes);
        context.DrawRectangle(null, pen, new Rect(Bounds.Size).Inflate(Pulse));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttached = true;
        RequestNextFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _isAttached = false;
    }

    private void RequestNextFrame() => TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);

    private void OnFrame(TimeSpan frameTime)
    {
        if (!_isAttached)
            return;

        if (IsEffectivelyVisible)
        {
            Pulse = PulseAt(_clock.Elapsed, _metrics.LandingPulsePeriod, _metrics.LandingPulseLength);
            InvalidateVisual();
        }

        RequestNextFrame();
    }
}
