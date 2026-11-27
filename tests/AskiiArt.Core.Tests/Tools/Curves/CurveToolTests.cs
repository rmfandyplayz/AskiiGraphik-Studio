// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Curves;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools.Curves;

// The Curve tool (ADR 0083): clicks place anchors, the curve shows in the document as it's built, and a finished curve
// is one undo step. Cells here are half as wide as they're tall, as in most fonts, and a marker reaches half a cell.
public sealed class CurveToolTests
{
    private const double HalfAsWide = 0.5;
    private const double Precision = 1e-9;
    private static readonly ToolMarkerReach _reach = new(0.5, 0.5);
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly string[] _empty = [.. Enumerable.Repeat("············", 6)];

    private readonly EditingSession _session;
    private readonly ToolContext _context;
    private readonly CurveTool _tool;

    public CurveToolTests()
    {
        _session = new EditingSession(
            Document.Create(new CanvasSize(12, 6), "Layer", CharacterSet.Ascii, colorMode: ColorMode.Ansi));
        _context = TestTools.Context(_session, HalfAsWide);
        _context.Colors.Foreground = _red;
        _context.Colors.Background = _blue;
        _tool = new CurveTool(_context);
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    private ToolOptionValues Style => _context.Settings.For(LineStyleOptions.Group);

    private ToolOptionValues Character => _context.Settings.For(CharacterBrushOptions.Group);

    [Fact]
    public void TwoClicks_DrawAStraightLine_AndEnterFinishesIt_AsOneStep()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);

