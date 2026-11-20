// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

public sealed class SelectionShapesTests
{
    [Fact]
    public void AnEllipseInOneCell_IsThatCell()
    {
        var mask = SelectionShapes.Ellipse(new CanvasSize(3, 3), new CellRect(1, 1, 1, 1));

        Assert.Equal(["···", "·#·", "···"], TestMasks.Describe(mask));
    }

    [Fact]
    public void AnEllipse_HoldsTheCellsWhoseCentresAreInside()
    {
        var mask = SelectionShapes.Ellipse(new CanvasSize(5, 5), new CellRect(0, 0, 5, 5));

        Assert.Equal(["·###·", "#####", "#####", "#####", "·###·"], TestMasks.Describe(mask));
    }

    [Fact]
    public void AWideEllipse_FitsItsBox()
    {
        var mask = SelectionShapes.Ellipse(new CanvasSize(9, 3), new CellRect(1, 0, 7, 3));

        Assert.Equal(["··#####··", "·#######·", "··#####··"], TestMasks.Describe(mask));
    }

    [Fact]
    public void AnEllipsePartlyOffTheCanvas_KeepsThePartOnIt()
    {
        var mask = SelectionShapes.Ellipse(new CanvasSize(5, 4), new CellRect(-2, -2, 5, 5));

        Assert.Equal(["###··", "###··", "##···", "·····"], TestMasks.Describe(mask));
    }

    [Fact]
    public void AnEllipseOffTheCanvas_IsEmpty()
    {
        Assert.True(SelectionShapes.Ellipse(new CanvasSize(5, 4), new CellRect(6, 0, 3, 3)).IsEmpty);
    }

    [Fact]
    public void ATriangle_HoldsTheCellsWhoseCentresAreInsideOrOnItsEdges()
    {
        var mask = SelectionShapes.Polygon(new CanvasSize(6, 6), [new(0, 0), new(6, 0), new(0, 6)]);

        Assert.Equal(["######", "#####·", "####··", "###···", "##····", "#·····"], TestMasks.Describe(mask));
    }

    [Fact]
    public void AFigureEight_HoldsBothLoops()
    {
        var mask = SelectionShapes.Polygon(new CanvasSize(4, 4), [new(0, 0), new(4, 4), new(4, 0), new(0, 4)]);

        Assert.Equal(["#··#", "####", "####", "#··#"], TestMasks.Describe(mask));
    }

    [Fact]
    public void APathThatGoesRoundTwice_StillHoldsWhatItGoesRound()
    {
        CanvasPoint[] square = [new(0.2, 0.2), new(3.8, 0.2), new(3.8, 3.8), new(0.2, 3.8)];

        var mask = SelectionShapes.Polygon(new CanvasSize(5, 5), [.. square, .. square]);

        Assert.Equal(["####·", "####·", "####·", "####·", "·····"], TestMasks.Describe(mask));
    }

    [Fact]
    public void APolygonPartlyOffTheCanvas_KeepsThePartOnIt()
    {
        var mask = SelectionShapes.Polygon(new CanvasSize(4, 4),
            [new(-2, -2), new(2.2, -2), new(2.2, 2.2), new(-2, 2.2)]);

        Assert.Equal(["##··", "##··", "····", "····"], TestMasks.Describe(mask));
    }

    [Fact]
    public void APolygonOffTheCanvas_IsEmpty()
    {
        var mask = SelectionShapes.Polygon(new CanvasSize(4, 4), [new(5, 0), new(9, 0), new(9, 3)]);

        Assert.True(mask.IsEmpty);
    }

    [Fact]
    public void FewerThanThreeCorners_HoldNothing()
    {
        Assert.True(SelectionShapes.Polygon(new CanvasSize(4, 4), [new(0, 0), new(4, 4)]).IsEmpty);
    }
}
