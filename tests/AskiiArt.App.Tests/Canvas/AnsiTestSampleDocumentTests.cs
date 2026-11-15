// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Tests.Canvas;

public sealed class AnsiTestSampleDocumentTests
{
    [Fact]
    public void Create_TheAnsiOne_FitsATerminal_AndUsesEveryAnsiColourAsTextAndAsABackground()
    {
        var document = AnsiTestSampleDocument.Create("Layer", ColorMode.Ansi);
        var cells = CellsOf(document);

        Assert.Equal(ColorMode.Ansi, document.ColorMode);
        Assert.True(document.CanvasSize.Width <= 80 && document.CanvasSize.Height <= 24, "It should fit 80 × 24.");
        foreach (var color in Enum.GetValues<AnsiColor>())
        {
            Assert.Contains(cells, cell => cell.HasGlyph && cell.Foreground == CellColor.FromAnsi(color));
            Assert.Contains(cells, cell => cell.Background == CellColor.FromAnsi(color));
        }

        Assert.Contains(cells, cell => cell.HasGlyph && cell.Foreground.IsUnset && !cell.Background.IsUnset);
        Assert.Contains(cells, cell => !cell.HasGlyph && !cell.Background.IsUnset);
        Assert.Contains(cells, cell => cell.HasGlyph && cell.Glyph.Value == '┌');
    }

    [Fact]
    public void Create_TheRgbOne_HasTheAnsiColoursAsTheVgaPaletteShowsThem_AndMore()
    {
        var document = AnsiTestSampleDocument.Create("Layer", ColorMode.Rgb);
        var colors = CellsOf(document).SelectMany(cell => new[] { cell.Foreground, cell.Background })
            .Where(color => !color.IsUnset).ToHashSet();

        Assert.Equal(ColorMode.Rgb, document.ColorMode);
        Assert.All(Enum.GetValues<AnsiColor>(),
            color => Assert.Contains(CellColor.FromRgb(AnsiPalette.Vga[color]), colors));
        Assert.True(colors.Count > 50, "The gradient rows should add many more colours.");
    }

    [Fact]
    public void Create_InMonochrome_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AnsiTestSampleDocument.Create("Layer", ColorMode.Monochrome));

    private static Cell[] CellsOf(Document document)
    {
        var layer = Assert.Single(document.ActiveFrame.Layers);
        return (from y in Enumerable.Range(0, document.CanvasSize.Height)
                from x in Enumerable.Range(0, document.CanvasSize.Width)
                select layer.GetCell(new CellPoint(x, y))).ToArray();
    }
}
