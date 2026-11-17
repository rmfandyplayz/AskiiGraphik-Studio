// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tests.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools;

// The Standard ramp's levels are 1 '.', 2 ':', 3 '-', 4 '=', 5 '+', 6 '*', 7 '#', 8 '%' and 9 '@'.
public sealed class BrushToolTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    // Each ramp glyph a step darker than the last, and 'A' about as dark as '*', level 6.
    private static readonly FakeGlyphDensities _densities = new(new Dictionary<char, double>
    {
        ['.'] = 0.1,
        [':'] = 0.2,
        ['-'] = 0.3,
        ['='] = 0.4,
        ['+'] = 0.5,
        ['*'] = 0.6,
        ['#'] = 0.7,
        ['%'] = 0.8,
        ['@'] = 0.9,
        ['A'] = 0.62,
        ['W'] = 0.95
    });

    private readonly EditingSession _session;
    private readonly PaintColors _colors = new();
    private readonly ToolOptionValues _tip = new(BrushTool.TipOptions);
    private readonly ToolOptionValues _options = new(RampBrushOptions.Group);
    private readonly BrushTool _tool;

    public BrushToolTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(6, 3, ColorMode.Ansi));
        _tool = CreateTool();
        _colors.Foreground = _red;
        _colors.Background = _blue;
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    private CommandHistory History => _session.History;

    [Fact]
    public void ItsOptions_AreTheGroupsTip_ThenItsOwn()
    {
        Assert.Equal([BrushTool.TipOptions, RampBrushOptions.Group], BrushTool.Definition.OptionGroups);
        Assert.Equal(BuiltInRamps.Standard.Id, _options.Get(RampBrushOptions.Ramp));
        Assert.Equal(100, _options.Get(RampBrushOptions.Intensity));
        Assert.Equal(50, _options.Get(RampBrushOptions.Softness));
        Assert.Equal(RampBrushOptions.BlendModeBlend, _options.Get(RampBrushOptions.BlendMode));
    }

    [Fact]
    public void OnlyBlend_CanBeChosen_AsTheBlendMode()
    {
        Assert.Throws<ArgumentException>(() => _options.Set(RampBrushOptions.BlendMode, "replace"));
        Assert.Throws<ArgumentException>(() => _options.Set(RampBrushOptions.BlendMode, "darken"));
        Assert.Equal(3, RampBrushOptions.BlendMode.Choices.Count);
    }

    [Fact]
    public void TheRampChoices_ShowEachRampsCharacters()
    {
        var standard = RampBrushOptions.Ramp.Choices[0];

        Assert.Equal(
            BuiltInRamps.All.Select(ramp => ramp.Id),
            RampBrushOptions.Ramp.Choices.Select(choice => choice.Value));
        Assert.Equal(".:-=+*#%@", standard.Detail);
    }

    [Fact]
    public void AClickAtFullIntensity_PaintsTheDarkestGlyph_InTheCurrentColors()
    {
        Click(1, 1);

        Assert.Equal(TestCells.Glyph('@', _red, _blue), CellAt(1, 1));
        Assert.Equal(["······", "·@····", "······"], TestCells.Read(Layer));
    }

    [Theory]
    [InlineData(50, '+')]
    [InlineData(20, ':')]
    [InlineData(5, '·')]
    public void TheIntensity_ChoosesTheLevel(int intensity, char expected)
    {
        _options.Set(RampBrushOptions.Intensity, intensity);

        Click(0, 0);

        Assert.Equal(expected.ToString(), TestCells.Read(Layer)[0][..1]);
    }

    [Fact]
    public void AHardTip_PaintsEveryCellFully()
    {
        SetTip(3, softness: 0);

        Click(1, 1);

        Assert.Equal(["@@@···", "@@@···", "@@@···"], TestCells.Read(Layer));
    }

    // At full softness the edge ring of a 3-cell square gets about a quarter of the weight, which takes an empty cell
    // to level 2.
    [Fact]
    public void ASoftTip_PaintsLighterTowardsItsEdge()
    {
        SetTip(3, softness: 100);

        Click(1, 1);

        Assert.Equal([":::···", ":@:···", ":::···"], TestCells.Read(Layer));
    }

    [Fact]
    public void GoingOverACellAgainInOneStroke_AddsNothing()
    {
        SetTip(3, softness: 100);

        Press(1, 1);
        Move(4, 1);
        Move(1, 1);
        Release(1, 1);

        Assert.Equal(':', TestCells.Read(Layer)[0][0]);
    }

    [Fact]
    public void ASecondStroke_PushesFurther()
    {
        SetTip(3, softness: 100);

        Click(1, 1);
        Click(1, 1);

        Assert.Equal('=', TestCells.Read(Layer)[0][0]);
        Assert.Equal(2, History.UndoCount);
    }

    [Fact]
    public void ALowIntensity_LightensDarkerCells()
    {
        TestCells.Paint(Layer, "@");
        _options.Set(RampBrushOptions.Intensity, 50);

        Click(0, 0);

        Assert.Equal('+', TestCells.Read(Layer)[0][0]);
    }

    [Fact]
    public void LighteningToNothing_TakesTheGlyphAway_AndKeepsTheBackground()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('#', _green, _green));
        _options.Set(RampBrushOptions.Intensity, 0);

        Click(0, 0);

        Assert.Equal(Cell.FromBackground(_green), CellAt(0, 0));
    }

    [Fact]
    public void AGlyphTheRampDoesntHold_CountsAsTheLevelThatMeasuresNearest()
    {
        SetTip(3, softness: 100);
        TestCells.Paint(Layer, "A");

        Click(1, 1);

        // From level 6, a quarter of the way to 9.
        Assert.Equal('#', TestCells.Read(Layer)[0][0]);
    }

    [Fact]
    public void AGlyphTheBrushDoesntMoveAWholeLevel_IsLeftAlone_ColorsAndAll()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('W', _green, _green));

        Click(0, 0);

        Assert.Equal(TestCells.Glyph('W', _green, _green), CellAt(0, 0));
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void ABackgroundOfNone_KeepsTheCellsBackground_AsTypingDoes()
    {
        Layer.SetCell(new CellPoint(0, 0), Cell.FromBackground(_green));
        _colors.Background = CellColor.Unset;

        Click(0, 0);

        Assert.Equal(TestCells.Glyph('@', _red, _green), CellAt(0, 0));
    }

    [Fact]
    public void ARampTheCharacterSetCantHold_PaintsNothing_AndWarns()
    {
        var document = Document.Create(new CanvasSize(6, 3), "Layer", CharacterSet.Ascii, colorMode: ColorMode.Ansi);
        var settings = new ToolSettings();
        var session = new EditingSession(document);
        var tool = (BrushTool)BrushTool.Definition.Create(new ToolContext(session, _colors, settings, () => 1,
            () => _densities));
        Assert.Null(BrushTool.Definition.WarningFor!(settings, document));

        settings.For(RampBrushOptions.Group).Set(RampBrushOptions.Ramp, BuiltInRamps.Blocks.Id);
        tool.PointerPressed(PointerAt(0, 0));
        tool.PointerReleased(PointerAt(0, 0));

        Assert.True(document.ActiveFrame.Layers[0].GetCell(new CellPoint(0, 0)).IsEmpty);
        Assert.Equal(0, session.History.UndoCount);
        Assert.Equal(RampBrushOptions.RampDoesNotFitWarningKey, BrushTool.Definition.WarningFor!(settings, document));
    }

    [Fact]
    public void AMonochromeDocument_GetsGlyphsWithoutColors()
    {
        var session = new EditingSession(TestCells.CreateDocument(2, 1));
        var tool = new BrushTool(session, _colors, TestTools.Tip(_tip), _options, () => _densities);

        tool.PointerPressed(PointerAt(0, 0));
        tool.PointerReleased(PointerAt(0, 0));

        Assert.Equal(TestCells.Glyph('@'), session.Document.ActiveFrame.Layers[0].GetCell(new CellPoint(0, 0)));
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

        Assert.True(CellAt(0, 0).IsEmpty);
        Assert.Equal(stepsBefore, History.UndoCount);
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
        Assert.Equal(["······", "······", "······"], TestCells.Read(Layer));
    }

    [Fact]
    public void Esc_TakesTheStrokeBack_AndLeavesNoStep()
    {
        Press(0, 0);
        Move(2, 0);

        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.True(CellAt(0, 0).IsEmpty);
        Assert.Equal(0, History.StepCount);
        Assert.False(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));
    }

    [Fact]
    public void AStroke_KeepsItsSettings_WhenTheyChangeDuringIt()
    {
        Press(0, 0);
        _options.Set(RampBrushOptions.Intensity, 0);
        _colors.Foreground = _green;
        Move(2, 0);
        Release(2, 0);

        Assert.Equal(TestCells.Glyph('@', _red, _blue), CellAt(2, 0));
    }

    [Fact]
    public void AltClick_PicksBothColorsShown()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _red));

        Press(0, 0, ToolModifiers.Alt);
        Release(0, 0);

        Assert.Equal(_green, _colors.Foreground);
        Assert.Equal(_red, _colors.Background);
        Assert.True(CellAt(0, 0).HasGlyph);
        Assert.Equal(0, History.UndoCount);
    }

    [Fact]
    public void Esc_WhilePicking_GivesBothColorsBack()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _green));

        Press(0, 0, ToolModifiers.Alt);
        Assert.True(_tool.KeyPressed(ToolKey.Escape, ToolModifiers.None));

        Assert.Equal(_red, _colors.Foreground);
        Assert.Equal(_blue, _colors.Background);
    }

    [Fact]
    public void TheBracketKeys_ChangeTheSharedTipsSize_AndNumberKeysArentTheBrushs()
    {
        Assert.True(_tool.TextEntered("]"));
        Assert.False(_tool.TextEntered("2"));

        Assert.Equal(2, _tip.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void TheOverlay_IsTheTipsFootprint()
    {
        SetTip(3, softness: 50);

        Assert.Equal(3, _tool.Overlay.Footprint!.Width);
    }

    private BrushTool CreateTool() =>
        new(_session, _colors, TestTools.Tip(_tip), _options, () => _densities);

    private void SetTip(int size, int softness)
    {
        TestTools.SetTip(_tip, size);
        _options.Set(RampBrushOptions.Softness, softness);
    }

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private void Click(int x, int y)
    {
        Press(x, y);
        Release(x, y);
    }

    private static ToolPointerInput PointerAt(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        new(new CellPoint(x, y), 0.5, 0.5, modifiers);

    private void Press(int x, int y, ToolModifiers modifiers = ToolModifiers.None) =>
        _tool.PointerPressed(PointerAt(x, y, modifiers));

    private void Move(int x, int y) => _tool.PointerMoved(PointerAt(x, y));

    private void Release(int x, int y) => _tool.PointerReleased(PointerAt(x, y));
}
