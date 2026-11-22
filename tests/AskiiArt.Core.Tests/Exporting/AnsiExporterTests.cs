// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Tests.Selection;

namespace AskiiArt.Core.Tests.Exporting;

public sealed class AnsiExporterTests
{
    // The escape character, which starts every code, written as "ESC" so the expected text can be read.
    private const string Escape = "\u001b";

    private readonly AnsiExporter _exporter = new();

    [Fact]
    public void IsTheAnsFormat()
    {
        Assert.Equal("askiiart.export.ansi", _exporter.Id);
        Assert.Equal(".ans", _exporter.FileExtension);
    }

    [Fact]
    public void Export_AMonochromeDocument_IsThePlainText()
    {
        var document = TestCells.CreateDocument(3, 2);
        TestCells.Paint(document.ActiveFrame.Layers[0], "ab·", "··c");

        Assert.Equal(new TextExporter().Export(document), _exporter.Export(document));
    }

    [Fact]
    public void Export_IsUtf8WithoutAByteOrderMark_WithALineBreakAfterEveryRow()
    {
        var document = TestCells.CreateDocument(2, 2, ColorMode.Ansi);
        document.ActiveFrame.Layers[0].SetCell(new CellPoint(0, 0), Cell.FromGlyph(new Rune('┌')));

        Assert.Equal("┌ \n  \n"u8.ToArray(), _exporter.Export(document));
    }

    [Theory]
    [InlineData(AnsiColor.Black, 30)]
    [InlineData(AnsiColor.Red, 31)]
    [InlineData(AnsiColor.White, 37)]
    [InlineData(AnsiColor.BrightBlack, 90)]
    [InlineData(AnsiColor.BrightCyan, 96)]
    [InlineData(AnsiColor.BrightWhite, 97)]
    public void ToText_AnAnsiColour_UsesItsCode_AndTenMoreAsABackground(AnsiColor color, int code)
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Ansi);
        var layer = document.ActiveFrame.Layers[0];
        layer.SetCell(0, TestCells.Glyph('a', TestCells.Ansi(color), CellColor.Unset));
        layer.SetCell(1, Cell.FromBackground(TestCells.Ansi(color)));

        // The cell without a glyph leaves the text colour as it was.
        Assert.Equal($"ESC[{code}maESC[{code + 10}m ESC[0m", Readable(_exporter.ToText(document, "\n")));
    }

    [Fact]
    public void ToText_AnRgbColour_UsesA24BitCode()
    {
        var document = TestCells.CreateDocument(1, 1, ColorMode.Rgb);
        var cell = TestCells.Glyph('a', TestCells.Rgb(255, 128, 0), TestCells.Rgb(0, 10, 200));
        document.ActiveFrame.Layers[0].SetCell(0, cell);

        Assert.Equal("ESC[38;2;255;128;0;48;2;0;10;200maESC[0m", Readable(_exporter.ToText(document, "\n")));
    }

    [Fact]
    public void ToText_WritesACodeOnlyWhereAColourChanges()
    {
        var document = TestCells.CreateDocument(5, 1, ColorMode.Ansi);
        var layer = document.ActiveFrame.Layers[0];
        var red = TestCells.Ansi(AnsiColor.Red);
        var blue = TestCells.Ansi(AnsiColor.Blue);
        layer.SetCell(0, TestCells.Glyph('a', red, CellColor.Unset));
        layer.SetCell(1, TestCells.Glyph('b', red, CellColor.Unset));
        layer.SetCell(2, TestCells.Glyph('c', red, blue));
        layer.SetCell(3, TestCells.Glyph('d', CellColor.Unset, blue));
        layer.SetCell(4, TestCells.Glyph('e'));

        Assert.Equal("ESC[31mabESC[44mcESC[39mdESC[49me", Readable(_exporter.ToText(document, "\n")));
    }

    [Fact]
    public void ToText_ACellWithoutAGlyph_LeavesTheTextColourAsItWas()
    {
        var document = TestCells.CreateDocument(4, 1, ColorMode.Ansi);
        var layer = document.ActiveFrame.Layers[0];
        var green = TestCells.Ansi(AnsiColor.Green);
        layer.SetCell(0, TestCells.Glyph('a', green, CellColor.Unset));
        layer.SetCell(2, Cell.FromBackground(TestCells.Ansi(AnsiColor.Yellow)));
        layer.SetCell(3, TestCells.Glyph('b', green, CellColor.Unset));

        Assert.Equal("ESC[32ma ESC[43m ESC[49mbESC[0m", Readable(_exporter.ToText(document, "\n")));
    }

    [Fact]
    public void ToText_ResetsTheColoursAtTheEndOfEachRowThatSetOne_AndEndsWithoutALineBreak()
    {
        var document = TestCells.CreateDocument(2, 3, ColorMode.Ansi);
        var layer = document.ActiveFrame.Layers[0];
        layer.SetCell(1, Cell.FromBackground(TestCells.Ansi(AnsiColor.Blue)));
        layer.SetCell(new CellPoint(0, 1), TestCells.Glyph('x'));
        layer.SetCell(new CellPoint(0, 2), TestCells.Glyph('y', TestCells.Ansi(AnsiColor.Red), CellColor.Unset));

        Assert.Equal(" ESC[44m ESC[0m\r\nx \r\nESC[31my ESC[0m", Readable(_exporter.ToText(document, "\r\n")));
    }

    [Fact]
    public void ToText_FlattensTheVisibleLayers()
    {
        var document = TestCells.CreateDocument(2, 1, ColorMode.Ansi);
        TestCells.Paint(document.ActiveFrame.Layers[0], "ab");
        var top = TestCells.AddLayer(document);
        top.SetCell(0, Cell.FromBackground(TestCells.Ansi(AnsiColor.Blue)));
        var hidden = TestCells.AddLayer(document, "Hidden");
        hidden.SetCell(1, TestCells.Glyph('z', TestCells.Ansi(AnsiColor.Red), CellColor.Unset));
        hidden.SetProperties(hidden.Properties with { IsVisible = false });

        Assert.Equal("ESC[44maESC[49mb", Readable(_exporter.ToText(document, "\n")));
    }

    [Fact]
    public void Export_KeepsCharactersOutsideTheBasicPlane()
    {
        var document = TestCells.CreateDocument(1, 1, ColorMode.Ansi);
        document.ActiveFrame.Layers[0].SetCell(new CellPoint(0, 0), Cell.FromGlyph(new Rune(0x1F600)));

        Assert.Equal([0xF0, 0x9F, 0x98, 0x80, (byte)'\n'], _exporter.Export(document));
    }

    [Fact]
    public void ToText_WithASelection_IsItsBounds_WithUnselectedCellsAsPlainSpaces()
    {
        var document = TestCells.CreateDocument(3, 2, ColorMode.Ansi);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "abc", "def");
        layer.SetCell(new CellPoint(2, 0), Cell.FromBackground(TestCells.Ansi(AnsiColor.Red)));

        var text = _exporter.ToText(document, "\n", TestMasks.Parse("·#·", "··#"));

        Assert.Equal("b \n f", Readable(text));
    }

    private static string Readable(string text) => text.Replace(Escape, "ESC", StringComparison.Ordinal);
}
