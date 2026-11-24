// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// How the drawing of the canvas is scaled and shifted on screen while the view glides to a new zoom (ADR 0076): a point
/// at (x, y) in the drawing shows at (x × <see cref="ScaleX"/> + <see cref="OffsetX"/>, y × <see cref="ScaleY"/> +
/// <see cref="OffsetY"/>), all in device pixels.
/// </summary>
internal readonly record struct ViewTransform(double ScaleX, double ScaleY, double OffsetX, double OffsetY)
{
    /// <summary>The drawing as it is, unscaled and unshifted.</summary>
    public static ViewTransform Identity { get; } = new(1, 1, 0, 0);

    /// <summary>True when the drawing shows bigger or smaller than it's drawn.</summary>
    /// <remarks>A glide that keeps the cell size uses a scale of exactly 1, so comparing exactly is safe.</remarks>
    public bool IsScaled => ScaleX != 1 || ScaleY != 1;

    /// <summary>Where <paramref name="point"/> in the drawing shows on screen.</summary>
    public Point Apply(Point point) => new(point.X * ScaleX + OffsetX, point.Y * ScaleY + OffsetY);
}
