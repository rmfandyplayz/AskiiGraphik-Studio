// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;

namespace AskiiArt.Core.Tests.Tools;

public sealed class BrushToolTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    private readonly EditingSession _session;
    private readonly PaintColors _colors = new();
    private readonly BrushTool _tool;

    public BrushToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3, ColorMode.Ansi));
        TestCells.Paint(Layer, "abc···", "ghi···", "mno···");
        _tool = new BrushTool(_session, _colors);
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
        _colors.Target = PaintColorTarget.Background;

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
        _colors.Target = PaintColorTarget.Background;
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
    public void AStroke_KeepsItsColor_WhenTheChoiceChangesDuringIt()
    {
        Press(0, 0);
        _colors.Foreground = _green;
        _tool.TextEntered(BrushTool.BackgroundBrushKey);
        Move(2, 0);
        Release(2, 0);

        Assert.Equal(_red, CellAt(2, 0).Foreground);
        Assert.False(CellAt(2, 0).HasBackground);
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
        _colors.Target = PaintColorTarget.Background;

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

    [Fact]
    public void Keys1And2_ChooseTheFGAndBGBrushes()
    {
        Assert.True(_tool.TextEntered(BrushTool.BackgroundBrushKey));
        Assert.Equal(PaintColorTarget.Background, _colors.Target);

        Assert.True(_tool.TextEntered(BrushTool.ForegroundBrushKey));
        Assert.Equal(PaintColorTarget.Foreground, _colors.Target);

        Assert.False(_tool.TextEntered("3"));
        Assert.False(_tool.TextEntered("b"));
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
        _colors.Target = PaintColorTarget.Background;

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

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, modifiers));

    private void Move(int x, int y) =>
        _tool.PointerMoved(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None));

    private void Release(int x, int y) =>
        _tool.PointerReleased(new ToolPointerInput(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None));
}
