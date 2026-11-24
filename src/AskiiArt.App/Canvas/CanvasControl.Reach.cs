// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Moving;
using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// How far the canvas view can scroll, and how it settles (ADR 0072). The view reaches the canvas, the zone around it,
/// and a selection past that zone with room around it. It never moves while something may be moving: during a gesture,
/// while an arrow is held, and for the pause after one that can carry a run of presses on. Then it glides back out of
/// space nothing needs any more. After a run of arrow presses that moved the selection, it brings the selection into
/// view, as a paste does (ADR 0077).
/// </summary>
internal sealed partial class CanvasControl
{
    public static readonly StyledProperty<double> CellWidthToHeightProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(CellWidthToHeight), 0.5);

    public static readonly StyledProperty<double> ViewGlideMillisecondsProperty =
        AvaloniaProperty.Register<CanvasControl, double>(nameof(ViewGlideMilliseconds));

    private readonly HashSet<ToolKey> _arrowsDown = [];

    // When the last arrow held was let go, until the pause after it is over.
    private TimeSpan? _arrowsLetGoAt;
    private bool _isArrowPauseFrameRequested;

    // Whether the selection changed while arrows were held or in the pause after them, so it's brought into view then.
    private bool _hasArrowRunChangedSelection;
    private ViewGlide? _glide;
    private bool _isGlideFrameRequested;

    /// <summary>How wide the document's cells are for their height, which sets how many rows the zone has.</summary>
    public double CellWidthToHeight
    {
        get => GetValue(CellWidthToHeightProperty);
        set => SetValue(CellWidthToHeightProperty, value);
    }

    /// <summary>How long the view takes to glide to where it settles, in milliseconds; zero makes it jump.</summary>
    public double ViewGlideMilliseconds
    {
        get => GetValue(ViewGlideMillisecondsProperty);
        set => SetValue(ViewGlideMillisecondsProperty, value);
    }

    // While something may be moving, the view stays where it is.
    private bool IsHoldingView => _isToolGestureActive || IsArrowRunUnderWay;

    private bool IsArrowRunUnderWay => _arrowsDown.Count > 0 || _arrowsLetGoAt is not null;

    private CellRect CurrentReach()
    {
        if (Composite is null)
            return CellRect.Empty;

        var selection = Selection is { IsEmpty: false } selected ? selected.Mask : null;
        return CanvasReach.Of(Composite.Size, selection, CellWidthToHeight);
    }

    // The reach follows the selection. The canvas stays where it is, then settles once nothing is moving.
    private void UpdateReach()
    {
        if (_viewport is not { } viewport || Composite is null)
            return;

        var reach = CurrentReach();
        if (reach != viewport.Reach)
            SetViewport(viewport.WithReach(reach).HeldAt(viewport.CanvasOrigin));

        SettleView();
    }

    // Glides out of space nothing needs any more, unless something may still be moving or the view is gliding.
    private void SettleView()
    {
        if (_viewport is not { } viewport || IsHoldingView || _glide is not null || _zoomGlide is not null)
            return;

        GlideTo(viewport.Released().CanvasOrigin);
    }

    private void GlideTo(PixelPoint target)
    {
        if (_viewport is not { } viewport)
            return;

        var from = viewport.CanvasOrigin;
        var duration = TimeSpan.FromMilliseconds(Math.Max(0, ViewGlideMilliseconds));
        if (from == target || duration == TimeSpan.Zero || _topLevel is null)
        {
            _glide = null;
            SetViewport(viewport.HeldAt(target).Released());
            return;
        }

        _glide = new ViewGlide(from, target, _antsClock.Elapsed, duration);
        RequestGlideFrame();
    }

    private void RequestGlideFrame()
    {
        if (_isGlideFrameRequested || _topLevel is null)
            return;

        _isGlideFrameRequested = true;
        _topLevel.RequestAnimationFrame(OnGlideFrame);
    }

    // Each frame holds the view where the glide has got to. Something starting to move stops it there.
    private void OnGlideFrame(TimeSpan frameTime)
    {
        _isGlideFrameRequested = false;
        if (_glide is not { } glide || _viewport is not { } viewport)
            return;

        if (IsHoldingView)
        {
            _glide = null;
            return;
        }

        var now = _antsClock.Elapsed;
        if (glide.IsOverAt(now))
        {
            _glide = null;
            SetViewport(viewport.HeldAt(glide.To).Released());
            return;
        }

        SetViewport(viewport.HeldAt(glide.OriginAt(now)));
        RequestGlideFrame();
    }

    // Scrolling or zooming takes over from a glide under way, from where it is. A zoom glide's view is already where
    // it ends, so it ends there.
    private void StopGlide()
    {
        EndZoomGlide();
        if (_glide is null)
            return;

        _glide = null;
        if (_viewport is { } viewport)
            SetViewport(viewport.Released());
    }

    private void HoldForArrow(ToolKey arrow)
    {
        _arrowsDown.Add(arrow);
        _arrowsLetGoAt = null;
    }

    // The view settles once no arrow has been held for the pause that ends a run of presses. Frames watch the time,
    // as the glide's do.
    private void LetGoOfArrow(ToolKey arrow)
    {
        if (!_arrowsDown.Remove(arrow) || _arrowsDown.Count > 0)
            return;

        _arrowsLetGoAt = _antsClock.Elapsed;
        RequestArrowPauseFrame();
    }

    private void RequestArrowPauseFrame()
    {
        if (_isArrowPauseFrameRequested)
            return;

        if (_topLevel is null)
        {
            _arrowsLetGoAt = null;
            SettleAfterArrows();
            return;
        }

        _isArrowPauseFrameRequested = true;
        _topLevel.RequestAnimationFrame(OnArrowPauseFrame);
    }

    private void OnArrowPauseFrame(TimeSpan frameTime)
    {
        _isArrowPauseFrameRequested = false;
        if (_arrowsLetGoAt is not { } letGoAt)
            return;

        if (_antsClock.Elapsed - letGoAt < MoveTool.ArrowRunPause)
        {
            RequestArrowPauseFrame();
            return;
        }

        _arrowsLetGoAt = null;
        SettleAfterArrows();
    }

    // A run of arrow presses that moved the selection brings it into view, as a paste does; otherwise the view settles.
    private void SettleAfterArrows()
    {
        var hasChangedSelection = _hasArrowRunChangedSelection;
        _hasArrowRunChangedSelection = false;
        if (hasChangedSelection && !IsHoldingView && Selection is { IsEmpty: false } selection)
            BringIntoView(selection.Mask.Bounds);

        SettleView();
    }

    private void LetGoOfAllArrows()
    {
        foreach (var arrow in _arrowsDown.ToArray())
            LetGoOfArrow(arrow);
    }
}
