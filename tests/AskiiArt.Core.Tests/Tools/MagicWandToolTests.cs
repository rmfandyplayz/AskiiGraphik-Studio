// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Tools;

public sealed class MagicWandToolTests
{
    // A cell 10 logical pixels wide, so moving one cell sideways moves the tolerance by 5.
    private const double CellWidth = 10;

    private readonly EditingSession _session = new(TestCells.CreateDocument(5, 3));
    private readonly ToolContext _context;
    private readonly MagicWandTool _tool;

    public MagicWandToolTests()
    {
        _context = TestTools.Context(_session, densities: TestTools.RampSteps);
        _tool = new MagicWandTool(_context);
    }

    private ToolOptionValues Options => _context.Settings.For(MagicWandOptions.Group);

    private string[] Selected => TestMasks.Describe(_session.Selection.Mask);

    [Fact]
    public void AClick_SelectsTheSameGlyphJoinedAlongSides_AsOneUndoStep()
    {
        TestCells.Paint(_session.ActiveLayer, "aab·a", "·a·aa", "a·a··");

        Click(0, 0);

        Assert.Equal(["##···", "·#···", "·····"], Selected);
        Assert.Equal(1, _session.History.UndoCount);
        Assert.Equal(UndoStepNames.MagicWand, _session.History.GetStepNameKey(0));
    }

    [Fact]
    public void Contiguous_FollowsWindingRuns_AboveAndBelow()
    {
        TestCells.Paint(_session.ActiveLayer, "a·aaa", "a·a·a", "aaa·a");

        Click(0, 0);

        Assert.Equal(["#·###", "#·#·#", "###·#"], Selected);
    }

    [Fact]
    public void Global_SelectsEveryMatchingCell()
    {
        TestCells.Paint(_session.ActiveLayer, "aab·a", "·a·aa", "a·a··");
        Options.Set(MagicWandOptions.Area, "global");

        Click(0, 0);

        Assert.Equal(["##··#", "·#·##", "#·#··"], Selected);
    }

    [Fact]
    public void ClickingAnEmptyCell_SelectsTheJoinedBlankCells_SpacesIncluded()
    {
        TestCells.Paint(_session.ActiveLayer, "a ·a·", "aa·aa", "··a··");

        Click(1, 0);

        Assert.Equal(["·##··", "··#··", "·····"], Selected);
    }

    [Fact]
    public void Lightness_MatchesGlyphsWithin_TheTolerance()
    {
        // In the test font, '*' is 0.6, 'A' 0.62, '#' 0.7 and '@' 0.9.
        TestCells.Paint(_session.ActiveLayer, "*A#@·");
        Options.Set(MagicWandOptions.Match, "lightness");
        Options.Set(MagicWandOptions.Area, "global");
        Options.Set(MagicWandOptions.Tolerance, 5);

        Click(0, 0);

        Assert.Equal(["##···", "·····", "·····"], Selected);

        Options.Set(MagicWandOptions.Tolerance, 10);
        Click(0, 0);

        Assert.Equal(["###··", "·····", "·····"], Selected);
    }

    [Fact]
    public void Lightness_CountsBlankCellsAsNoInk()
    {
        // In the test font, '.' is 0.1 and ':' 0.2.
        TestCells.Paint(_session.ActiveLayer, ".:·  ");
        Options.Set(MagicWandOptions.Match, "lightness");
        Options.Set(MagicWandOptions.Area, "global");
        Options.Set(MagicWandOptions.Tolerance, 10);

        Click(2, 0);

        Assert.Equal(["#·###", "#####", "#####"], Selected);
    }

    [Fact]
    public void TextColor_MatchesCloseColours_AndOnlyCellsWithInk()
    {
        var layer = _session.ActiveLayer;
        layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', TestCells.Rgb(200, 0, 0), CellColor.Unset));
        layer.SetCell(new CellPoint(1, 0), TestCells.Glyph('b', TestCells.Rgb(210, 10, 0), CellColor.Unset));
        layer.SetCell(new CellPoint(2, 0), TestCells.Glyph('c', TestCells.Rgb(250, 0, 0), CellColor.Unset));
        layer.SetCell(new CellPoint(3, 0), TestCells.Glyph('d'));
        Options.Set(MagicWandOptions.Match, "text-color");
        Options.Set(MagicWandOptions.Area, "global");
        Options.Set(MagicWandOptions.Tolerance, 10);

        Click(0, 0);

        Assert.Equal(["##···", "·····", "·····"], Selected);
    }

