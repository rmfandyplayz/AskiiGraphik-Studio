// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class SetColorModeCommandTests
{
    private static CellColor Red => TestCells.Ansi(AnsiColor.Red);

    private static CellColor Blue => TestCells.Ansi(AnsiColor.Blue);

    private static CellColor Orange => TestCells.Rgb(255, 128, 0);

    [Fact]
    public void SwitchingAnsiToRgb_GivesEveryColorItsPaletteValue_InEveryLayer()
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Ansi);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        bottom.SetCell(0, TestCells.Glyph('a', Red, Blue));
        top.SetCell(1, Cell.FromBackground(Blue));

        new CommandHistory(document).Execute(new SetColorModeCommand(ColorMode.Rgb));

        Assert.Equal(ColorMode.Rgb, document.ColorMode);
        Assert.Equal(TestCells.Glyph('a', TestCells.Rgb(0xAA, 0, 0), TestCells.Rgb(0, 0, 0xAA)), bottom.GetCell(0));
        Assert.Equal(Cell.FromBackground(TestCells.Rgb(0, 0, 0xAA)), top.GetCell(1));
    }

    [Fact]
    public void SwitchingRgbToAnsi_PicksTheNearestOfThe16()
    {
        var document = TestCells.CreateDocument(1, 1, ColorMode.Rgb);
        var layer = document.ActiveFrame.Layers[0];
        layer.SetCell(0, TestCells.Glyph('a', Orange, TestCells.Rgb(10, 10, 10)));

        new CommandHistory(document).Execute(new SetColorModeCommand(ColorMode.Ansi));

        Assert.Equal(TestCells.Glyph('a', TestCells.Ansi(AnsiColor.BrightRed), TestCells.Ansi(AnsiColor.Black)),
            layer.GetCell(0));
    }

    [Fact]
    public void SwitchingToMonochrome_RemovesEveryColor_AndABackgroundOnItsOwnLeavesAnEmptyCell()
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Rgb);
        var layer = document.ActiveFrame.Layers[0];
        layer.SetCell(0, TestCells.Glyph('a', Orange, Orange));
        layer.SetCell(1, Cell.FromBackground(Orange));

        new CommandHistory(document).Execute(new SetColorModeCommand(ColorMode.Monochrome));

        Assert.Equal(ColorMode.Monochrome, document.ColorMode);
        Assert.Equal(TestCells.Glyph('a'), layer.GetCell(0));
        Assert.Equal(Cell.Empty, layer.GetCell(1));
    }

    [Fact]
    public void UndoAndRedo_RestoreTheModeAndEveryColor()
    {
        var document = TestCells.CreateDocument(3, 1, ColorMode.Rgb);
        var layer = document.ActiveFrame.Layers[0];
        layer.SetCell(0, TestCells.Glyph('a', Orange, CellColor.Unset));
        layer.SetCell(1, Cell.FromBackground(Orange));
        layer.SetCell(2, TestCells.Glyph('b'));
        var before = TestCommands.Snapshot(document);
        var history = new CommandHistory(document);
        history.Execute(new SetColorModeCommand(ColorMode.Monochrome));
        var after = TestCommands.Snapshot(document);

        history.Undo();
        Assert.Equal(before, TestCommands.Snapshot(document));

        history.Redo();
        Assert.Equal(after, TestCommands.Snapshot(document));
    }

    [Fact]
    public void CountCellsThatLookDifferent_CountsCellsWhoseColorsWouldBeRemovedOrMoved()
    {
        var document = TestCells.CreateDocument(3, 1, ColorMode.Rgb);
        var bottom = document.ActiveFrame.Layers[0];
        bottom.SetCell(0, TestCells.Glyph('a', Orange, CellColor.Unset));
        bottom.SetCell(1, Cell.FromBackground(TestCells.Rgb(0xAA, 0, 0)));
        bottom.SetCell(2, TestCells.Glyph('b'));
        TestCells.AddLayer(document).SetCell(2, Cell.FromBackground(Orange));

        Assert.Equal(2, SetColorModeCommand.CountCellsThatLookDifferent(document, ColorMode.Ansi));
        Assert.Equal(3, SetColorModeCommand.CountCellsThatLookDifferent(document, ColorMode.Monochrome));
        Assert.Equal(0, SetColorModeCommand.CountCellsThatLookDifferent(document, ColorMode.Rgb));
    }

    [Fact]
    public void CountCellsThatLookDifferent_IsZero_FromAnsiToRgb()
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Ansi);
        document.ActiveFrame.Layers[0].SetCell(0, TestCells.Glyph('a', Red, Blue));

        Assert.Equal(0, SetColorModeCommand.CountCellsThatLookDifferent(document, ColorMode.Rgb));
        Assert.Equal(1, SetColorModeCommand.CountCellsThatLookDifferent(document, ColorMode.Monochrome));
    }

    [Fact]
    public void SwitchingColorMode_RaisesOneChangeEventWithSettingsChanged()
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Ansi);
        document.ActiveFrame.Layers[0].SetCell(0, TestCells.Glyph('a', Red, CellColor.Unset));
        var events = new List<DocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        new CommandHistory(document).Execute(new SetColorModeCommand(ColorMode.Monochrome));

        var change = Assert.Single(events);
        Assert.True(change.SettingsChanged);
        Assert.Single(change.CellChanges);
    }

    [Fact]
    public void EstimatedMemory_GrowsWithTheCellsItChanges()
    {
        var changesNothing = new SetColorModeCommand(ColorMode.Monochrome);
        changesNothing.Apply(TestCells.CreateDocument(10, 10, ColorMode.Ansi));
        var colored = TestCells.CreateDocument(10, 10, ColorMode.Ansi);
        for (var index = 0; index < 100; index++)
            colored.ActiveFrame.Layers[0].SetCell(index, Cell.FromBackground(Red));

        var removesColors = new SetColorModeCommand(ColorMode.Monochrome);
        removesColors.Apply(colored);

        Assert.True(removesColors.EstimatedMemoryInBytes > changesNothing.EstimatedMemoryInBytes);
    }

    [Fact]
    public void Constructor_RejectsAnUnknownMode()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SetColorModeCommand((ColorMode)3));
    }
}
