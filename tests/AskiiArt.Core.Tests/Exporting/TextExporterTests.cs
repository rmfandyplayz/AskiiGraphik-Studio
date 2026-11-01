// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Exporting;

namespace AskiiArt.Core.Tests.Exporting;

public sealed class TextExporterTests
{
    private readonly TextExporter _exporter = new(CompositingPolicy.Default);

    [Fact]
    public void Export_FlattensTheVisibleLayers_FullWidth_WithALineBreakAfterEveryRow()
    {
        var bytes = _exporter.Export(CreateLayeredDocument());

        Assert.Equal("aX  \n  é \n", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void ToText_JoinsTheRowsWithTheGivenLineBreak_AndEndsWithoutOne()
    {
        var text = _exporter.ToText(CreateLayeredDocument(), "\r\n");

        Assert.Equal("aX  \r\n  é ", text);
    }

    [Fact]
    public void Export_AnEmptyCanvas_IsSpaces()
    {
        var text = Encoding.UTF8.GetString(_exporter.Export(TestCells.CreateDocument(3, 2)));

        Assert.Equal("   \n   \n", text);
    }

    [Fact]
    public void Export_IsUtf8WithoutAByteOrderMark_AndKeepsCharactersOutsideTheBasicPlane()
    {
        var document = TestCells.CreateDocument(1, 1);
        document.ActiveFrame.Layers[0].SetCell(new CellPoint(0, 0), Cell.FromGlyph(new Rune(0x1F600)));

        Assert.Equal([0xF0, 0x9F, 0x98, 0x80, (byte)'\n'], _exporter.Export(document));
    }

    [Theory]
    [InlineData(SpaceHandling.HidesLayersBelow, " ")]
    [InlineData(SpaceHandling.TreatedAsEmpty, "a")]
    public void ToText_FlattensWithTheGivenPolicy(SpaceHandling spaceHandling, string expected)
    {
        var document = TestCells.CreateDocument(1, 1);
        TestCells.Paint(document.ActiveFrame.Layers[0], "a");
        TestCells.Paint(TestCells.AddLayer(document), " ");

        var text = new TextExporter(new CompositingPolicy(spaceHandling)).ToText(document, "\n");

        Assert.Equal(expected, text);
    }

    [Fact]
    public void Describes_ItsFiles()
    {
        Assert.Equal("askiiart.export.text", _exporter.Id);
        Assert.Equal(".txt", _exporter.FileExtension);
    }

    // Bottom to top: art, art on top of it, and a hidden layer that mustn't show.
    private static Document CreateLayeredDocument()
    {
        var document = TestCells.CreateDocument(4, 2);
        TestCells.Paint(document.ActiveFrame.Layers[0], "ab··", "····");
        TestCells.Paint(TestCells.AddLayer(document), "·X··", "··é·");
        var hidden = TestCells.AddLayer(document);
        TestCells.Paint(hidden, "ZZZZ", "ZZZZ");
        hidden.SetProperties(hidden.Properties with { IsVisible = false });
        return document;
    }
}
