// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Selection;

/// <summary>Selections that reach past the canvas's edges, and the zone a move can take them into (ADR 0071).</summary>
public sealed class SelectionPastCanvasTests
{
    private static readonly CanvasSize _canvas = new(6, 3);

    // With cells half as wide as they're tall: 10 columns and 5 rows past each edge.
    private static readonly CellRect _zone = SelectionZone.Around(_canvas, 0.5);

    [Theory]
    [InlineData(0.5, 5)]
    [InlineData(1.0, 10)]
    [InlineData(0.04, 1)]
    public void TheZone_ReachesAsFarOnScreenAboveAndBelow_AsItsTenColumnsDo(double cellWidthToHeight, int rows)
    {
        Assert.Equal(rows, SelectionZone.RowsPastCanvas(cellWidthToHeight));
        Assert.Equal(CellRect.FromEdges(-10, -rows, 16, 3 + rows), SelectionZone.Around(_canvas, cellWidthToHeight));
    }

    [Fact]
    public void MovedWithin_KeepsCellsPastTheCanvas_InsideTheArea()
    {
        var moved = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-3, -1), _zone);

        Assert.Equal(new CellRect(-3, -1, 2, 1), moved.Bounds);
        Assert.True(moved.Contains(new CellPoint(-2, -1)));
        Assert.False(moved.IsEmpty);
        Assert.False(moved.HasCellsOnCanvas);
        Assert.True(moved.ReachesPastCanvas);
    }

    [Fact]
    public void MovedWithin_DropsCellsOutsideTheArea()
    {
        var moved = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-11, 0), _zone);

        Assert.Equal(new CellRect(-10, 0, 1, 1), moved.Bounds);
    }

    [Fact]
    public void MovedOnto_StillKeepsToTheCanvas()
    {
        Assert.Equal(new CellRect(0, 0, 1, 1), FirstTwoCells().MovedOnto(_canvas, new CellPoint(-1, 0)).Bounds);
    }

    [Fact]
    public void CutToCanvas_LeavesOutTheCellsPastTheEdges()
    {
        var shape = TestMasks.Parse("##····", "#·····", "······").MovedWithin(_canvas, new CellPoint(-1, 0), _zone);

        var onCanvas = shape.CutToCanvas();

        Assert.Equal(["#·····", "······", "······"], TestMasks.Describe(onCanvas));
        Assert.True(shape.HasCellsOnCanvas);
        Assert.False(onCanvas.ReachesPastCanvas);
        Assert.Same(onCanvas, onCanvas.CutToCanvas());
    }

    [Fact]
    public void TheOutline_FollowsTheCellsPastTheCanvas_RatherThanClosingAlongItsEdge()
    {
        var moved = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone);

        Assert.Contains(new CellEdge(CellEdgeSide.Left, -1, 0, 1), moved.Edges);
        Assert.DoesNotContain(moved.Edges, edge => edge is { Side: CellEdgeSide.Left, Column: 0 });
    }

    [Fact]
    public void CombinedWith_CountsCellsPastTheCanvas()
    {
        var pastTheEdge = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone);
        var third = SelectionMask.Rectangle(_canvas, new CellRect(2, 0, 1, 1));

        Assert.Equal(new CellRect(-1, 0, 4, 1), pastTheEdge.CombinedWith(third, SelectionCombineMode.Add).Bounds);
        Assert.Equal(new CellRect(-1, 0, 1, 1),
            pastTheEdge.CombinedWith(pastTheEdge.CutToCanvas(), SelectionCombineMode.Subtract).Bounds);
    }

    [Fact]
    public void InvertGrowAndShrink_WorkOnTheCanvasOnly()
    {
        var pastTheEdge = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone);

        Assert.Equal(["·#####", "######", "######"], TestMasks.Describe(pastTheEdge.Inverted()));
        Assert.False(pastTheEdge.Inverted().ReachesPastCanvas);
        Assert.Equal(new CellRect(0, 0, 2, 2), pastTheEdge.Grown().Bounds);
        Assert.False(pastTheEdge.Shrunk().ReachesPastCanvas);
    }

    [Theory]
    [InlineData(SelectionCombineMode.Replace)]
    [InlineData(SelectionCombineMode.Add)]
    [InlineData(SelectionCombineMode.Subtract)]
    [InlineData(SelectionCombineMode.Intersect)]
    public void ASelectionTool_DropsThePartPastTheCanvas_InEveryMode(SelectionCombineMode mode)
    {
        var before = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone);
        var shape = SelectionMask.Rectangle(_canvas, new CellRect(0, 0, 3, 1));

        var preview = new SelectionPreview(before, mode, shape);

        Assert.False(preview.Result.ReachesPastCanvas);
        Assert.True(preview.Lost.Contains(new CellPoint(-1, 0)));
    }

    [Fact]
    public void AClickInAddMode_DropsThePartPastTheCanvas()
    {
        var before = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone);

        var preview = new SelectionPreview(before, SelectionCombineMode.Add, () => null);

        Assert.Equal(new CellRect(0, 0, 1, 1), preview.Result.Bounds);
    }

    [Fact]
    public void ASelectionAllPastTheCanvas_StillCounts_SoToolsPaintNothing_AndTheresNothingToClearOrCopy()
    {
        var session = SessionWithArt();
        Select(session, FirstTwoCells().MovedWithin(_canvas, new CellPoint(-3, 0), _zone));
        var layer = session.ActiveLayer;

        var cells = session.EditCellsInSelection(layer.Id);
        cells.Set(new CellPoint(2, 1), Cell.FromGlyph(new Rune('z')));

        Assert.False(session.Selection.IsEmpty);
        Assert.Null(cells.Build());
        Assert.False(SelectionContents.CanClear(session));
        Assert.Equal(string.Empty, new TextExporter().ToText(session.Document, "\n", session.Selection.Mask));
    }

    [Fact]
    public void ClearCopyAndCopyAsText_TakeTheSelectionsCellsOnTheCanvas()
    {
        var session = SessionWithArt();
        Select(session, FirstTwoCells().MovedWithin(_canvas, new CellPoint(-1, 0), _zone));

        var clip = CellClip.Copy(session.ActiveLayer, session.Selection.Mask);
        var text = new TextExporter().ToText(session.Document, "\n", session.Selection.Mask);
        Assert.True(SelectionContents.Clear(session));

        Assert.Equal("a", clip.ToText("\n"));
        Assert.Equal(new CellPoint(0, 0), clip.Origin);
        Assert.Equal("a", text);
        Assert.Equal(["·bcdef", "ghijkl", "mnopqr"], TestCells.Read(session.ActiveLayer));
    }

    [Fact]
    public void ResizingTheCanvas_KeepsTheCellsThatLandOnIt_AndDropsTheRest()
    {
        var pastTheEdge = FirstTwoCells().MovedWithin(_canvas, new CellPoint(-2, 0), _zone);
        var plan = CanvasResizePlan.Create(_canvas, new CanvasSize(7, 3), ResizeAnchor.TopRight);

        var resized = pastTheEdge.Resized(plan);

        Assert.Equal(new CellRect(0, 0, 1, 1), resized.Bounds);
        Assert.False(resized.ReachesPastCanvas);
    }

    private static SelectionMask FirstTwoCells() => SelectionMask.Rectangle(_canvas, new CellRect(0, 0, 2, 1));

    private static EditingSession SessionWithArt()
    {
        var session = new EditingSession(TestCells.CreateDocument(_canvas.Width, _canvas.Height));
        TestCells.Paint(session.ActiveLayer, "abcdef", "ghijkl", "mnopqr");
        return session;
    }

    private static void Select(EditingSession session, SelectionMask mask) =>
        session.History.Execute(new SetSelectionCommand(session.Selection, mask, UndoStepNames.Move));
}
