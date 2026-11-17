// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tests.Tools;

// The Standard ramp's levels are 1 '.', 2 ':', 3 '-', 4 '=', 5 '+', 6 '*', 7 '#', 8 '%' and 9 '@'.
public sealed class SoftEraserToolTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    private readonly EditingSession _session;
    private readonly ToolOptionValues _tip = new(EraserTool.OptionGroup);
    private readonly ToolOptionValues _ramp = new(RampOptions.Group);
    private readonly ToolOptionValues _options = new(SoftEraserOptions.Group);
    private readonly SoftEraserTool _tool;

    public SoftEraserToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3, ColorMode.Ansi));
        TestCells.Paint(Layer, "@@@@@@", "@@@@@@", "@@@@@@");
        _tool = new SoftEraserTool(_session, TestTools.Tip(_tip), _ramp, _options, () => TestTools.RampSteps);
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void ItsOptions_AreTheErasersTip_TheRampItSharesWithTheBrush_ThenItsOwn()
    {
        Assert.Equal([EraserTool.OptionGroup, RampOptions.Group, SoftEraserOptions.Group],
            SoftEraserTool.Definition.OptionGroups);
        Assert.Equal(100, _options.Get(SoftEraserOptions.Strength));
        Assert.Equal(50, _options.Get(SoftEraserOptions.Softness));
    }

    [Fact]
    public void AClickAtFullStrength_EmptiesTheCell()
    {
        Click(1, 1);

        Assert.Equal(["@@@@@@", "@·@@@@", "@@@@@@"], TestCells.Read(Layer));
    }

    [Theory]
    [InlineData(40, '+')]
    [InlineData(20, '#')]
    [InlineData(5, '@')]
    public void TheStrength_ChoosesHowFarTheMiddleLightens(int strength, char expected)
    {
        _options.Set(SoftEraserOptions.Strength, strength);

        Click(0, 0);

        Assert.Equal(expected, TestCells.Read(Layer)[0][0]);
    }

    [Fact]
    public void ASecondStroke_LightensFurther()
    {
        _options.Set(SoftEraserOptions.Strength, 40);

        Click(0, 0);
        Click(0, 0);

        // From '+', level 5, 40% of the way to nothing.
        Assert.Equal('-', TestCells.Read(Layer)[0][0]);
        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void GoingOverACellAgainInOneStroke_AddsNothing()
    {
        _options.Set(SoftEraserOptions.Strength, 40);

        Press(0, 0);
        Move(3, 0);
        Move(0, 0);
        Release(0, 0);

        Assert.Equal("++++@@", TestCells.Read(Layer)[0]);
    }

    [Fact]
    public void AHardTip_LightensEveryCellFully()
    {
        SetTip(3, softness: 0);

        Click(1, 1);

        Assert.Equal(["···@@@", "···@@@", "···@@@"], TestCells.Read(Layer));
    }

    // At full softness the edge ring of a 3-cell square gets about a quarter of the weight, which takes '@' down two
    // levels.
    [Fact]
    public void ASoftTip_LightensLessTowardsItsEdge()
    {
        SetTip(3, softness: 100);

        Click(1, 1);

        Assert.Equal(["###@@@", "#·#@@@", "###@@@"], TestCells.Read(Layer));
    }

    [Fact]
    public void ALightenedGlyph_KeepsItsOwnColors()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('@', _green, _red));
        _options.Set(SoftEraserOptions.Strength, 40);

        Click(0, 0);

        Assert.Equal(TestCells.Glyph('+', _green, _red), CellAt(0, 0));
    }

    [Fact]
    public void AnEmptiedGlyph_KeepsTheBackground()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('@', _green, _red));

        Click(0, 0);

        Assert.Equal(Cell.FromBackground(_red), CellAt(0, 0));
    }

    [Fact]
    public void AGlyphTheRampDoesntHold_StaysUntilItMovesAWholeLevel()
    {
        TestCells.Paint(Layer, "AA");
        _options.Set(SoftEraserOptions.Strength, 5);

        Click(0, 0);
        Assert.Equal('A', TestCells.Read(Layer)[0][0]);
        Assert.Equal(0, History.UndoCount);

        // From level 6, 40% of the way to nothing.
        _options.Set(SoftEraserOptions.Strength, 40);
        Click(1, 0);
        Assert.Equal('=', TestCells.Read(Layer)[0][1]);
    }

    [Fact]
    public void AnEmptyCell_IsLeftAlone_EvenWithABackground()
    {
        Layer.SetCell(new CellPoint(0, 0), Cell.FromBackground(_red));

        Click(0, 0);

        Assert.Equal(Cell.FromBackground(_red), CellAt(0, 0));
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void OneStroke_IsOneUndoStep_NamedErase()
    {
        Press(0, 0);
        Move(2, 0);
        Move(2, 2);
        Release(2, 2);

        Assert.Equal(1, History.UndoCount);
        Assert.Equal(UndoStepNames.Erase, History.GetStepNameKey(0));

        History.Undo();
        Assert.Equal(["@@@@@@", "@@@@@@", "@@@@@@"], TestCells.Read(Layer));
    }

    [Fact]
    public void Esc_TakesTheStrokeBack_AndLeavesNoStep()
    {
        Press(0, 0);
        Move(2, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(["@@@@@@", "@@@@@@", "@@@@@@"], TestCells.Read(Layer));
        Assert.Equal(0, History.StepCount);
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
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

        Click(0, 0);

        Assert.Equal('@', TestCells.Read(Layer)[0][0]);
        Assert.Equal(stepsBefore, History.UndoCount);
    }

    [Fact]
    public void ARampTheCharacterSetCantHold_ErasesNothing_AndWarns()
    {
        var document = Document.Create(new CanvasSize(6, 3), "Layer", CharacterSet.Ascii);
        TestCells.Paint(document.ActiveFrame.Layers[0], "@");
        var session = new EditingSession(document);
        var context = TestTools.Context(session, densities: TestTools.RampSteps);
        var tool = SoftEraserTool.Definition.Create(context);
        Assert.Null(SoftEraserTool.Definition.WarningFor!(context.Settings, document));

        context.Settings.For(RampOptions.Group).Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);
        tool.PointerPressed(PointerAt(0, 0));
        tool.PointerReleased(PointerAt(0, 0));

        Assert.Equal('@', TestCells.Read(document.ActiveFrame.Layers[0])[0][0]);
        Assert.Equal(0, session.History.UndoCount);
        Assert.Equal(RampOptions.RampDoesNotFitWarningKey,
            SoftEraserTool.Definition.WarningFor!(context.Settings, document));
    }

    [Fact]
    public void AStroke_KeepsItsSettings_WhenTheyChangeDuringIt()
    {
        Press(0, 0);
        _options.Set(SoftEraserOptions.Strength, 0);
        Move(2, 0);
        Release(2, 0);

        Assert.Equal("···@@@", TestCells.Read(Layer)[0]);
    }

    [Fact]
    public void TheBracketKeys_ChangeTheErasersTipSize_AndNumberKeysArentTheSoftErasers()
    {
        Assert.True(_tool.TextEntered("]"));
        Assert.False(_tool.TextEntered("2"));

        Assert.Equal(2, _tip.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void TheOverlay_IsTheTipsFootprint_AndChangesWithIt()
    {
        var changes = 0;
        _tool.OverlayChanged += (_, _) => changes++;

        _tool.TextEntered("]");

        Assert.Equal(1, changes);
        Assert.Equal(2, _tool.Overlay.Footprint?.Width);
        Assert.Null(_tool.Overlay.Caret);
    }

    [Fact]
    public void BeingDeactivated_KeepsWhatWasErased_AndStopsFollowingTheTip()
    {
        Press(0, 0);

        _tool.Deactivate();
        var changes = 0;
        _tool.OverlayChanged += (_, _) => changes++;
        Move(3, 0);
        _tip.Set(BrushTipOptions.Size, 5);

        Assert.Equal("·@@@@@", TestCells.Read(Layer)[0]);
        Assert.Equal(0, changes);
    }

    [Fact]
    public void TheSoftEraser_TakesNoText_AndLetsSpacePan()
    {
        Assert.False(_tool.TextEntered("a"));
        Assert.False(_tool.IsTakingText);
        Assert.True(_tool.AllowsSpacePanning);
        Assert.False(_tool.PansWithPrimaryButton);
    }

    private void SetTip(int size, int softness)
    {
        TestTools.SetTip(_tip, size);
        _options.Set(SoftEraserOptions.Softness, softness);
    }

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private void Click(int x, int y)
    {
        Press(x, y);
        Release(x, y);
    }

    private static ToolPointerInput PointerAt(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);

    private void Press(int x, int y) => _tool.PointerPressed(PointerAt(x, y));

    private void Move(int x, int y) => _tool.PointerMoved(PointerAt(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(PointerAt(x, y));
}
