// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Canvas;

/// <summary>
/// A small document for checking ANSI export in a terminal (step 2b.4), opened from the Debug menu. It fits an
/// 80 × 24 terminal and shows the 16 ANSI colours as text and as backgrounds, the canvas's own ink and paper beside
/// colours, cells holding only a background, and box and block characters. The RGB one stores the ANSI colours as
/// the RGB the VGA palette shows them as, and adds two rows that go round the colour wheel. The text is document
/// content, not UI, so it isn't in Strings.resx.
/// </summary>
internal static class AnsiTestSampleDocument
{
    public static readonly CanvasSize Size = new(70, 19);

    private const int Left = 2;
    private const int Top = 1;
    private const int GradientWidth = 40;
    private const string Label = "                        ";

    public static Document Create(string layerName, ColorMode colorMode)
    {
        if (colorMode == ColorMode.Monochrome)
            throw new ArgumentOutOfRangeException(nameof(colorMode), colorMode, "The sample is in colour.");

        var document = Document.Create(Size, layerName, colorMode: colorMode);
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        var lines = CreateLines(colorMode);
        for (var row = 0; row < lines.Count; row++)
        {
            var column = 0;
            foreach (var run in lines[row])
            {
                foreach (var rune in run.Text.EnumerateRunes())
                    cells.Set(new CellPoint(Left + column++, Top + row), run.ToCell(rune));
            }
        }

        // Applied directly rather than through the history: there's nothing to undo back to.
        cells.Build()?.Apply(document);
        return document;
    }

    private static List<Run[]> CreateLines(ColorMode colorMode)
    {
        // An RGB document can't hold ANSI colours, so they're stored as the RGB the palette shows them as.
        CellColor Ansi(AnsiColor color)
        {
            if (colorMode == ColorMode.Ansi)
                return CellColor.FromAnsi(color);

            return CellColor.FromRgb(AnsiPalette.Vga[color]);
        }

        var allColors = Enum.GetValues<AnsiColor>().Order().ToArray();
        var modeName = colorMode == ColorMode.Ansi ? "ANSI" : "RGB";
        List<Run[]> lines =
        [
            [Plain($"AskiiArt {modeName} test sample (temporary)")],
            [],
            [Plain("Text:        "), .. AnsiNames(bright: false, Ansi)],
            [Plain("Bright text: "), .. AnsiNames(bright: true, Ansi)],
            [],
            [Plain("Backgrounds: "), .. allColors.Select(color => BackgroundOnly(3, Ansi(color)))],
            [Plain("Numbered:    "), .. allColors.Select(color => Numbered(color, Ansi(color)))],
            [],
            [Plain("Ink on a background:    "), new Run(" the terminal's own text colour ", CellColor.Unset,
                Ansi(AnsiColor.Blue))],
            [Plain("Colour on the paper:    "), new Run("bright cyan", Ansi(AnsiColor.BrightCyan), CellColor.Unset)],
            [Plain("Colour on a background: "), new Run(" bright yellow on red ", Ansi(AnsiColor.BrightYellow),
                Ansi(AnsiColor.Red))],
            [Plain("Only a background:      "), BackgroundOnly(8, Ansi(AnsiColor.Magenta))],
            [Plain("Box and blocks:         "), new Run("┌──┬──┐ ░░▒▒▓▓██", Ansi(AnsiColor.BrightYellow),
                Ansi(AnsiColor.Blue))],
            [Plain(Label), new Run("└──┴──┘ ▀▀▄▄▌▌▐▐", Ansi(AnsiColor.BrightYellow), Ansi(AnsiColor.Blue))]
        ];

        if (colorMode == ColorMode.Rgb)
        {
            lines.Add([]);
            lines.Add([Plain("24-bit text:            "), .. Gradient(position =>
                new Run("█", ColorSampleDocument.Hue(position, 1), CellColor.Unset))]);
            lines.Add([Plain("24-bit backgrounds:     "), .. Gradient(position =>
                BackgroundOnly(1, ColorSampleDocument.Hue(position, 0.6)))]);
        }

        return lines;
    }

    // Each name in its own colour. The bright ones keep the plain names, so the two rows line up.
    private static IEnumerable<Run> AnsiNames(bool bright, Func<AnsiColor, CellColor> ansi)
    {
        for (var number = 0; number < 8; number++)
        {
            var color = (AnsiColor)(bright ? number + 8 : number);
            yield return new Run(((AnsiColor)number).ToString(), ansi(color), CellColor.Unset);
            yield return Plain(" ");
        }
    }

    private static IEnumerable<Run> Gradient(Func<double, Run> cellAt) =>
        Enumerable.Range(0, GradientWidth).Select(index => cellAt(index / (double)GradientWidth));

    // The colour's number in the terminal's own text colour, on the colour.
    private static Run Numbered(AnsiColor color, CellColor background) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{(int)color,2} "), CellColor.Unset, background);

    private static Run Plain(string text) => new(text, CellColor.Unset, CellColor.Unset);

    private static Run BackgroundOnly(int width, CellColor background) =>
        new(new string(' ', width), CellColor.Unset, background, HasGlyphs: false);

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
