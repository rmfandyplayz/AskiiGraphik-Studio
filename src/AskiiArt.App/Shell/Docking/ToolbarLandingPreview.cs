// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.Docking.Solving;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A dashed outline showing where a dragged toolbar would go. Over a drop zone that's all it is, around the zone. For a
/// toolbar pushed back inside the canvas region, a real copy of the toolbar shows in it, a little see-through. It never
/// catches the pointer. Placeholder: the owner designs the real one.
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
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = metrics.MotionDuration }];

        toolbar.Bind(OpacityProperty, toolbar.GetResourceObservable("AppLandingToolbarOpacity"));
        Children.Add(toolbar);
        Children.Add(Outline);
    }

    /// <summary>The copy of the toolbar.</summary>
    public ToolbarFrame Toolbar { get; }

    /// <summary>The outline around it.</summary>
    public ToolbarLandingOutline Outline { get; }

    /// <summary>Whether the copy shows in the outline, or the outline is all there is.</summary>
    public bool ShowsCopy
    {
        get => Toolbar.IsVisible;
        set => Toolbar.IsVisible = value;
    }

    /// <summary>The size the toolbar wants each way round.</summary>
    public ToolbarShapes MeasureBothWays() => Toolbar.MeasureBothWays();
}

/// <summary>A dashed rectangle whose dashes run around it like ants, for as long as it's showing.</summary>
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

    /// <summary>How far the dashes have run so far, in logical pixels.</summary>
    public double DistanceRun { get; private set; }

    public override void Render(DrawingContext context)
    {
        // Just inside its bounds, because a pen draws half its thickness on each side of the line.
        var thickness = _metrics.LandingOutlineThickness;
        var line = new Rect(Bounds.Size).Deflate(thickness / 2);

        var ants = MarchingAnts.Around(2 * (line.Width + line.Height),
            _metrics.LandingOutlineDash * thickness, _metrics.LandingOutlineDashGap * thickness);

        // A pen counts its dashes in multiples of its thickness.
        var dashes = new DashStyle([ants.Dash / thickness, ants.Gap / thickness], ants.OffsetAfter(DistanceRun) / thickness);
        context.DrawRectangle(null, new Pen(OutlineBrush, thickness, dashes), line);
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
            DistanceRun = _clock.Elapsed.TotalSeconds * _metrics.LandingAntsSpeed;
            InvalidateVisual();
        }

        RequestNextFrame();
    }
}
