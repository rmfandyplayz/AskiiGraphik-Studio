// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas view gliding to a new zoom and place, as after a paste (ADR 0076). The view itself is already where the
/// glide ends, so the pointer and the scroll bars work as they will there; only the drawing glides, from how the view
/// looked at <see cref="From"/> to how it looks at the end, over <see cref="Duration"/>, quickly at first and slowing to a
/// stop. The cell size changes by the same ratio every moment, so each zoom step takes as long as the next, while the spot
/// of the canvas in the middle of the view moves steadily from where it was to where it ends. Pure maths, so it's tested
/// without a window.
/// </summary>
internal sealed record ZoomGlide(CanvasViewport From, TimeSpan Start, TimeSpan Duration)
{
    /// <summary>True once the glide has reached its end at <paramref name="now"/>.</summary>
    public bool IsOverAt(TimeSpan now) => now - Start >= Duration;

    /// <summary>
    /// How the view <paramref name="to"/>, where the glide ends, is scaled and shifted on screen at <paramref name="now"/>
    /// to show how far the glide has got.
    /// </summary>
    public ViewTransform TransformAt(TimeSpan now, CanvasViewport to)
    {
        ArgumentNullException.ThrowIfNull(to);
        if (IsOverAt(now))
            return ViewTransform.Identity;

        var progress = Math.Clamp((now - Start) / Duration, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        var fromMiddle = From.UsableCentre;
        var toMiddle = to.UsableCentre;
        var fromCentre = From.CanvasPointAt(fromMiddle);
        var toCentre = to.CanvasPointAt(toMiddle);
        var toOrigin = to.CanvasOrigin;

        var (scaleX, offsetX) = Along(From.CellSize.Width, to.CellSize.Width, fromMiddle.X, toMiddle.X, fromCentre.X,
            toCentre.X, toOrigin.X, eased);
        var (scaleY, offsetY) = Along(From.CellSize.Height, to.CellSize.Height, fromMiddle.Y, toMiddle.Y, fromCentre.Y,
            toCentre.Y, toOrigin.Y, eased);
        return new ViewTransform(scaleX, scaleY, offsetX, offsetY);
    }

    /// <summary>
    /// What to draw at <paramref name="now"/>: a view at the zoom of <paramref name="to"/>, where the glide ends, covering
    /// everything the screen shows of the glide then, and how that view is scaled and shifted onto the screen.
    /// </summary>
    public (CanvasViewport Viewport, ViewTransform Transform) FrameAt(TimeSpan now, CanvasViewport to)
    {
        var transform = TransformAt(now, to);
        if (transform == ViewTransform.Identity)
            return (to, transform);

        // The part of the end's view that shows on screen, rounded out to whole pixels.
        var size = to.ViewportSize;
        var left = (int)Math.Floor(-transform.OffsetX / transform.ScaleX);
        var top = (int)Math.Floor(-transform.OffsetY / transform.ScaleY);
        var right = (int)Math.Ceiling((size.Width - transform.OffsetX) / transform.ScaleX);
        var bottom = (int)Math.Ceiling((size.Height - transform.OffsetY) / transform.ScaleY);

        var origin = to.CanvasOrigin;
        var drawn = new CanvasViewport(to.CanvasSize, to.CellSize, new PixelSize(right - left, bottom - top), to.Margin,
                default, reach: to.Reach)
            .HeldAt(new PixelPoint(origin.X - left, origin.Y - top));
        var shifted = transform with
        {
            OffsetX = transform.OffsetX + left * transform.ScaleX,
            OffsetY = transform.OffsetY + top * transform.ScaleY
        };
        return (drawn, shifted);
    }

    // Along one axis: the cell size, the canvas spot in the middle of the view and that middle's place on screen all move
    // from the start's to the end's, and the end's view is scaled and shifted to match.
    private static (double Scale, double Offset) Along(int fromCell, int toCell, double fromMiddle, double toMiddle,
        double fromCentre, double toCentre, int toOrigin, double eased)
    {
        var middle = fromMiddle + (toMiddle - fromMiddle) * eased;
        var centre = fromCentre + (toCentre - fromCentre) * eased;

        // Unscaled, a shift by whole pixels keeps every cell's edges on the device's pixel grid.
        if (fromCell == toCell)
            return (1, Math.Round(middle - centre * toCell - toOrigin));

        var cell = fromCell * Math.Pow((double)toCell / fromCell, eased);
        var scale = cell / toCell;
        var origin = middle - centre * cell;
        return (scale, origin - toOrigin * scale);
    }
}
