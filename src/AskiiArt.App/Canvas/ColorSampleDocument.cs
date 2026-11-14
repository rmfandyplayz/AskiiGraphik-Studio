// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Canvas;

/// <summary>
/// A 500 × 500 RGB document for checking how the canvas draws colour, opened from the Debug menu.
/// It has the same rings of shading as <see cref="SampleDocument"/>, so it's as busy, and nearly
/// every glyph has a colour of its own: on the paper in the left half, and on backgrounds in the
/// right half, where the gaps between rings hold only a background. A block near the top-left, in
/// the canvas's own ink and paper, shows the 16 ANSI colours and the other cases worth checking.
/// The text is document content, not UI, so it isn't in Strings.resx.
/// </summary>
internal static class ColorSampleDocument
{
    public static readonly CanvasSize Size = SampleDocument.Size;

    // Where the right half, with backgrounds, starts.
    private const int BackgroundsFromColumn = 250;

    private static readonly CellColor _orange = Rgb(230, 140, 40);
    private static readonly CellColor _skyBlue = Rgb(40, 150, 230);
    private static readonly CellColor _yellow = Rgb(250, 230, 90);
    private static readonly CellColor _navy = Rgb(20, 30, 110);
    private static readonly CellColor _white = Rgb(255, 255, 255);
    private static readonly CellColor _forest = Rgb(20, 90, 40);

    private static readonly Run[][] _textBlock =
    [
        [Plain("AskiiArt colour sample (temporary). Set colours look the same in light and dark mode;")],
        [Plain("only the unset ones follow it, drawn in the canvas's own ink and paper like these words.")],
        [],
        [Plain("ANSI colours as text:    "), .. AnsiNames(bright: false)],
        [Plain("The bright ones:         "), .. AnsiNames(bright: true)],
        [],
        [Plain("ANSI colours as backgrounds:")],
        [Plain("  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15")],
        [Plain(" "), .. AnsiSwatches()],
        [],
        [Plain("Ink on a background:      "), new Run(" the canvas's ink ", CellColor.Unset, _orange)],
        [Plain("Coloured text on paper:   "), new Run("sky blue", _skyBlue, CellColor.Unset)],
        [Plain("Only a background:        "), BackgroundOnly(8, _skyBlue)],
        [Plain("No seams between cells:   "), new Run("┌──┬──┐ ░░▒▒▓▓██", _yellow, _navy)],
        [Plain("                          "), new Run("└──┴──┘ ▀▀▄▄▌▌▐▐", _yellow, _navy)],
        [Plain("Missing glyphs (flagged): "), new Run("漢字 \U0010FFFD", _white, _forest)]
    ];

    public static Document Create(string layerName)
    {
        var document = Document.Create(Size, layerName, colorMode: ColorMode.Rgb);
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);

        DrawRings(cells);
        DrawTextBlock(cells, left: 4, top: 3);

        // Applied directly rather than through the history: there's nothing to undo back to.
        cells.Build()?.Apply(document);
        return document;
    }

    private static void DrawRings(CellEditBuilder cells)
    {
        for (var y = 0; y < Size.Height; y++)
        {
            for (var x = 0; x < Size.Width; x++)
            {
                var glyph = SampleDocument.ShadingAt(x, y);

                // Text goes round the colour wheel from the top-left corner to the bottom-right one.
                var wheelPosition = (x + y) / (double)(Size.Width + Size.Height);
                if (x < BackgroundsFromColumn)
                {
                    if (glyph != ' ')
                        Set(cells, x, y, Cell.FromGlyph(new Rune(glyph), Hue(wheelPosition, 0.8), CellColor.Unset));

                    continue;
                }

                // Dark backgrounds go round the wheel from top to bottom, under brighter text.
                var background = Hue(y / (double)Size.Height, 0.3);
                if (glyph == ' ')
                    Set(cells, x, y, Cell.FromBackground(background));
                else
                    Set(cells, x, y, Cell.FromGlyph(new Rune(glyph), Hue(wheelPosition, 1), background));
            }
        }
    }

    private static void DrawTextBlock(CellEditBuilder cells, int left, int top)
    {
        var width = _textBlock.Max(line => line.Sum(run => run.Text.EnumerateRunes().Count())) + 4;
        var height = _textBlock.Length + 2;

        // Clear a box, in the canvas's own ink and paper, so the block stands out from the rings.
        for (var y = top - 1; y < top - 1 + height; y++)
        {
            for (var x = left - 2; x < left - 2 + width; x++)
                Set(cells, x, y, Cell.FromGlyph(new Rune(' ')));
        }

        for (var row = 0; row < _textBlock.Length; row++)
        {
            var column = 0;
            foreach (var run in _textBlock[row])
            {
                foreach (var rune in run.Text.EnumerateRunes())
                    Set(cells, left + column++, top + row, run.ToCell(rune));
            }
        }
    }

    // Each name in its own colour. The bright ones keep the plain names, so the two rows line up.
    private static IEnumerable<Run> AnsiNames(bool bright)
    {
        for (var number = 0; number < 8; number++)
        {
            var color = (AnsiColor)(bright ? number + 8 : number);
            yield return new Run(((AnsiColor)number).ToString(), Ansi(color), CellColor.Unset);
            yield return Plain(" ");
        }
    }

    private static IEnumerable<Run> AnsiSwatches()
    {
        foreach (var color in Enum.GetValues<AnsiColor>().Order())
            yield return BackgroundOnly(3, Ansi(color));
    }

    // A colour round the colour wheel (0 to 1, from red), at a brightness from 0 to 1.
    private static CellColor Hue(double wheelPosition, double brightness)
    {
        const double saturation = 0.75;
        var sector = wheelPosition % 1 * 6;
        var fraction = sector - Math.Floor(sector);
        var high = brightness;
        var low = brightness * (1 - saturation);
        var rising = low + (high - low) * fraction;
        var falling = high - (high - low) * fraction;
        var (red, green, blue) = (int)sector switch
        {
            0 => (high, rising, low),
            1 => (falling, high, low),
            2 => (low, high, rising),
            3 => (low, falling, high),
            4 => (rising, low, high),
            _ => (high, low, falling)
        };

        return CellColor.FromRgb(new RgbColor(ToByte(red), ToByte(green), ToByte(blue)));
    }

    private static byte ToByte(double value) => (byte)Math.Round(value * byte.MaxValue);

    // An RGB document can't hold ANSI colours, so they're stored as the RGB the palette shows them as.
    private static CellColor Ansi(AnsiColor color) => CellColor.FromRgb(AnsiPalette.Vga[color]);

    private static CellColor Rgb(byte red, byte green, byte blue) => CellColor.FromRgb(new RgbColor(red, green, blue));

    private static Run Plain(string text) => new(text, CellColor.Unset, CellColor.Unset);

    private static Run BackgroundOnly(int width, CellColor background) =>
        new(new string(' ', width), CellColor.Unset, background, HasGlyphs: false);

    private static void Set(CellEditBuilder cells, int x, int y, Cell cell) => cells.Set(new CellPoint(x, y), cell);

    // Text written in the same colours, or as many cells holding only the background.
    private readonly record struct Run(string Text, CellColor Foreground, CellColor Background, bool HasGlyphs = true)
    {
        public Cell ToCell(Rune glyph)
        {
            if (HasGlyphs)
                return Cell.FromGlyph(glyph, Foreground, Background);

            return Cell.FromBackground(Background);
        }
    }
}
