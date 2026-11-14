// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class AnsiPaletteTests
{
    [Theory]
    [InlineData(AnsiColor.Black, "#000000")]
    [InlineData(AnsiColor.Red, "#aa0000")]
    [InlineData(AnsiColor.Yellow, "#aa5500")]
    [InlineData(AnsiColor.White, "#aaaaaa")]
    [InlineData(AnsiColor.BrightBlack, "#555555")]
    [InlineData(AnsiColor.BrightBlue, "#5555ff")]
    [InlineData(AnsiColor.BrightWhite, "#ffffff")]
    public void Vga_HasTheStandardValues(AnsiColor color, string expected)
    {
        Assert.Equal(expected, AnsiPalette.Vga[color].ToString());
    }

    [Fact]
    public void FindNearest_OfAPaletteColor_IsThatColor()
    {
        foreach (var color in Enum.GetValues<AnsiColor>())
            Assert.Equal(color, AnsiPalette.Vga.FindNearest(AnsiPalette.Vga[color]));
    }

    [Theory]
    [InlineData(255, 0, 0, AnsiColor.Red)]
    [InlineData(255, 128, 0, AnsiColor.BrightRed)]
    [InlineData(10, 10, 10, AnsiColor.Black)]
    [InlineData(200, 200, 200, AnsiColor.White)]
    [InlineData(255, 255, 0, AnsiColor.BrightYellow)]
    public void FindNearest_PicksTheColorThatLooksClosest(int red, int green, int blue, AnsiColor expected)
    {
        Assert.Equal(expected, AnsiPalette.Vga.FindNearest(new RgbColor((byte)red, (byte)green, (byte)blue)));
    }

    [Theory]
    [InlineData(0, 0, 85)]
    [InlineData(0, 85, 0)]
    public void FindNearest_OnATie_PicksTheLowerNumberedColor(int red, int green, int blue)
    {
        // Each is exactly halfway between black and blue or green.
        Assert.Equal(AnsiColor.Black, AnsiPalette.Vga.FindNearest(new RgbColor((byte)red, (byte)green, (byte)blue)));
    }

    [Fact]
    public void TheIndexer_RejectsAnUnknownColor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiPalette.Vga[(AnsiColor)16]);
    }
}
