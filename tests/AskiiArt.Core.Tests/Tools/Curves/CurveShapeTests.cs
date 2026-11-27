// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Curves;

namespace AskiiArt.Core.Tests.Tools.Curves;

// A curve's shape (ADR 0083): smooth anchors' handles from their neighbours, and the path through the anchors.
public sealed class CurveShapeTests
{
    private const double Precision = 1e-9;

    [Fact]
    public void ASmoothAnchorsHandles_LieAlongTheLineFromTheAnchorBeforeToTheOneAfter()
    {
        CurveAnchor[] anchors = [Smooth(0, 0), Smooth(3, 3), Smooth(6, 0)];

        var middle = CurveShape.Resolve(anchors, isClosed: false)[1];

        AssertNear(new CanvasPoint(2, 3), middle.In);
        AssertNear(new CanvasPoint(4, 3), middle.Out);
    }

    [Fact]
    public void AnOpenCurvesEnds_CountThemselvesAsTheMissingNeighbour()
    {
        CurveAnchor[] anchors = [Smooth(0, 0), Smooth(6, 0)];

        var resolved = CurveShape.Resolve(anchors, isClosed: false);

        AssertNear(new CanvasPoint(1, 0), resolved[0].Out);
        AssertNear(new CanvasPoint(5, 0), resolved[1].In);
    }

    [Fact]
    public void AClosedCurve_TakesTheLastAnchorAsTheFirstOnesNeighbour()
    {
        CurveAnchor[] anchors = [Smooth(0, 0), Smooth(6, 0), Smooth(6, 6)];

        var first = CurveShape.Resolve(anchors, isClosed: true)[0];

        AssertNear(new CanvasPoint(0, 1), first.In);
        AssertNear(new CanvasPoint(0, -1), first.Out);
    }

    [Fact]
    public void OtherAnchors_KeepTheirOwnHandles()
    {
        var mirrored = Smooth(3, 3).WithHandle(CurveHandleSide.Out, new CanvasPoint(5, 1), unlinks: false);
        CurveAnchor[] anchors = [Smooth(0, 0), mirrored, CurveAnchor.Corner(new CanvasPoint(6, 0))];

        var resolved = CurveShape.Resolve(anchors, isClosed: false);

        Assert.Equal(mirrored, resolved[1]);
        Assert.Equal(anchors[2], resolved[2]);
        AssertNear(new CanvasPoint(1, 5), mirrored.In);
    }

    [Fact]
    public void ThePath_RunsThroughEveryAnchor_InShortPieces()
    {
        CurveAnchor[] anchors = [Smooth(0, 0), Smooth(4, 4), Smooth(8, 0)];

        var path = CurveShape.PathOf(CurveShape.Resolve(anchors, isClosed: false), isClosed: false);

        Assert.False(path.IsClosed);
        Assert.Equal(new CanvasPoint(0, 0), path.Points[0]);
        Assert.Contains(new CanvasPoint(4, 4), path.Points);
        Assert.Equal(new CanvasPoint(8, 0), path.Points[^1]);
        for (var index = 0; index < path.PieceCount; index++)
        {
            var (from, to) = path.Piece(index);
            Assert.True(Math.Abs(to.X - from.X) <= 0.5 && Math.Abs(to.Y - from.Y) <= 0.5);
        }
    }

    [Fact]
    public void TwoSmoothAnchors_MakeAStraightPath()
    {
        CurveAnchor[] anchors = [Smooth(0, 1), Smooth(10, 1)];

        var path = CurveShape.PathOf(CurveShape.Resolve(anchors, isClosed: false), isClosed: false);

        Assert.All(path.Points, point => Assert.Equal(1, point.Y, Precision));
    }

    [Fact]
    public void AClosedPath_JoinsBackToItsFirstAnchor()
    {
        CurveAnchor[] anchors = [Smooth(0, 0), Smooth(6, 0), Smooth(6, 6)];

        var path = CurveShape.PathOf(CurveShape.Resolve(anchors, isClosed: true), isClosed: true);

        Assert.True(path.IsClosed);
        Assert.Equal(new CanvasPoint(0, 0), path.Points[0]);
        Assert.NotEqual(new CanvasPoint(0, 0), path.Points[^1]);
    }

    private static CurveAnchor Smooth(double x, double y) => CurveAnchor.Smooth(new CanvasPoint(x, y));

    private static void AssertNear(CanvasPoint expected, CanvasPoint actual)
    {
        Assert.Equal(expected.X, actual.X, Precision);
        Assert.Equal(expected.Y, actual.Y, Precision);
    }
}
