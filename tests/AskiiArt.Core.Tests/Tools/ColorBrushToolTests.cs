// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools;

public sealed class ColorBrushToolTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    private readonly EditingSession _session;
    private readonly PaintColors _colors = new();
    private readonly ToolOptionValues _tipValues = new(BrushTool.TipOptions);
    private ColorBrushTool _tool;

    public ColorBrushToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3, ColorMode.Ansi));
        TestCells.Paint(Layer, "abc···", "ghi···", "mno···");
        _tool = new ColorBrushTool(PaintColorTarget.Foreground, _session, _colors, TestTools.Tip(_tipValues));
        _colors.Foreground = _red;
        _colors.Background = _blue;
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void TheFGBrush_ColorsTheTextOfCellsWithAGlyph_AndSkipsEmptyCells()
    {
        Press(0, 0);
        Move(5, 0);
        Release(5, 0);

        Assert.Equal(TestCells.Glyph('a', _red, CellColor.Unset), CellAt(0, 0));
        Assert.Equal(TestCells.Glyph('c', _red, CellColor.Unset), CellAt(2, 0));
        Assert.True(CellAt(3, 0).IsEmpty);
        Assert.Equal(["abc···", "ghi···", "mno···"], TestCells.Read(Layer));
    }

    [Fact]
    public void TheBGBrush_ColorsTheBackground_OfAnyCell()
    {
        UseTheBGBrush();

        Press(2, 1);
        Move(3, 1);
        Release(3, 1);

        Assert.Equal(TestCells.Glyph('i', CellColor.Unset, _blue), CellAt(2, 1));
        Assert.Equal(Cell.FromBackground(_blue), CellAt(3, 1));
    }

    [Fact]
    public void ColorsOfNone_TakeTheColorsAway()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _red, _blue));
        Layer.SetCell(new CellPoint(3, 0), Cell.FromBackground(_blue));
        _colors.Reset();

        Press(0, 0);
        Release(0, 0);
        UseTheBGBrush();
        Press(0, 0);
        Move(3, 0);
        Release(3, 0);

        Assert.Equal(TestCells.Glyph('a'), CellAt(0, 0));
        Assert.True(CellAt(3, 0).IsEmpty);
    }

    [Fact]
    public void TheColor_IsConvertedToTheDocumentsColorMode()
    {
        _colors.Foreground = TestCells.Rgb(250, 10, 10);

        Press(0, 0);
        Release(0, 0);

        var nearest = CellColor.FromAnsi(AnsiPalette.Vga.FindNearest(new RgbColor(250, 10, 10)));
        Assert.Equal(nearest, CellAt(0, 0).Foreground);
    }

    [Fact]
    public void AStroke_KeepsItsColor_WhenTheColorChangesDuringIt()
    {
        Press(0, 0);
        _colors.Foreground = _green;
        Move(2, 0);
        Release(2, 0);

        Assert.Equal(_red, CellAt(2, 0).Foreground);
        Assert.False(CellAt(2, 0).HasBackground);
    }

    // The colour the windows change doesn't matter: each brush paints its own colour (ADR 0049).
    [Fact]
    public void EachBrush_PaintsItsOwnColor_WhicheverColorIsChosen()
    {
        _colors.Target = PaintColorTarget.Background;
        Press(0, 0);
        Release(0, 0);

        UseTheBGBrush();
        _colors.Target = PaintColorTarget.Foreground;
        Press(1, 0);
        Release(1, 0);

        Assert.Equal(TestCells.Glyph('a', _red, CellColor.Unset), CellAt(0, 0));
        Assert.Equal(TestCells.Glyph('b', CellColor.Unset, _blue), CellAt(1, 0));
    }

    [Fact]
    public void TheirDescriptors_AreTheFGAndBGBrushes()
    {
        Assert.Equal(ColorBrushTool.ForegroundId, _tool.Descriptor.Id);

        UseTheBGBrush();

        Assert.Equal(ColorBrushTool.BackgroundId, _tool.Descriptor.Id);
    }

    [Fact]
    public void OneStroke_IsOneUndoStep_NamedPaint()
    {
        Press(0, 0);
        Move(2, 0);
        Move(2, 2);
        Release(2, 2);

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Paint, History.GetStepNameKey(0));

        History.Undo();
        Assert.True(CellAt(1, 0).Foreground.IsUnset);
        Assert.True(CellAt(2, 2).Foreground.IsUnset);

        History.Redo();
        Assert.Equal(_red, CellAt(2, 1).Foreground);
    }

    [Fact]
    public void AStrokeThatChangesNothing_LeavesNoStep()
    {
        Press(4, 0);
        Move(5, 2);
        Release(5, 2);

        Assert.False(History.CanUndo);
    }

    [Fact]
    public void AMonochromeDocument_IsLeftAlone()
    {
        History.Execute(new SetColorModeCommand(ColorMode.Monochrome));
        var stepsBefore = History.UndoCount;
        UseTheBGBrush();

        Press(0, 0);
        Move(3, 0);
        Release(3, 0);

        Assert.Equal(stepsBefore, History.UndoCount);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ALockedOrHiddenLayer_IsLeftAlone(bool isLocked, bool isHidden)
    {
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with
        {
            IsLocked = isLocked,
            IsVisible = !isHidden
        }));
        var stepsBefore = History.UndoCount;

        Press(0, 0);
        Move(2, 0);
        Release(2, 0);

        Assert.True(CellAt(0, 0).Foreground.IsUnset);
        Assert.Equal(stepsBefore, History.UndoCount);
    }

    [Fact]
    public void Esc_TakesTheStrokeBack_AndLeavesNoStep()
    {
        Press(0, 0);
        Move(2, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.True(CellAt(0, 0).Foreground.IsUnset);
        Assert.Equal(0, History.StepCount);

        Move(2, 2);
        Release(2, 2);
        Assert.Equal(0, History.StepCount);
    }

    [Fact]
    public void Esc_WithNoStroke_IsntUsed()
    {
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    // The App chooses the group's tools with the number keys (ADR 0049).
    [Fact]
    public void NumberKeys_ArentTheBrushs()
    {
        Assert.False(_tool.TextEntered("1"));
        Assert.False(_tool.TextEntered("2"));
        Assert.False(_tool.TextEntered("b"));
        Assert.Equal(PaintColorTarget.Foreground, _colors.Target);
    }

    [Fact]
    public void AltClick_PicksTheTextColorShown_FromTheTopmostVisibleLayer()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _blue));
        var top = TestCells.AddLayer(_session.Document);
        top.SetCell(new CellPoint(1, 0), TestCells.Glyph('x', _blue, CellColor.Unset));

        Press(0, 0, ToolModifiers.Alt);
        Release(0, 0);
        Assert.Equal(_green, _colors.Foreground);

        Press(1, 0, ToolModifiers.Alt);
        Release(1, 0);
        Assert.Equal(_blue, _colors.Foreground);
        Assert.Equal(_blue, _colors.Background);
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void AltClick_WithTheBGBrush_PicksTheBackgroundShown()
    {
        Layer.SetCell(new CellPoint(4, 0), Cell.FromBackground(_green));
        UseTheBGBrush();

        Press(4, 0, ToolModifiers.Alt);
        Release(4, 0);

        Assert.Equal(_green, _colors.Background);
        Assert.Equal(_red, _colors.Foreground);
    }

    [Fact]
    public void AltClick_OnACellShowingNoColor_PicksNone()
    {
        Press(4, 0, ToolModifiers.Alt);
        Release(4, 0);
        Assert.True(_colors.Foreground.IsUnset);

        _colors.Foreground = _red;
        Press(0, 0, ToolModifiers.Alt);
        Release(0, 0);
        Assert.True(_colors.Foreground.IsUnset);
    }

    [Fact]
    public void AltClick_IgnoresHiddenLayers_AndWorksOnALockedOne()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, CellColor.Unset));
        var hidden = TestCells.AddLayer(_session.Document);
        hidden.SetCell(new CellPoint(0, 0), TestCells.Glyph('x', _blue, CellColor.Unset));
        History.Execute(new SetLayerPropertiesCommand(hidden.Id, hidden.Properties with { IsVisible = false }));
        History.Execute(new SetLayerPropertiesCommand(Layer.Id, Layer.Properties with { IsLocked = true }));

        Press(0, 0, ToolModifiers.Alt);
        Release(0, 0);

        Assert.Equal(_green, _colors.Foreground);
    }

    [Fact]
    public void AltDrag_KeepsPicking_AndPaintsNothing()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, CellColor.Unset));
        Layer.SetCell(new CellPoint(2, 0), TestCells.Glyph('c', _blue, CellColor.Unset));

        Press(0, 0, ToolModifiers.Alt);
        Assert.Equal(_green, _colors.Foreground);
        Move(2, 0);
        Release(2, 0);

        Assert.Equal(_blue, _colors.Foreground);
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void AltClick_OutsideTheCanvas_PicksNothing()
    {
        Press(9, 9, ToolModifiers.Alt);
        Release(9, 9);

        Assert.Equal(_red, _colors.Foreground);
    }

    [Fact]
    public void AltClick_InAMonochromeDocument_KeepsTheChosenColor()
    {
        History.Execute(new SetColorModeCommand(ColorMode.Monochrome));

        Press(0, 0, ToolModifiers.Alt);
        Release(0, 0);

        Assert.Equal(_red, _colors.Foreground);
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void Esc_WhilePicking_GivesTheColorBack()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, CellColor.Unset));

        Press(0, 0, ToolModifiers.Alt);
        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(_red, _colors.Foreground);
    }

    [Fact]
    public void ASizedTip_PaintsItsFootprint_CentredOnThePointer()
    {
        UseTheBGBrush();
        TestTools.SetTip(_tipValues, 3);

        Press(1, 1);
        Release(1, 1);

        Assert.Equal(_blue, CellAt(0, 0).Background);
        Assert.Equal(_blue, CellAt(2, 2).Background);
        Assert.True(CellAt(3, 1).Background.IsUnset);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void ADragWithASizedTip_PaintsEverythingItSweeps()
    {
        UseTheBGBrush();
        TestTools.SetTip(_tipValues, 2);

        Press(0, 0);
        Move(4, 0);
        Release(4, 0);

        Assert.Equal(_blue, CellAt(5, 1).Background);
        Assert.True(CellAt(0, 2).Background.IsUnset);
        Assert.Equal(1, History.UndoCount);
    }

    [Fact]
    public void TheBracketKeys_ChangeTheSize()
    {
        Assert.True(_tool.TextEntered("]"));
        Assert.True(_tool.TextEntered("]"));
        Assert.True(_tool.TextEntered("["));
        Assert.False(_tool.TextEntered("a"));

        Assert.Equal(2, _tipValues.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void AltClick_PicksFromThePointersCell_WhateverTheSize()
    {
        TestTools.SetTip(_tipValues, 3);
        Layer.SetCell(new CellPoint(1, 1), TestCells.Glyph('h', _green, CellColor.Unset));

        Press(1, 1, ToolModifiers.Alt);
        Release(1, 1);

        Assert.Equal(_green, _colors.Foreground);
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void TheOverlay_IsTheTipsFootprint_AndStopsFollowingItOnceDeactivated()
    {
        var changes = 0;
        _tool.OverlayChanged += (_, _) => changes++;

        _tipValues.Set(BrushTipOptions.Size, 4);
        Assert.Equal(1, changes);
        Assert.Equal(4, _tool.Overlay.Footprint?.Width);

        _tool.Deactivate();
        _tipValues.Set(BrushTipOptions.Size, 5);
        Assert.Equal(1, changes);
    }

    [Fact]
    public void AStroke_KeepsItsTip_WhenTheSizeChangesDuringIt()
    {
        UseTheBGBrush();
        Press(0, 0);
        _tool.TextEntered("]");

        Move(2, 0);
        Release(2, 0);

        Assert.Equal(_blue, CellAt(2, 0).Background);
        Assert.True(CellAt(2, 1).Background.IsUnset);
    }

    [Fact]
    public void BeingDeactivated_KeepsWhatWasPainted_AndEndsTheStroke()
    {
        Press(0, 0);
        _tool.Deactivate();
        Move(2, 0);

        Assert.Equal(_red, CellAt(0, 0).Foreground);
        Assert.True(CellAt(2, 0).Foreground.IsUnset);
        Assert.Equal(1, History.UndoCount);
    }

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private void UseTheBGBrush()
    {
        _tool.Deactivate();
        _tool = new ColorBrushTool(PaintColorTarget.Background, _session, _colors, TestTools.Tip(_tipValues));
    }

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, modifiers));

    private void Move(int x, int y) =>
        _tool.PointerMoved(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None));

    private void Release(int x, int y) =>
        _tool.PointerReleased(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None));
}