    [Fact]
    public void TextColor_TreatsAnAnsiColourAsItsRgb()
    {
        var red = AnsiPalette.Vga[AnsiColor.Red];
        var layer = _session.ActiveLayer;
        layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', TestCells.Ansi(AnsiColor.Red), CellColor.Unset));
        layer.SetCell(new CellPoint(1, 0),
            TestCells.Glyph('b', TestCells.Rgb(red.Red, red.Green, red.Blue), CellColor.Unset));
        Options.Set(MagicWandOptions.Match, "text-color");
        Options.Set(MagicWandOptions.Tolerance, 0);

        Click(0, 0);

        Assert.Equal(["##···", "·····", "·····"], Selected);
    }

    [Fact]
    public void BackgroundColor_MatchesCellsWithNoBackground_OnlyWithEachOther()
    {
        var layer = _session.ActiveLayer;
        layer.SetCell(new CellPoint(1, 0), Cell.FromBackground(TestCells.Rgb(0, 0, 0)));
        layer.SetCell(new CellPoint(2, 0), TestCells.Glyph('x'));
        Options.Set(MagicWandOptions.Match, "background-color");
        Options.Set(MagicWandOptions.Tolerance, 100);

        Click(0, 0);

        Assert.Equal(["#·###", "#####", "#####"], Selected);
    }

    [Fact]
    public void VisibleLayers_ComparesWhatTheCanvasShows()
    {
        TestCells.Paint(_session.ActiveLayer, "aaaaa");
        var top = TestCells.AddLayer(_session.Document);
        TestCells.Paint(top, "··b··");

        Click(0, 0);

        Assert.Equal(["#####", "·····", "·····"], Selected);

        Options.Set(MagicWandOptions.Sample, "visible-layers");
        Click(0, 0);

        Assert.Equal(["##···", "·····", "·····"], Selected);
    }

    [Fact]
    public void VisibleLayers_LeavesHiddenLayersOut()
    {
        TestCells.Paint(_session.ActiveLayer, "aaaaa");
        var top = TestCells.AddLayer(_session.Document);
        TestCells.Paint(top, "··b··");
        _session.History.Execute(new SetLayerPropertiesCommand(top.Id, top.Properties with { IsVisible = false }));
        Options.Set(MagicWandOptions.Sample, "visible-layers");

        Click(0, 0);

        Assert.Equal(["#####", "·····", "·····"], Selected);
    }

