// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Lines;

namespace AskiiArt.Core.Tools.Curves;

/// <summary>
/// The shape a curve's anchors make (ADR 0083): from each anchor to the next, a cubic Bézier piece from the anchor's out
/// handle to the next one's in handle, and from the last back to the first when it's closed.
/// </summary>
internal static class CurveShape
{
    // Each piece is drawn as straight pieces this many to a cell along its handles, so each is short next to the parts
    // a cell is measured in, as an ellipse's are.
    private const int StraightPiecesPerCell = 4;

    // A smooth anchor's handles reach this part of the way from the anchor before it to the one after, as a
    // Catmull-Rom spline's do.
    private const double SmoothHandleReach = 1.0 / 6;

    /// <summary>
    /// <paramref name="anchors"/> with each smooth anchor's handles set from its neighbours, along the line from the one
    /// before it to the one after. An open curve's ends count themselves as their missing neighbour.
    /// </summary>
    public static CurveAnchor[] Resolve(IReadOnlyList<CurveAnchor> anchors, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(anchors);
        var resolved = new CurveAnchor[anchors.Count];
        for (var index = 0; index < anchors.Count; index++)
        {
            var anchor = anchors[index];
            if (anchor.Kind != CurveAnchorKind.Smooth)
            {
                resolved[index] = anchor;
                continue;
            }

            var before = NeighbourPosition(anchors, index - 1, isClosed, anchor.Position);
            var after = NeighbourPosition(anchors, index + 1, isClosed, anchor.Position);
            var across = (after.X - before.X) * SmoothHandleReach;
            var down = (after.Y - before.Y) * SmoothHandleReach;
            var position = anchor.Position;
            resolved[index] = anchor with
            {
                In = new CanvasPoint(position.X - across, position.Y - down),
                Out = new CanvasPoint(position.X + across, position.Y + down)
            };
        }

        return resolved;
    }

    /// <summary>
    /// The path the curve through <paramref name="resolved"/> anchors follows, as <see cref="Resolve"/> makes them, in
    /// short straight pieces.
    /// </summary>
    /// <exception cref="ArgumentException">There are no anchors.</exception>
    public static LinePath PathOf(IReadOnlyList<CurveAnchor> resolved, bool isClosed)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        if (resolved.Count == 0)
            throw new ArgumentException("A curve needs at least one anchor.", nameof(resolved));

        var points = new List<CanvasPoint>();
        var pieceCount = isClosed ? resolved.Count : resolved.Count - 1;
        for (var index = 0; index < pieceCount; index++)
            AddPiece(resolved[index], resolved[(index + 1) % resolved.Count], points);

        // A closed path joins back to its first point by itself.
        if (!isClosed)
            points.Add(resolved[^1].Position);

        return LinePath.Through(points, isClosed && resolved.Count > 1);
    }

    // The piece's points from its start, leaving out its end, where the next piece starts.
    private static void AddPiece(CurveAnchor from, CurveAnchor to, List<CanvasPoint> points)
    {
        var start = from.Position;
        var startHandle = from.Out;
        var endHandle = to.In;
        var end = to.Position;
        var handlesLength = Distance(start, startHandle) + Distance(startHandle, endHandle) + Distance(endHandle, end);
        var steps = Math.Max(1, (int)Math.Ceiling(handlesLength * StraightPiecesPerCell));
        for (var step = 0; step < steps; step++)
            points.Add(PointOnPiece(start, startHandle, endHandle, end, (double)step / steps));
    }

    private static CanvasPoint PointOnPiece(CanvasPoint start, CanvasPoint startHandle, CanvasPoint endHandle,
        CanvasPoint end, double t)
    {
        var u = 1 - t;
        var startWeight = u * u * u;
        var startHandleWeight = 3 * u * u * t;
        var endHandleWeight = 3 * u * t * t;
        var endWeight = t * t * t;
        return new CanvasPoint(
            startWeight * start.X + startHandleWeight * startHandle.X + endHandleWeight * endHandle.X +
            endWeight * end.X,
            startWeight * start.Y + startHandleWeight * startHandle.Y + endHandleWeight * endHandle.Y +
            endWeight * end.Y);
    }

    private static CanvasPoint NeighbourPosition(IReadOnlyList<CurveAnchor> anchors, int index, bool isClosed,
        CanvasPoint itself)
    {
        if (index >= 0 && index < anchors.Count)
            return anchors[index].Position;

        if (!isClosed)
            return itself;

        return anchors[(index + anchors.Count) % anchors.Count].Position;
    }

    private static double Distance(CanvasPoint from, CanvasPoint to)
    {
        var across = to.X - from.X;
        var down = to.Y - from.Y;
        return Math.Sqrt(across * across + down * down);
    }
}
