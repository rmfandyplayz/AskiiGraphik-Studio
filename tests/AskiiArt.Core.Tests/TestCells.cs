// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests;

/// <summary>Short ways to build cells and layer content in tests.</summary>
internal static class TestCells
{
    /// <summary>Marks an empty cell in the strings passed to <see cref="Paint"/>.</summary>
    public const char EmptyMarker = '·';

    public static Cell Glyph(char character) => Cell.FromGlyph(new Rune(character));

    public static Cell Glyph(char character, CellColor foreground, CellColor background) =>
        Cell.FromGlyph(new Rune(character), foreground, background);

    public static CellColor Ansi(AnsiColor color) => CellColor.FromAnsi(color);

    public static CellColor Rgb(byte red, byte green, byte blue) => CellColor.FromRgb(new RgbColor(red, green, blue));

    public static Document CreateDocument(int width, int height, ColorMode colorMode = ColorMode.Monochrome) =>
        Document.Create(new CanvasSize(width, height), "Layer", colorMode: colorMode);

    /// <summary>
    /// Writes <paramref name="rows"/> into the layer starting at its top left. Each character is a
    /// glyph without colours, except <see cref="EmptyMarker"/>, which leaves the cell empty.
    /// </summary>
    public static void Paint(Layer layer, params string[] rows)
    {
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                var character = rows[y][x];
                layer.SetCell(new CellPoint(x, y), character == EmptyMarker ? Cell.Empty : Glyph(character));
            }
        }
    }

    /// <summary>
    /// Reads the whole layer's glyphs back in the format <see cref="Paint"/> takes. Colours aren't
    /// shown, and a cell holding only a background reads as empty.
    /// </summary>
    public static string[] Read(Layer layer)
    {
        var rows = new string[layer.Size.Height];
        for (var y = 0; y < rows.Length; y++)
        {
            var row = new StringBuilder();
            for (var x = 0; x < layer.Size.Width; x++)
                row.Append(Describe(layer.GetCell(new CellPoint(x, y))));

            rows[y] = row.ToString();
        }

        return rows;
    }

    /// <summary>Renders cells in the format <see cref="Paint"/> takes, <paramref name="width"/> cells per row.</summary>
    public static string[] Describe(ReadOnlySpan<Cell> cells, int width)
    {
        var rows = new string[cells.Length / width];
        for (var y = 0; y < rows.Length; y++)
        {
            var row = new StringBuilder();
            foreach (var cell in cells.Slice(y * width, width))
                row.Append(Describe(cell));

            rows[y] = row.ToString();
        }

        return rows;
    }

    private static string Describe(Cell cell) => cell.HasGlyph ? cell.Glyph.ToString() : EmptyMarker.ToString();

    public static Layer AddLayer(Document document, string name = "Extra")
    {
        var layer = new Layer(Guid.NewGuid(), new LayerProperties(name), document.CanvasSize);
        document.ActiveFrame.InsertLayer(document.ActiveFrame.Layers.Count, layer);
        return layer;
    }
}
