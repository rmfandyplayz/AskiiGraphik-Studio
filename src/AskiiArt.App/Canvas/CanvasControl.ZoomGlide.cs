// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;
using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Bringing cells into view after a paste or a run of arrow presses (ADRs 0076, 0077). Cells that aren't fully in view
/// bring the view to their centre, at the current zoom if they fit there with room round them, else zoomed out to the
/// largest usual step at which they do, or to 25%. The view glides there, zoom and all, and nothing else can change the
/// zoom until it's done.
/// </summary>
internal sealed partial class CanvasControl
{
    public static readonly StyledProperty<double> ZoomGlideMillisecondsProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(ZoomGlideMilliseconds));

    public static readonly DirectProperty<CanvasControl, bool> IsZoomLockedProperty =
        AvaloniaProperty.RegisterDirect<CanvasControl, bool>(nameof(IsZoomLocked), control => control.IsZoomLocked);

    private ZoomGlide? _zoomGlide;
    private bool _isZoomGlideFrameRequested;

    // True while a glide sets the zoom factor itself, having already worked out the view at that zoom.
    private bool _isGlideSettingZoom;

    /// <summary>How long the view takes to glide to a paste, zoom and all, in milliseconds; zero makes it jump.</summary>
    public double ZoomGlideMilliseconds
    {
        get => GetValue(ZoomGlideMillisecondsProperty);
        set => SetValue(ZoomGlideMillisecondsProperty, value);
    }

    /// <summary>True while the view glides to a paste, when nothing else may change the zoom.</summary>
    public bool IsZoomLocked
    {
        get;
        private set => SetAndRaise(IsZoomLockedProperty, ref field, value);
    }

    /// <summary>
    /// Brings <paramref name="cells"/> into view when any of them is out of it, as after a paste: the view glides to
    /// their centre, zoomed out if need be to fit them with room round them (ADRs 0076, 0077). Cells fully in view leave
    /// the view as it is.
    /// </summary>
    public void BringIntoView(CellRect cells)
    {
        if (_viewport is null || cells.IsEmpty || Font is null)
            return;

        if (_viewport.UsableOnCanvas.Contains(PixelsOf(cells, _viewport.CellSize)))
            return;

        // A settling glide under way stops where it is, still held there, so the new glide starts from what's on screen.
        // Releasing it first would jump to where it was going (4.11's testing).
        _glide = null;
        EndZoomGlide();
        var from = _viewport;
        var factor = ViewFit.ZoomFor(cells, ZoomFactor, from.UsableSize, from.Margin, CellSizeAt);
        var cellSize = CellSizeAt(factor);
        var zoomed = cellSize == from.CellSize ? from : from.Zoomed(cellSize, from.UsableCentre);
        var centre = PixelsOf(cells, cellSize).Center;
        var middle = zoomed.UsableCentre;
        var origin = new PixelPoint((int)Math.Round(middle.X) - centre.X, (int)Math.Round(middle.Y) - centre.Y);
        GlideZoomTo(factor, zoomed.HeldAt(origin).Released());
    }

    private static PixelRect PixelsOf(CellRect cells, PixelSize cellSize) =>
        new(cells.Left * cellSize.Width, cells.Top * cellSize.Height, cells.Width * cellSize.Width,
            cells.Height * cellSize.Height);

    // The cell size at a zoom factor, as the atlas for it would measure it.
    private PixelSize CellSizeAt(double zoomFactor)
    {
        var metrics = CellMetrics.Measure(Font!, PixelSizeAt(zoomFactor));
        return new PixelSize(metrics.Width, metrics.Height);
    }

    private float PixelSizeAt(double zoomFactor) => Math.Clamp((float)(BaseFontSize * zoomFactor * _renderScaling),
        CellMetrics.MinimumPixelSize, CellMetrics.MaximumPixelSize);

    // The view goes straight to where it ends, so the pointer and the scroll bars work as they will there, and only the
    // drawing glides.
    private void GlideZoomTo(double zoomFactor, CanvasViewport target)
    {
        if (_viewport is not { } from)
            return;

        _isGlideSettingZoom = true;
        try
        {
            SetCurrentValue(ZoomFactorProperty, zoomFactor);
        }
        finally
        {
            _isGlideSettingZoom = false;
        }

        SetViewport(target);
        var duration = TimeSpan.FromMilliseconds(Math.Max(0, ZoomGlideMilliseconds));
        var isStill = from.CellSize == target.CellSize && from.CanvasOrigin == target.CanvasOrigin;
        if (isStill || duration == TimeSpan.Zero || _topLevel is null)
            return;

        _zoomGlide = new ZoomGlide(from, _antsClock.Elapsed, duration);
        IsZoomLocked = true;
        RequestZoomGlideFrame();
    }

    private void RequestZoomGlideFrame()
    {
        if (_isZoomGlideFrameRequested || _topLevel is null)
            return;

        _isZoomGlideFrameRequested = true;
        _topLevel.RequestAnimationFrame(OnZoomGlideFrame);
    }

    // Each frame draws where the glide has got to. Something starting to move ends it at once, at its end.
    private void OnZoomGlideFrame(TimeSpan frameTime)
    {
        _isZoomGlideFrameRequested = false;
        if (_zoomGlide is not { } glide)
            return;

        if (IsHoldingView || glide.IsOverAt(_antsClock.Elapsed))
        {
            EndZoomGlide();
            SettleView();
            return;
        }

        InvalidateVisual();
        RequestZoomGlideFrame();
    }

    // The view is already where the glide ends, so ending it only stops drawing the way there.
    private void EndZoomGlide()
    {
        if (_zoomGlide is null)
            return;

        _zoomGlide = null;
        IsZoomLocked = false;
        InvalidateVisual();
    }

    /// <summary>Where the canvas's top-left corner shows on screen now, in device pixels, gliding or not.</summary>
    internal Point CanvasOriginOnScreen()
    {
        if (_viewport is null)
            return default;

        var (viewport, transform) = DrawnView(_viewport);
        return transform.Apply(new Point(viewport.CanvasOrigin.X, viewport.CanvasOrigin.Y));
    }

    // What to draw now: the view, or while a zoom glides, a view covering what the screen shows of the glide and how
    // it's scaled and shifted there.
    private (CanvasViewport Viewport, ViewTransform Transform) DrawnView(CanvasViewport viewport) =>
        _zoomGlide is { } glide
            ? glide.FrameAt(_antsClock.Elapsed, viewport)
            : (viewport, ViewTransform.Identity);
}
