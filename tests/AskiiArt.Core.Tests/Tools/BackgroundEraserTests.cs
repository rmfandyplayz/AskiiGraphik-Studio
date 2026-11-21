// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools;

// The BG Eraser shares everything else with the Eraser, which EraserToolTests covers.
public sealed class BackgroundEraserTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _green = TestCells.Ansi(AnsiColor.Green);

    private readonly EditingSession _session;
    private readonly ToolOptionValues _tip = new(EraserTool.OptionGroup);
    private readonly EraserTool _tool;

    public BackgroundEraserTests()
    {
        _session = new EditingSession(TestCells.CreateDocument(4, 1, ColorMode.Ansi));
        _tool = new EraserTool(EraserTarget.Background, _session, TestTools.Tip(_tip));
    }

    private Layer Layer => _session.Document.ActiveFrame.Layers[0];

    [Fact]
    public void ItIsTheBgEraser_WithTheErasersTip()
    {
        Assert.Equal(EraserTool.BackgroundId, _tool.Descriptor.Id);
        Assert.Equal("ToolBackgroundEraser", _tool.Descriptor.NameKey);
        Assert.Equal([EraserTool.OptionGroup], EraserTool.BackgroundDefinition.OptionGroups);
        Assert.Equal(EraserTool.BackgroundId,
            EraserTool.BackgroundDefinition.Create(TestTools.Context(_session)).Descriptor.Id);
    }

    [Fact]
    public void Dragging_TakesBackgroundsAway_AndLeavesGlyphsAndTheirColors()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, _red));
        Layer.SetCell(new CellPoint(1, 0), Cell.FromBackground(_red));
        Layer.SetCell(new CellPoint(2, 0), TestCells.Glyph('b', _green, CellColor.Unset));

        _tool.PointerPressed(PointerAt(0, 0));
        _tool.PointerMoved(PointerAt(3, 0));
        _tool.PointerReleased(PointerAt(3, 0));

        Assert.Equal(TestCells.Glyph('a', _green, CellColor.Unset), CellAt(0, 0));
        Assert.Equal(Cell.Empty, CellAt(1, 0));
        Assert.Equal(TestCells.Glyph('b', _green, CellColor.Unset), CellAt(2, 0));
        Assert.Equal(1, _session.History.UndoCount);
        Assert.Equal(UndoStepNames.Erase, _session.History.GetStepNameKey(0));
    }

    [Fact]
    public void CellsWithoutBackgrounds_LeaveNoStep()
    {
        Layer.SetCell(new CellPoint(0, 0), TestCells.Glyph('a', _green, CellColor.Unset));

        _tool.PointerPressed(PointerAt(0, 0));
        _tool.PointerReleased(PointerAt(0, 0));

        Assert.Equal(0, _session.History.UndoCount);
    }

    [Fact]
    public void AMonochromeDocument_GetsAWarning_ThatItHasNoBackgrounds()
    {
        var settings = new ToolSettings();

        Assert.Equal(EraserTool.NoBackgroundsWarningKey,
            EraserTool.BackgroundDefinition.WarningFor!(settings, TestCells.CreateDocument(2, 1)));
        Assert.Null(EraserTool.BackgroundDefinition.WarningFor!(settings, _session.Document));
        Assert.Null(EraserTool.Definition.WarningFor);
    }

    [Fact]
    public void WhileCellsAreSelected_ADrag_TakesAwayOnlyTheirBackgrounds()
    {
        for (var x = 0; x < 4; x++)
            Layer.SetCell(new CellPoint(x, 0), Cell.FromBackground(_red));

        TestTools.Select(_session, "·##·");

        _tool.PointerPressed(PointerAt(0, 0));
        _tool.PointerMoved(PointerAt(3, 0));
        _tool.PointerReleased(PointerAt(3, 0));

        Assert.Equal(Cell.FromBackground(_red), CellAt(0, 0));
        Assert.Equal(Cell.Empty, CellAt(1, 0));
        Assert.Equal(Cell.Empty, CellAt(2, 0));
        Assert.Equal(Cell.FromBackground(_red), CellAt(3, 0));
    }

    private Cell CellAt(int x, int y) => Layer.GetCell(new CellPoint(x, y));

    private static ToolPointerInput PointerAt(int x, int y) => new(new CellPoint(x, y), 0.5, 0.5, ToolModifiers.None);
}
