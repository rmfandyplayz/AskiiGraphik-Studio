// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CellColorTests
{
    [Fact]
    public void Default_IsUnset()
    {
        var color = default(CellColor);

        Assert.True(color.IsUnset);
        Assert.Equal(CellColor.Unset, color);
        Assert.Equal(CellColorKind.Unset, color.Kind);
        Assert.False(color.TryGetAnsi(out _));
        Assert.False(color.TryGetRgb(out _));
        Assert.Equal("unset", color.ToString());
    }

    [Theory]
    [InlineData(AnsiColor.Black)]
    [InlineData(AnsiColor.Yellow)]
    [InlineData(AnsiColor.BrightWhite)]
    public void FromAnsi_GivesTheSameColorBack(AnsiColor ansi)
    {
        var color = CellColor.FromAnsi(ansi);

        Assert.False(color.IsUnset);
        Assert.Equal(CellColorKind.Ansi, color.Kind);
        Assert.True(color.TryGetAnsi(out var back));
        Assert.Equal(ansi, back);
        Assert.False(color.TryGetRgb(out _));
        Assert.Equal(ansi.ToString(), color.ToString());
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(18, 171, 86)]
    public void FromRgb_GivesTheSameColorBack(int red, int green, int blue)
    {
        var rgb = new RgbColor((byte)red, (byte)green, (byte)blue);

        var color = CellColor.FromRgb(rgb);

        Assert.False(color.IsUnset);
        Assert.Equal(CellColorKind.Rgb, color.Kind);
        Assert.True(color.TryGetRgb(out var back));
        Assert.Equal(rgb, back);
        Assert.False(color.TryGetAnsi(out _));
    }

    [Fact]
    public void AnAnsiColorAndAnRgbColor_AreNeverEqual_EvenWhenTheyLookTheSame()
    {
        Assert.NotEqual(CellColor.FromAnsi(AnsiColor.Red), CellColor.FromRgb(AnsiPalette.Vga[AnsiColor.Red]));
        Assert.NotEqual(CellColor.FromAnsi(AnsiColor.Black), CellColor.FromRgb(new RgbColor(0, 0, 0)));
    }

    [Fact]
    public void RgbColor_PrintsAsLowercaseHex()
    {
        Assert.Equal("#12ab05", new RgbColor(0x12, 0xAB, 0x05).ToString());
        Assert.Equal("#12ab05", CellColor.FromRgb(new RgbColor(0x12, 0xAB, 0x05)).ToString());
    }

    [Fact]
    public void FromAnsi_RejectsAnUnknownColor()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CellColor.FromAnsi((AnsiColor)16));
    }
}