        Assert.Equal("·------·····", TestCells.Read(Layer)[1]);
        Assert.True(_tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));

        Assert.False(_tool.IsDrawing);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
        Assert.Equal("·------·····", TestCells.Read(Layer)[1]);
        Assert.Equal(TestCells.Glyph('-', _red, _blue), Layer.GetCell(new CellPoint(1, 1)));
        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Curve, History.GetStepNameKey(0));
    }

    [Fact]
    public void APieceOfCurve_FollowsThePointer_ToWhereTheNextAnchorWouldGo()
    {
        Click(1.5, 1.5);

        Move(8.5, 1.5);
        Assert.Equal("·--------···", TestCells.Read(Layer)[1]);

        Move(3.5, 1.5);
        Assert.Equal("·---········", TestCells.Read(Layer)[1]);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ClickedAnchors_MakeASmoothCurveThroughThem()
    {
        Click(0.5, 4.5);
        Click(5.5, 0.5);
        Click(10.5, 4.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(
        [
            "····/-\\·····",
            "···/···\\····",
            "··/·····\\···",
            "·/·······\\··",
            "/·········\\·",
            "············"
        ], TestCells.Read(Layer));
    }

    [Fact]
    public void AnAltClick_PlacesACorner()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5, ToolModifiers.Alt);
        Click(6.5, 4.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(
        [
            "············",
            "·-----+·····",
            "······|·····",
            "······|·····",
            "······|·····",
            "············"
        ], TestCells.Read(Layer));
    }

    [Fact]
    public void WithoutAlt_TheCurveRoundsTheAnchor()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Click(6.5, 4.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.NotEqual(new Rune('+'), Layer.GetCell(new CellPoint(6, 1)).Glyph);
    }

    [Fact]
    public void ClickingTheFirstAnchor_ClosesTheCurve_AndFinishesIt()
    {
        Click(1.5, 1.5, ToolModifiers.Alt);
        Click(6.5, 1.5, ToolModifiers.Alt);
        Click(6.5, 4.5, ToolModifiers.Alt);
        Click(1.5, 4.5, ToolModifiers.Alt);

        Click(1.5, 1.5);

        Assert.False(_tool.IsDrawing);
        Assert.Equal(
        [
            "············",
            "·+----+·····",
            "·|····|·····",
            "·|····|·····",
            "·+----+·····",
            "············"
        ], TestCells.Read(Layer));
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ADoubleClick_PlacesItsAnchor_AndFinishes()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Click(6.5, 1.5, clickCount: 2);

        Assert.False(_tool.IsDrawing);
        Assert.Equal("·------·····", TestCells.Read(Layer)[1]);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void Backspace_TakesTheLastAnchorAway()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Click(6.5, 4.5);

        Assert.True(_tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None));
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal([_empty[0], "·------·····", .. _empty[2..]], TestCells.Read(Layer));
    }

    [Fact]
    public void Undo_TakesTheLastAnchorAway_WhileACurveIsDrawn()
    {
        Assert.False(_tool.TakesUndo);
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Click(6.5, 4.5);
        Assert.True(_tool.TakesUndo);

        _tool.Undo();
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal([_empty[0], "·------·····", .. _empty[2..]], TestCells.Read(Layer));
        Assert.False(_tool.TakesUndo);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void TakingAwayTheOnlyAnchor_ThrowsTheCurveAway()
    {
        Click(1.5, 1.5);
        Move(6.5, 1.5);

        _tool.Undo();

        Assert.False(_tool.IsDrawing);
        Assert.Equal(_empty, TestCells.Read(Layer));
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void Esc_ThrowsTheWholeCurveAway()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Move(6.5, 4.5);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.False(_tool.IsDrawing);
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
        Assert.Equal(_empty, TestCells.Read(Layer));
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void KeysWithoutACurve_AreLeftToTheApp()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Enter, ToolModifiers.None));
        Assert.False(_tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None));
    }

    [Fact]
    public void SwitchingTools_KeepsTheCurve_WithoutThePieceToThePointer()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);
        Move(6.5, 4.5);

        _tool.Deactivate();

        Assert.False(_tool.IsDrawing);
        Assert.Equal([_empty[0], "·------·····", .. _empty[2..]], TestCells.Read(Layer));
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ACurveOfOneAnchor_DrawsNothing()
    {
        Click(1.5, 1.5);

        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(_empty, TestCells.Read(Layer));
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void DraggingAsAnAnchorIsPlaced_PullsOutHandles_EachTheOthersMirrorImage()
    {
        Click(1.5, 1.5);

        Press(6.5, 3.5);
        Move(8.5, 1.5);

        Assert.Equal([new(4.5, 5.5), new(8.5, 1.5)], HandleEndsOf(new CanvasPoint(6.5, 3.5)));
    }

    [Fact]
    public void AltDuringTheDrag_UnlinksTheHandles()
    {
        Click(1.5, 1.5);
        Press(6.5, 3.5);
        Move(8.5, 1.5);

        Move(8.5, 0.5, ToolModifiers.Alt);

        Assert.Equal([new(4.5, 5.5), new(8.5, 0.5)], HandleEndsOf(new CanvasPoint(6.5, 3.5)));
    }

    [Fact]
    public void DraggingFromAnAltClick_PullsOutOnlyTheHandleAfterIt()
    {
        Click(1.5, 1.5);

        Press(6.5, 3.5, ToolModifiers.Alt);
        Move(8.5, 1.5);

        Assert.Equal([new(8.5, 1.5)], HandleEndsOf(new CanvasPoint(6.5, 3.5)));
    }

    [Fact]
    public void AnAnchorPlacedWithoutLeavingItsMarker_GetsNoHandlesOfItsOwn()
    {
        Click(1.5, 1.5);

        Press(6.5, 3.5);
        Move(6.7, 3.4);
        Release(6.7, 3.4);
        Click(10.5, 3.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal(1, History.UndoCount);
        Assert.NotEqual(new Rune('+'), Layer.GetCell(new CellPoint(6, 3)).Glyph);
    }

    [Fact]
    public void Shift_KeepsTheNextAnchorInLineWithTheLast()
    {
        Click(1.5, 1.5);
        Click(6.5, 2.5, ToolModifiers.Shift);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal([_empty[0], "·------·····", .. _empty[2..]], TestCells.Read(Layer));
    }

    [Fact]
    public void ShiftWhileDraggingAHandle_KeepsItTo45DegreeSteps_AsTheyLookOnScreen()
    {
        Click(1.5, 1.5);
        Press(6.5, 3.5);

        Move(9, 2, ToolModifiers.Shift);

        var end = HandleEndsOf(new CanvasPoint(6.5, 3.5))[^1];
        Assert.Equal((end.X - 6.5) * HalfAsWide, 3.5 - end.Y, Precision);
    }

    [Fact]
    public void DraggingAnAnchor_MovesIt()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);

        Press(6.5, 1.5);
        Move(6.5, 3.5);
        Release(6.5, 3.5);

        Assert.True(_tool.IsDrawing);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);
        Assert.True(Layer.GetCell(new CellPoint(6, 1)).IsEmpty);
        Assert.False(Layer.GetCell(new CellPoint(6, 3)).IsEmpty);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void DraggingAHandle_ReshapesTheCurve_AndMirrorsTheOther()
    {
        Click(1.5, 1.5);
        Press(6.5, 3.5);
        Move(8.5, 3.5);
        Release(8.5, 3.5);

        Press(8.5, 3.5);
        Move(8.5, 1.5);
        Release(8.5, 1.5);

        Assert.Equal([new(4.5, 5.5), new(8.5, 1.5)], HandleEndsOf(new CanvasPoint(6.5, 3.5)));
    }

    [Fact]
    public void AltWhileDraggingAHandle_UnlinksItFromTheOther()
    {
        Click(1.5, 1.5);
        Press(6.5, 3.5);
        Move(8.5, 3.5);
        Release(8.5, 3.5);

        Press(8.5, 3.5);
        Move(8.5, 1.5, ToolModifiers.Alt);

        Assert.Equal([new(4.5, 3.5), new(8.5, 1.5)], HandleEndsOf(new CanvasPoint(6.5, 3.5)));
    }

    [Fact]
    public void EachAnchor_HasAMarker_TheFirstClosingTheCurveOnceThereAreTwo()
    {
        Click(1.5, 1.5);
        Assert.Equal([new ToolMarker(new CanvasPoint(1.5, 1.5), ToolMarkerKind.Corner)], _tool.Overlay.Markers!);

        Click(6.5, 1.5);
        Assert.Equal(
            [ToolMarkerKind.Finish, ToolMarkerKind.Corner], _tool.Overlay.Markers!.Select(marker => marker.Kind));

        Move(1.5, 1.5);
        Assert.Equal(ToolMarkerKind.FinishUnderPointer, _tool.Overlay.Markers![0].Kind);
    }

    [Fact]
    public void TheSelectedAnchorsHandles_AndTheOneUnderThePointers_AreStrong()
    {
        var middle = new CanvasPoint(5.5, 0.5);
        Click(0.5, 4.5);
        Click(5.5, 0.5);
        Click(10.5, 4.5);
        Assert.All(HandlesOf(middle), handle => Assert.False(handle.IsStrong));

        Move(5.5, 0.5);
        Assert.All(HandlesOf(middle), handle => Assert.True(handle.IsStrong));

        Click(5.5, 0.5);
        Move(8.5, 5.5);
        Assert.Equal(2, HandlesOf(middle).Length);
        Assert.All(HandlesOf(middle), handle => Assert.True(handle.IsStrong));
    }

    [Fact]
    public void ASmoothEndsHandlePastTheCurve_DoesntShow()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);

        Assert.Equal([new(2.333333333333333, 1.5)], HandleEndsOf(new CanvasPoint(1.5, 1.5)), new PointsNear());
        Assert.Equal([new(5.666666666666667, 1.5)], HandleEndsOf(new CanvasPoint(6.5, 1.5)), new PointsNear());
    }

    [Fact]
    public void AnUndoFromElsewhere_EndsTheCurveWhereItIs()
    {
        Click(1.5, 1.5);
        Click(6.5, 1.5);

        History.Undo();
        Move(3.5, 3.5);

        Assert.False(_tool.IsDrawing);
        Assert.Equal(_empty, TestCells.Read(Layer));
        Assert.True(History.CanRedo);
    }

    [Fact]
    public void WithIncludeJoins_TheCurveJoinsTheLinesThere()
    {
        TestCells.Paint(Layer, "····|·······", "····|·······", "····|·······", "····|·······", "············",
            "············");
        _context.Settings.For(LineStyleOptions.JoinsGroup).Set(LineStyleOptions.IncludeJoins, true);

        Click(1.5, 2.5);
        Click(7.5, 2.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal("·---+---····", TestCells.Read(Layer)[2]);
    }

    [Fact]
    public void InSingleCharacter_TheCurveIsTheCharacter_WhichJoinsTheRecentOnes()
    {
        Style.Set(LineStyleOptions.Style, "singleCharacter");
        Character.Set(CharacterBrushOptions.Character, new Rune('*'));

        Click(1.5, 1.5);
        Click(4.5, 1.5);
        _tool.KeyPressed(ToolKey.Enter, ToolModifiers.None);

        Assert.Equal("·****·······", TestCells.Read(Layer)[1]);
        Assert.Equal(new Rune('*'), _context.Settings.RecentGlyphs.Glyphs[0]);
    }

    [Fact]
    public void TheCurve_FollowsTheStyleAsItChanges_WhileItsDrawn()
    {
        Click(1.5, 1.5);
        Click(4.5, 1.5);

        Character.Set(CharacterBrushOptions.Character, new Rune('#'));
        Style.Set(LineStyleOptions.Style, "singleCharacter");
        Move(4.5, 1.5);

        Assert.Equal("·####·······", TestCells.Read(Layer)[1]);
    }

    [Fact]
    public void ALockedLayer_StartsNoCurve()
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));

        Click(1.5, 1.5);

        Assert.False(_tool.IsDrawing);
    }

    [Fact]
    public void TheCurve_HasTheLinesStyle_IncludeJoins_AndTheCharacter()
    {
        Assert.Equal([LineStyleOptions.Group, LineStyleOptions.JoinsGroup, CharacterBrushOptions.Group],
            CurveTool.Definition.OptionGroups);
    }

    private ToolHandle[] HandlesOf(CanvasPoint anchor) =>
        [.. _tool.Overlay.Handles!.Where(handle => handle.Anchor == anchor)];

    // The anchor's handles' ends, the one before it first.
    private CanvasPoint[] HandleEndsOf(CanvasPoint anchor) => [.. HandlesOf(anchor).Select(handle => handle.End)];

    private void Click(double x, double y, ToolModifiers modifiers = ToolModifiers.None, int clickCount = 1)
    {
        _tool.PointerPressed(Input(x, y, modifiers, clickCount));
        _tool.PointerReleased(Input(x, y, modifiers));
    }

    private void Press(double x, double y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(Input(x, y, modifiers));

    private void Move(double x, double y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerMoved(Input(x, y, modifiers));

    private void Release(double x, double y) => _tool.PointerReleased(Input(x, y));

    private static ToolPointerInput Input(double x, double y, ToolModifiers modifiers = ToolModifiers.None,
        int clickCount = 1)
    {
        var cell = new CellPoint((int)Math.Floor(x), (int)Math.Floor(y));
        return new ToolPointerInput(cell, x - cell.X, y - cell.Y, modifiers, ClickCount: clickCount,
            MarkerReach: _reach);
    }

    private sealed class PointsNear : IEqualityComparer<CanvasPoint>
    {
        public bool Equals(CanvasPoint first, CanvasPoint second) =>
            Math.Abs(first.X - second.X) < Precision && Math.Abs(first.Y - second.Y) < Precision;

        public int GetHashCode(CanvasPoint point) => 0;
    }
}
