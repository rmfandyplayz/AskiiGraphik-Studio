// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tests.Selection;

namespace AskiiArt.Core.Tests.Clipboard;

public sealed class CellClipTests
{
    private readonly Document _document = TestCells.CreateDocument(5, 3, ColorMode.Ansi);

    public CellClipTests() => TestCells.Paint(Layer, "abcde", "fghij", "klmno");

    private Layer Layer => _document.ActiveFrame.Layers[0];

    [Fact]
    public void Copy_TakesTheSelectedCells_InTheSelectionsBounds_WithItsShape_AndWhereItWas()
    {
        var clip = CellClip.Copy(Layer, TestMasks.Parse("·····", "·##··", "··##·"));

        Assert.Equal(new CanvasSize(3, 2), clip.Size);
        Assert.Equal(new CellPoint(1, 1), clip.Origin);
        Assert.Equal(["##·", "·##"], TestMasks.Describe(clip.Shape));
        Assert.Equal(["gh·", "·mn"], Describe(clip));
    }

    [Fact]
    public void Copy_KeepsColours()
    {
        var red = TestCells.Glyph('x', TestCells.Ansi(AnsiColor.Red), TestCells.Ansi(AnsiColor.Blue));
        Layer.SetCell(new CellPoint(0, 0), red);

        var clip = CellClip.Copy(Layer, TestMasks.Parse("#····", "·····", "·····"));

        Assert.Equal(red, clip.GetCell(new CellPoint(0, 0)));
    }

    [Fact]
    public void Copy_WithNothingSelected_Throws()
    {
        Assert.Throws<ArgumentException>(() => CellClip.Copy(Layer, TestMasks.Parse("·····", "·····", "·····")));
    }

    [Fact]
    public void ToText_IsTheRectanglesRows_WithEmptyAndUnselectedCellsAsSpaces()
    {
        Layer.SetCell(new CellPoint(2, 1), Cell.Empty);
        var clip = CellClip.Copy(Layer, TestMasks.Parse("·····", "·##··", "··##·"));

        Assert.Equal("g  \r\n mn", clip.ToText("\r\n"));
    }

    [Fact]
    public void FittedTo_Ascii_TakesAwayGlyphsItCantHold_ButKeepsTheirBackgrounds()
    {
        var blue = TestCells.Ansi(AnsiColor.Blue);
        Layer.SetCell(new CellPoint(0, 0), Cell.FromGlyph(new Rune('é'), CellColor.Unset, blue));
        Layer.SetCell(new CellPoint(1, 0), Cell.FromGlyph(new Rune('ü')));
        var clip = CellClip.Copy(Layer, TestMasks.Parse("###··", "·····", "·····"));

        var fitted = clip.FittedTo(CharacterSet.Ascii, ColorMode.Ansi);

        Assert.Equal(Cell.FromBackground(blue), fitted.GetCell(new CellPoint(0, 0)));
        Assert.True(fitted.GetCell(new CellPoint(1, 0)).IsEmpty);
        Assert.Equal(TestCells.Glyph('c'), fitted.GetCell(new CellPoint(2, 0)));
    }

    [Fact]
    public void FittedTo_AnotherColourMode_ConvertsColoursAsSwitchingTheModeDoes()
    {
        var red = TestCells.Glyph('x', TestCells.Ansi(AnsiColor.Red), CellColor.Unset);
        Layer.SetCell(new CellPoint(0, 0), red);
        var clip = CellClip.Copy(Layer, TestMasks.Parse("#····", "·····", "·····"));

        Assert.Equal(ColorMode.Rgb.Convert(red), clip.FittedTo(CharacterSet.Unicode, ColorMode.Rgb).GetCell(default));
        Assert.Equal(TestCells.Glyph('x'), clip.FittedTo(CharacterSet.Unicode, ColorMode.Monochrome).GetCell(default));
    }

    [Fact]
    public void FittedTo_WhatAlreadyFits_IsTheSameClip()
    {
        var clip = CellClip.Copy(Layer, TestMasks.Parse("##···", "·····", "·····"));

        Assert.Same(clip, clip.FittedTo(CharacterSet.Ascii, ColorMode.Ansi));
    }

    private static string[] Describe(CellClip clip)
    {
        var cells = new Cell[clip.Size.CellCount];
        for (var y = 0; y < clip.Size.Height; y++)
        {
            for (var x = 0; x < clip.Size.Width; x++)
                cells[(y * clip.Size.Width) + x] = clip.GetCell(new CellPoint(x, y));
        }

        return TestCells.Describe(cells, clip.Size.Width);
    }
}
