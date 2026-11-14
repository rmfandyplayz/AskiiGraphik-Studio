// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas.Rendering;
using AskiiArt.Core.Documents;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas.Rendering;

public sealed class CellColorRenderingTests
{
    private static readonly SKColor _ink = new(12, 34, 56);

    [Fact]
    public void TextColor_Unset_IsTheInk()
    {
        Assert.Equal(_ink, CellColorRendering.TextColor(CellColor.Unset, _ink));
    }

    [Fact]
    public void BackgroundColor_Unset_IsTransparent()
    {
        Assert.Equal(SKColors.Transparent, CellColorRendering.BackgroundColor(CellColor.Unset));
    }

    [Fact]
    public void RgbColor_IsDrawnAsItIs()
    {
        var color = CellColor.FromRgb(new RgbColor(1, 128, 255));

        Assert.Equal(new SKColor(1, 128, 255), CellColorRendering.TextColor(color, _ink));
        Assert.Equal(new SKColor(1, 128, 255), CellColorRendering.BackgroundColor(color));
    }

    [Fact]
    public void AnsiColor_IsDrawnAsTheVgaPaletteShowsIt()
    {
        Assert.All(Enum.GetValues<AnsiColor>(), ansi =>
        {
            var vga = AnsiPalette.Vga[ansi];
            var expected = new SKColor(vga.Red, vga.Green, vga.Blue);

            Assert.Equal(expected, CellColorRendering.TextColor(CellColor.FromAnsi(ansi), _ink));
            Assert.Equal(expected, CellColorRendering.BackgroundColor(CellColor.FromAnsi(ansi)));
        });
    }
}
