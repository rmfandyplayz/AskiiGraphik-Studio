// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Clipboard;

/// <summary>
/// Where a paste lands (ADR 0067): centred on the pointer, as the owner chose; where it was copied from when the
/// pointer isn't over the canvas, or a paste there would leave nothing on it; and otherwise in the middle of the view.
/// </summary>
public static class PastePlacement
{
    /// <summary>The cell where the top-left corner of <paramref name="clip"/>'s rectangle lands.</summary>
    /// <param name="clip">What's pasted.</param>
    /// <param name="canvasSize">The size of the canvas it's pasted on.</param>
    /// <param name="pointerPosition">Where the pointer is over the canvas view, or null when it's elsewhere.</param>
    /// <param name="viewCentre">The point of the canvas in the middle of the view.</param>
    public static CellPoint TopLeft(CellClip clip, CanvasSize canvasSize, CanvasPoint? pointerPosition,
        CanvasPoint viewCentre)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (pointerPosition is { } point && LandsOnCanvas(clip, canvasSize, CentredOn(clip, point)))
            return CentredOn(clip, point);

        if (clip.Origin is { } origin && LandsOnCanvas(clip, canvasSize, origin))
            return origin;

        return CentredOn(clip, viewCentre);
    }

    // The middle of the rectangle goes as near the point as whole cells allow.
    private static CellPoint CentredOn(CellClip clip, CanvasPoint point) => new(
        (int)Math.Floor(point.X - (clip.Size.Width / 2.0) + 0.5),
        (int)Math.Floor(point.Y - (clip.Size.Height / 2.0) + 0.5));

    // True when a copied cell with something in it lands on the canvas, or the clip has none and its shape does.
    private static bool LandsOnCanvas(CellClip clip, CanvasSize canvasSize, CellPoint topLeft)
    {
        var onCanvas = new CellRect(topLeft.X, topLeft.Y, clip.Size.Width, clip.Size.Height)
            .Intersect(canvasSize.Bounds);
        if (onCanvas.IsEmpty)
            return false;

        if (!clip.HasContent)
            return !clip.Shape.MovedOnto(canvasSize, topLeft).IsEmpty;

        for (var y = onCanvas.Top; y < onCanvas.Bottom; y++)
        {
            for (var x = onCanvas.Left; x < onCanvas.Right; x++)
            {
                var point = new CellPoint(x - topLeft.X, y - topLeft.Y);
                if (clip.Shape.Contains(point) && !clip.GetCell(point).IsEmpty)
                    return true;
            }
        }

        return false;
    }
}
