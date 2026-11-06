// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AskiiArt.App.Shell.Docking;

/// <summary>
/// A dashed rectangle whose dashes run around it like ants, for as long as it's showing. It shows where a dragged toolbar
/// or window would land. Placeholder: the owner designs the real one.
/// </summary>
internal sealed class LandingOutline : Control
{
    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<LandingOutline, IBrush?>(nameof(OutlineBrush));

    private readonly ShellLayoutMetrics _metrics;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private bool _isAttached;

    static LandingOutline() => AffectsRender<LandingOutline>(OutlineBrushProperty);

    public LandingOutline(ShellLayoutMetrics metrics)
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