    [Fact]
    public void ThePress_PreviewsTheSelection_BeforeLettingGo()
    {
        TestCells.Paint(_session.ActiveLayer, "aab··");

        Press(0, 0);

        Assert.Equal(["##···", "·····", "·····"], TestMasks.Describe(_tool.Overlay.SelectionPreview!.Result));
        Assert.True(_session.Selection.IsEmpty);

        Release(0, 0);

        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void DraggingRight_RaisesTheTolerance_AndThePreviewFollows()
    {
        TestCells.Paint(_session.ActiveLayer, "*A#@·");
        Options.Set(MagicWandOptions.Match, "lightness");
        Options.Set(MagicWandOptions.Tolerance, 5);

        Press(0, 0);
        Move(1, 0);

        Assert.Equal(10, Options.Get(MagicWandOptions.Tolerance));
        Assert.Equal(["###··", "·····", "·····"], TestMasks.Describe(_tool.Overlay.SelectionPreview!.Result));

        Release(1, 0);

        Assert.Equal(["###··", "·····", "·····"], Selected);
        Assert.Equal(10, Options.Get(MagicWandOptions.Tolerance));
    }

    [Fact]
    public void DraggingLeft_LowersTheTolerance_NoLowerThanZero()
    {
        Options.Set(MagicWandOptions.Tolerance, 10);

        Press(4, 0);
        Move(3, 0);

        Assert.Equal(5, Options.Get(MagicWandOptions.Tolerance));

        Move(0, 0);

        Assert.Equal(0, Options.Get(MagicWandOptions.Tolerance));
    }

    [Fact]
    public void DraggingUpOrDown_LeavesTheToleranceAlone()
    {
        Options.Set(MagicWandOptions.Tolerance, 10);

        Press(2, 0);
        Move(2, 2);

        Assert.Equal(10, Options.Get(MagicWandOptions.Tolerance));
    }

    [Fact]
    public void Escape_CancelsTheClick_AndPutsTheToleranceBack()
    {
        TestCells.Paint(_session.ActiveLayer, "aab··");
        Options.Set(MagicWandOptions.Tolerance, 10);

        Press(0, 0);
        Move(2, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
        Assert.Equal(10, Options.Get(MagicWandOptions.Tolerance));
        Assert.Equal(ToolOverlay.None, _tool.Overlay);

        Release(2, 0);

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(0, _session.History.UndoCount);
    }

    [Fact]
    public void Escape_WithoutAClick_IsntTheTools()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void Deactivating_CancelsTheClick_AndPutsTheToleranceBack()
    {
        Options.Set(MagicWandOptions.Tolerance, 10);

        Press(0, 0);
        Move(3, 0);
        _tool.Deactivate();

        Assert.Equal(10, Options.Get(MagicWandOptions.Tolerance));
        Assert.Equal(ToolOverlay.None, _tool.Overlay);
    }

    [Fact]
    public void ShiftHeldAtThePress_AddsToTheSelection()
    {
        TestCells.Paint(_session.ActiveLayer, "aab··", "bbbbb", "··a··");
        Click(0, 0);

        Click(2, 2, ToolModifiers.Shift);

        Assert.Equal(["##···", "·····", "··#··"], Selected);
    }

    [Fact]
    public void AltHeldAtThePress_SubtractsFromTheSelection()
    {
        TestCells.Paint(_session.ActiveLayer, "aab··");
        SelectionEdits.SelectAll(_session);

        Click(0, 0, ToolModifiers.Alt);

        Assert.Equal(["··###", "#####", "#####"], Selected);
    }

    [Fact]
    public void AClickOffTheCanvas_Deselects_WhenReplacing()
    {
        SelectionEdits.SelectAll(_session);

        Click(7, 1);

        Assert.True(_session.Selection.IsEmpty);
    }

    [Fact]
    public void AnUndoDuringTheClick_ShowsAtOnce_AndTheWandComparesTheCellsAsTheyAreAgain()
    {
        TestCells.Paint(_session.ActiveLayer, "aaaaa");
        SelectionEdits.Select(_session, SelectionMask.Rectangle(_session.Document.CanvasSize, new CellRect(0, 2, 1, 1)),
            UndoStepNames.RectangleSelect);
        var paint = new CellEditBuilder(_session.Document, _session.ActiveLayer.Id);
        paint.Set(new CellPoint(2, 0), TestCells.Glyph('b'));
        _session.History.Execute(paint.Build(UndoStepNames.Paint)!);

        Press(0, 0, ToolModifiers.Shift);

        Assert.Equal(["##···", "·····", "#····"], TestMasks.Describe(_tool.Overlay.SelectionPreview!.Result));

        _session.History.Undo();

        Assert.Equal(["#####", "·····", "#····"], TestMasks.Describe(_tool.Overlay.SelectionPreview!.Result));

        _session.History.Undo();

        Assert.Equal(["#####", "·····", "·····"], TestMasks.Describe(_tool.Overlay.SelectionPreview!.Result));
    }

    [Fact]
    public void OptionsChangedBetweenClicks_ApplyToTheNextOne()
    {
        TestCells.Paint(_session.ActiveLayer, "ab·ba");
        Click(0, 0);
        Options.Set(MagicWandOptions.Area, "global");

        Click(0, 0);

        Assert.Equal(["#···#", "·····", "·····"], Selected);
    }

    [Fact]
    public void TheWand_SelectsOnALockedLayer()
    {
        TestCells.Paint(_session.ActiveLayer, "aab··");
        var layer = _session.ActiveLayer;
        _session.History.Execute(new SetLayerPropertiesCommand(layer.Id, layer.Properties with { IsLocked = true }));

        Click(0, 0);

        Assert.Equal(["##···", "·····", "·····"], Selected);
    }

    [Fact]
    public void ADefinition_ShowsTheModeThenTheWandsOwnOptions()
    {
        Assert.Equal([SelectionToolOptions.Group, MagicWandOptions.Group], MagicWandTool.Definition.OptionGroups);
        Assert.Equal(
            [MagicWandOptions.Area, MagicWandOptions.Sample, MagicWandOptions.Match, MagicWandOptions.Tolerance],
            MagicWandOptions.Group.Options);
    }

    [Fact]
    public void TheDefaults_AreContiguousSameGlyphOnTheActiveLayer_At10Percent()
    {
        Assert.Equal(new MagicWandSettings(true, false, MagicWandMatch.Glyph, 10),
            MagicWandOptions.Read(new ToolOptionValues(MagicWandOptions.Group)));
    }

    private static ToolPointerInput Input(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers, CellWidthOnScreen: CellWidth);

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(Input(x, y, modifiers));

    private void Move(int x, int y) => _tool.PointerMoved(Input(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(Input(x, y));

    private void Click(int x, int y, ToolModifiers modifiers = ToolModifiers.None)
    {
        Press(x, y, modifiers);
        Release(x, y);
    }
}
