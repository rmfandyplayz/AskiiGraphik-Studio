// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Tests.Canvas;

public sealed class ColorSampleDocumentTests
{
    [Fact]
    public void Create_IsABusyRgbDocumentWithEveryKindOfColouredCell()
    {
        var document = ColorSampleDocument.Create("Layer");

        var layer = Assert.Single(document.ActiveFrame.Layers);
        var cells = (from y in Enumerable.Range(0, document.CanvasSize.Height)
                     from x in Enumerable.Range(0, document.CanvasSize.Width)
                     select layer.GetCell(new CellPoint(x, y))).ToArray();

        Assert.Equal(ColorMode.Rgb, document.ColorMode);
        Assert.Equal(new CanvasSize(500, 500), document.CanvasSize);
        Assert.True(cells.Count(cell => cell.HasGlyph) > 100_000, "The sample should be busy.");
        Assert.Contains(cells, cell => cell.HasGlyph && !cell.Foreground.IsUnset && cell.Background.IsUnset);
        Assert.Contains(cells, cell => cell.HasGlyph && !cell.Foreground.IsUnset && !cell.Background.IsUnset);
        Assert.Contains(cells, cell => cell.HasGlyph && cell.Foreground.IsUnset && !cell.Background.IsUnset);
        Assert.Contains(cells, cell => !cell.HasGlyph && !cell.Background.IsUnset);
        Assert.Contains(cells, cell => cell.HasGlyph && cell.Foreground.IsUnset && cell.Background.IsUnset);
    }
}
