// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class ColorModeRulesTests
{
    private static CellColor Red => TestCells.Ansi(AnsiColor.Red);

    private static CellColor Orange => TestCells.Rgb(255, 128, 0);

    [Fact]
    public void Allows_LetsEachModeHoldOnlyItsOwnKindOfColor()
    {
        Assert.True(ColorMode.Monochrome.Allows(CellColor.Unset));
        Assert.False(ColorMode.Monochrome.Allows(Red));
        Assert.False(ColorMode.Monochrome.Allows(Orange));

        Assert.True(ColorMode.Ansi.Allows(CellColor.Unset));
        Assert.True(ColorMode.Ansi.Allows(Red));
        Assert.False(ColorMode.Ansi.Allows(Orange));

        Assert.True(ColorMode.Rgb.Allows(CellColor.Unset));
        Assert.False(ColorMode.Rgb.Allows(Red));
        Assert.True(ColorMode.Rgb.Allows(Orange));
    }

    [Fact]
    public void Allows_ACell_ChecksBothItsColors()
    {
        Assert.True(ColorMode.Ansi.Allows(TestCells.Glyph('a', Red, Red)));
        Assert.False(ColorMode.Ansi.Allows(TestCells.Glyph('a', Red, Orange)));
        Assert.False(ColorMode.Ansi.Allows(Cell.FromBackground(Orange)));
        Assert.True(ColorMode.Monochrome.Allows(TestCells.Glyph('a')));
    }

    [Fact]
    public void Convert_ToMonochrome_RemovesEveryColor()
    {
        Assert.Equal(CellColor.Unset, ColorMode.Monochrome.Convert(Red));
        Assert.Equal(CellColor.Unset, ColorMode.Monochrome.Convert(Orange));
        Assert.Equal(TestCells.Glyph('a'), ColorMode.Monochrome.Convert(TestCells.Glyph('a', Red, Orange)));
        Assert.Equal(Cell.Empty, ColorMode.Monochrome.Convert(Cell.FromBackground(Red)));
    }

    [Fact]
    public void Convert_RgbToAnsi_PicksTheNearestOfThe16()
    {
        Assert.Equal(TestCells.Ansi(AnsiColor.BrightRed), ColorMode.Ansi.Convert(Orange));
    }

    [Fact]
    public void Convert_AnsiToRgb_UsesThePalettesValue()
    {
        Assert.Equal(TestCells.Rgb(0xAA, 0, 0), ColorMode.Rgb.Convert(Red));
    }

    [Fact]
    public void Convert_LeavesAColorTheModeAllowsAlone()
    {
        Assert.Equal(Red, ColorMode.Ansi.Convert(Red));
        Assert.Equal(Orange, ColorMode.Rgb.Convert(Orange));
        Assert.Equal(CellColor.Unset, ColorMode.Rgb.Convert(CellColor.Unset));
    }

    [Fact]
    public void ConversionChangesTheLookOf_IsTrue_WhenAColorIsRemovedOrMoved()
    {
        Assert.True(ColorMode.Monochrome.ConversionChangesTheLookOf(Red));
        Assert.True(ColorMode.Ansi.ConversionChangesTheLookOf(Orange));

        Assert.False(ColorMode.Monochrome.ConversionChangesTheLookOf(CellColor.Unset));
        Assert.False(ColorMode.Ansi.ConversionChangesTheLookOf(Red));
        Assert.False(ColorMode.Rgb.ConversionChangesTheLookOf(Red));
        Assert.False(ColorMode.Ansi.ConversionChangesTheLookOf(TestCells.Rgb(0xAA, 0, 0)));
    }
}
