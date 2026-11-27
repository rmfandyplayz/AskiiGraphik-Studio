// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools.Curves;

/// <summary>How an anchor's handles are set (ADR 0083).</summary>
internal enum CurveAnchorKind
{
    /// <summary>Set from the anchors on either side, so the curve runs smoothly through it, as a click places it.</summary>
    Smooth,

    /// <summary>Set by dragging, each the other's mirror image across the anchor.</summary>
    Mirrored,

    /// <summary>Set by dragging, each on its own. Both at the anchor make a corner, as an Alt+click places it.</summary>
    Unlinked
}

/// <summary>Which of an anchor's two handles: the one shaping the curve before it, or after it.</summary>
internal enum CurveHandleSide
{
    /// <summary>The handle shaping the piece of curve that comes into the anchor.</summary>
    In,

    /// <summary>The handle shaping the piece that goes out of it.</summary>
    Out
}

/// <summary>
/// One anchor of a curve (ADR 0083): a point it passes through, in the middle of a cell, and two handles, which pull
/// the curve towards them on either side, in cells with fractions. A smooth anchor's handles are worked out from its
/// neighbours by <see cref="CurveShape.Resolve"/>, so the ones it holds count for nothing.
/// </summary>
internal readonly record struct CurveAnchor(CanvasPoint Position, CurveAnchorKind Kind, CanvasPoint In, CanvasPoint Out)
{
    /// <summary>A smooth anchor at <paramref name="position"/>.</summary>
    public static CurveAnchor Smooth(CanvasPoint position) => new(position, CurveAnchorKind.Smooth, position, position);

    /// <summary>A corner at <paramref name="position"/>: an anchor with no handles.</summary>
    public static CurveAnchor Corner(CanvasPoint position) =>
        new(position, CurveAnchorKind.Unlinked, position, position);

    /// <summary>The handle on <paramref name="side"/>.</summary>
    public CanvasPoint HandleOn(CurveHandleSide side) => side == CurveHandleSide.In ? In : Out;

    /// <summary>The anchor at <paramref name="position"/>, its handles going with it.</summary>
    public CurveAnchor MovedTo(CanvasPoint position)
    {
        var across = position.X - Position.X;
        var down = position.Y - Position.Y;
        return this with
        {
            Position = position,
            In = new CanvasPoint(In.X + across, In.Y + down),
            Out = new CanvasPoint(Out.X + across, Out.Y + down)
        };
    }

    /// <summary>
    /// The anchor with its handle on <paramref name="side"/> at <paramref name="end"/>. A mirrored anchor's other handle
    /// becomes its mirror image, unless <paramref name="unlinks"/>, which makes the two unlinked. A smooth anchor
    /// becomes mirrored first, so it should already hold its resolved handles.
    /// </summary>
    public CurveAnchor WithHandle(CurveHandleSide side, CanvasPoint end, bool unlinks)
    {
        var kind = Kind == CurveAnchorKind.Unlinked || unlinks ? CurveAnchorKind.Unlinked : CurveAnchorKind.Mirrored;
        var other = HandleOn(Opposite(side));
        if (kind == CurveAnchorKind.Mirrored)
            other = new CanvasPoint(2 * Position.X - end.X, 2 * Position.Y - end.Y);

        if (side == CurveHandleSide.In)
            return this with { Kind = kind, In = end, Out = other };

        return this with { Kind = kind, In = other, Out = end };
    }

    private static CurveHandleSide Opposite(CurveHandleSide side) =>
        side == CurveHandleSide.In ? CurveHandleSide.Out : CurveHandleSide.In;
}
