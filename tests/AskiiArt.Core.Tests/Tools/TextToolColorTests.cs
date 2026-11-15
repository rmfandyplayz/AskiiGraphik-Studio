// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Text;

namespace AskiiArt.Core.Tests.Tools;

/// <summary>The text tool typing in the current FG and BG colours (ADR 0042).</summary>
public sealed class TextToolColorTests
{
    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _blue = TestCells.Ansi(AnsiColor.Blue);
    private static readonly CellColor _orange = TestCells.Rgb(255, 136, 0);

    private readonly PaintColors _colors = new();

    [Fact]
    public void Typing_WritesTheFgColorAsText_OverTheBgColor()
    {
        var (layer, tool) = Create(ColorMode.Ansi);
        _colors.Foreground = _red;
        _colors.Background = _blue;

        Type(tool, "a");

        var cell = layer.GetCell(new CellPoint(0, 0));
        Assert.Equal(new System.Text.Rune('a'), cell.Glyph);
        Assert.Equal(_red, cell.Foreground);
        Assert.Equal(_blue, cell.Background);
    }

    [Fact]
    public void WithTheBgColorNone_TypingKeepsTheCellsOwnBackground()
    {
        var (layer, tool) = Create(ColorMode.Ansi);
        layer.SetCell(new CellPoint(0, 0), Cell.FromBackground(_blue));
        layer.SetCell(new CellPoint(1, 0), Cell.FromGlyph(new System.Text.Rune('x'), _red, _red));
        _colors.Foreground = _red;

        Type(tool, "ab");

        Assert.Equal(_blue, layer.GetCell(new CellPoint(0, 0)).Background);
        Assert.Equal(_red, layer.GetCell(new CellPoint(1, 0)).Background);
        Assert.Equal(new System.Text.Rune('b'), layer.GetCell(new CellPoint(1, 0)).Glyph);
    }

    [Fact]
    public void Typing_ConvertsTheColorsToTheDocumentsColorMode()
    {
        var (ansiLayer, ansiTool) = Create(ColorMode.Ansi);
        var (rgbLayer, rgbTool) = Create(ColorMode.Rgb);
        _colors.Foreground = _orange;
        _colors.Background = _blue;

        Type(ansiTool, "a");
        Type(rgbTool, "a");

        Assert.Equal(ColorMode.Ansi.Convert(_orange), ansiLayer.GetCell(new CellPoint(0, 0)).Foreground);
        Assert.Equal(_orange, rgbLayer.GetCell(new CellPoint(0, 0)).Foreground);
        Assert.Equal(CellColor.FromRgb(AnsiPalette.Vga[AnsiColor.Blue]),
            rgbLayer.GetCell(new CellPoint(0, 0)).Background);
    }

    [Fact]
    public void InAMonochromeDocument_TypingHasNoColors()
    {
        var (layer, tool) = Create(ColorMode.Monochrome);
        _colors.Foreground = _red;
        _colors.Background = _blue;

        Type(tool, "a");

        var cell = layer.GetCell(new CellPoint(0, 0));
        Assert.True(cell.Foreground.IsUnset);
        Assert.True(cell.Background.IsUnset);
    }

    [Fact]
    public void ColorsChosenAfterTheToolWasMade_AreTheOnesTyped()
    {
        var (layer, tool) = Create(ColorMode.Ansi);

        _colors.Foreground = _red;
        Type(tool, "a");

        Assert.Equal(_red, layer.GetCell(new CellPoint(0, 0)).Foreground);
    }

    [Fact]
    public void BackspaceAndDelete_EraseTheGlyph_ButKeepTheBackground()
    {
        var (layer, tool) = Create(ColorMode.Ansi);
        _colors.Background = _blue;
        Type(tool, "ab");

        Assert.True(tool.KeyPressed(ToolKey.Backspace, ToolModifiers.None));
        Assert.True(tool.KeyPressed(ToolKey.Left, ToolModifiers.None));
        Assert.True(tool.KeyPressed(ToolKey.Delete, ToolModifiers.None));

        Assert.Equal(Cell.FromBackground(_blue), layer.GetCell(new CellPoint(0, 0)));
        Assert.Equal(Cell.FromBackground(_blue), layer.GetCell(new CellPoint(1, 0)));
    }

    private (Layer Layer, TextTool Tool) Create(ColorMode colorMode)
    {
        var session = new EditingSession(TestCells.CreateDocument(4, 1, colorMode));
        var tool = new TextTool(session, _colors);
        tool.PointerPressed(new ToolPointerInput(new CellPoint(0, 0), 0.5, 0.5, ToolModifiers.None));
        return (session.Document.ActiveFrame.Layers[0], tool);
    }

    private static void Type(TextTool tool, string text) => Assert.True(tool.TextEntered(text));
}
