// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics;
using AskiiArt.App.Canvas.FrameRate;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// Hands one frame's <see cref="CanvasScene"/> to Avalonia's render thread and paints it straight
/// onto Skia through a lease, in one batched glyph draw per atlas page (ADR 0015). It owns the
/// scene and disposes it when Avalonia is done with the frame. Each paint is recorded in
/// <paramref name="timings"/> for the Debug frame-rate readout, with the <paramref name="buildTicks"/>
/// the scene took to build.
/// </summary>
internal sealed class CanvasDrawOperation(Rect bounds, CanvasScene scene, FrameTimings? timings, long buildTicks)
    : ICustomDrawOperation
{
    // Avalonia normally paints and disposes on its render thread, but nothing promises that, so
    // painting and disposing never overlap.
    private readonly Lock _gate = new();
    private bool _isDisposed;
    private long _buildTicks = buildTicks;

    public Rect Bounds => bounds;

    public bool HitTest(Point point) => bounds.Contains(point);

    // Every frame's scene is different, so a frame is never skipped as "unchanged".
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Render(ImmediateDrawingContext context)
    {
        // Missing only on a renderer without Skia (the headless test platform): nothing to paint.
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } leaseFeature)
            return;

        lock (_gate)
        {
            if (_isDisposed)
                return;

            var paintStarted = Stopwatch.GetTimestamp();
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();

            // Avalonia's transform maps logical pixels to device pixels. The scene is already in
            // device pixels, so keep only the offset, snapped to a whole pixel: every cell edge
            // then lands exactly on the device's pixel grid, at any display scaling.
            var matrix = canvas.TotalMatrix;
            canvas.SetMatrix(SKMatrix.CreateTranslation(MathF.Round(matrix.TransX), MathF.Round(matrix.TransY)));
            CanvasScenePainter.Paint(canvas, scene);
            canvas.Restore();
            RecordFrame(paintStarted);
        }
    }

    // Avalonia paints a scene again when something drawn over the canvas changes, but its build counts only once.
    private void RecordFrame(long paintStarted)
    {
        if (timings is not { IsRecording: true })
            return;

        var paintedAt = Stopwatch.GetTimestamp();
        timings.Record(paintedAt, _buildTicks + paintedAt - paintStarted);
        _buildTicks = 0;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            scene.Dispose();
        }
    }
}
