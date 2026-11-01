// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Canvas;

/// <summary>
/// A 500 × 500 document for checking performance, opened from the Debug menu (it was the startup
/// document until step 1.7). It's busy on purpose, so zooming, panning and crispness are easy to
/// judge: rings of shading that show at any zoom, a border, and a block of text near the top-left
/// that includes glyphs the default font lacks. The text is document content, not UI, so it isn't in
/// Strings.resx.
/// </summary>
internal static class SampleDocument
{
    public static readonly CanvasSize Size = new(500, 500);

    // From light to dark; the rings cycle through it.
    private const string Shading = " .:-=+*#%@";

    private static readonly string[] _textBlock =
    [
        "AskiiArt sample canvas (temporary, until New and Open exist)",
        "",
        "The quick brown fox jumps over the lazy dog. 0123456789",
        "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG! ?@#$%&*()[]{}<>",
        "Accents: àáâãäå çèéêë ìíîï ñòóôõö ùúûü ÀÉÎÕÜ ß",
        "Lines: ┌──┬──┐ ╔══╦══╗ ░░▒▒▓▓██  Arrows: ← ↑ → ↓ ↔ ↕",
        "       │  │  │ ║  ║  ║ ▀▀▄▄▌▌▐▐",
        "       └──┴──┘ ╚══╩══╝",
        "Fallback font (orange): 漢字 かな ☺ ♪   No font at all (red): \U0010FFFD"
    ];

    public static Document Create(string layerName)
    {
        var document = Document.Create(Size, layerName);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var cells = new CellEditBuilder(document, layerId);

        DrawRings(cells);
        DrawBorder(cells);
        DrawTextBlock(cells, left: 4, top: 3);

        // Applied directly rather than through the history: there's nothing to undo back to.
        cells.Build()?.Apply(document);
        return document;
    }

    private static void DrawRings(CellEditBuilder cells)
    {
        // Cells are about twice as tall as they're wide, so rows count double to keep rings round.
        var centres = new[] { (X: 150.0, Y: 150.0), (X: 380.0, Y: 330.0), (X: 120.0, Y: 420.0) };

        for (var y = 0; y < Size.Height; y++)
        {
            for (var x = 0; x < Size.Width; x++)
            {
                var nearest = centres.Min(centre =>
                    Math.Sqrt(Math.Pow(x - centre.X, 2) + Math.Pow((y - centre.Y) * 2, 2)));
                var shade = (int)(nearest / 3) % (2 * Shading.Length - 2);
                var index = shade < Shading.Length ? shade : 2 * Shading.Length - 2 - shade;
                if (index > 0)
                    Set(cells, x, y, Shading[index]);
            }
        }
    }

    private static void DrawBorder(CellEditBuilder cells)
    {
        var right = Size.Width - 1;
        var bottom = Size.Height - 1;

        for (var x = 1; x < right; x++)
        {
            Set(cells, x, 0, '─');
            Set(cells, x, bottom, '─');
        }

        for (var y = 1; y < bottom; y++)
        {
            Set(cells, 0, y, '│');
            Set(cells, right, y, '│');
        }

        Set(cells, 0, 0, '┌');
        Set(cells, right, 0, '┐');
        Set(cells, 0, bottom, '└');
        Set(cells, right, bottom, '┘');
    }

    private static void DrawTextBlock(CellEditBuilder cells, int left, int top)
    {
        var width = _textBlock.Max(line => line.EnumerateRunes().Count()) + 4;
        var height = _textBlock.Length + 2;

        // Clear a box so the text stands out from the rings.
        for (var y = top - 1; y < top - 1 + height; y++)
        {
            for (var x = left - 2; x < left - 2 + width; x++)
                Set(cells, x, y, ' ');
        }

        for (var row = 0; row < _textBlock.Length; row++)
        {
            var column = 0;
            foreach (var rune in _textBlock[row].EnumerateRunes())
                cells.Set(new CellPoint(left + column++, top + row), Cell.FromGlyph(rune));
        }
    }

    private static void Set(CellEditBuilder cells, int x, int y, char glyph) =>
        cells.Set(new CellPoint(x, y), Cell.FromGlyph(new Rune(glyph)));
}
