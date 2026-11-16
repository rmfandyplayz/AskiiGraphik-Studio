// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.App.Resources;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.App.Ramps;

/// <summary>
/// A document for checking glyph density (step 3.1), opened from the Debug menu. In the font of the document it
/// replaces, it lists characters from lightest to darkest as measured, with each one's darkness, shows each built-in
/// ramp as shipped and as this font would sort it, and paints each ramp from intensity 0 to 1. The text is document
/// content, not UI, so it isn't in Strings.resx, apart from the ramps' names.
/// </summary>
internal static class DensitySampleDocument
{
    /// <summary>How many cells wide each painted ramp is.</summary>
    public const int GradientWidth = 80;

    private const int LeftMargin = 2;
    private const int TopMargin = 1;
    private const int DarknessEntriesPerRow = 8;

    // Shades, halves and quadrants: they're fitted to the cell (ADR 0039), so they measure exact shares of it.
    private const string BlockCharacters = "░▒▓█▀▄▌▐▖▗▘▝▚▞▙▛▜▟";

    /// <summary>The header above the printable ASCII characters, sorted.</summary>
    public const string AsciiHeader = "Printable ASCII, lightest to darkest:";

    /// <summary>
    /// The start of the header above the painted ramps, which follow one per line in the order the app lists them.
    /// </summary>
    public const string GradientsHeader = "Each ramp painted from intensity 0 (left) to 1 (right)";

    public static Document Create(string layerName, FontReference font, GlyphDensityMeter meter)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(meter);

        var lines = CreateLines(meter);
        var width = lines.Max(line => line.EnumerateRunes().Count()) + 2 * LeftMargin;
        var size = new CanvasSize(width, lines.Count + 2 * TopMargin);
        var document = Document.Create(size, layerName, CharacterSet.Unicode, font);
        var cells = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        for (var row = 0; row < lines.Count; row++)
        {
            var column = 0;
            foreach (var rune in lines[row].EnumerateRunes())
            {
                // Spaces stay empty, so the sample has no solid cells but its text.
                if (rune.Value != ' ')
                    cells.Set(new CellPoint(LeftMargin + column, TopMargin + row), Cell.FromGlyph(rune));

                column++;
            }
        }

        // Applied directly rather than through the history: there's nothing to undo back to.
        cells.Build()?.Apply(document);
        return document;
    }

    /// <summary>The sample's text, one line per row.</summary>
    public static IReadOnlyList<string> CreateLines(GlyphDensityMeter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);

        var ascii = Enumerable.Range('!', '~' - '!' + 1).Select(codePoint => new Rune(codePoint)).ToList();
        var blocks = BlockCharacters.EnumerateRunes().ToList();
        var braille = BuiltInRamps.Braille.Ramp.Glyphs.ToList();
        var measured = ascii.Concat(blocks).Concat(braille).ToList();
        var lines = new List<string>
        {
            $"Density sample (temporary): {meter.Font.FamilyName}",
            "Each character is measured as the canvas draws it: the share of its cell it inks.",
            "",
            AsciiHeader,
            "  " + Sorted(ascii, meter),
            "Blocks, lightest to darkest:",
            "  " + Sorted(blocks, meter),
            "Braille, lightest to darkest:",
            "  " + Sorted(braille, meter),
            "",
            "Drawn from a fallback font, because this font doesn't have them:",
            "  " + FallbackCharacters(measured, meter),
            "",
            "Darkness of each, in percent of the cell:"
        };

        lines.AddRange(DarknessRows(measured, meter));
        lines.Add("");
        lines.Add("Built-in ramps, as shipped and as this font would sort them:");
        var nameWidth = BuiltInRamps.All.Max(ramp => RampName(ramp).Length) + 2;
        foreach (var ramp in BuiltInRamps.All)
        {
            lines.Add("  " + RampName(ramp).PadRight(nameWidth) + "shipped: " + ramp.Ramp);
            lines.Add("  " + new string(' ', nameWidth) + "sorted:  " + ramp.Ramp.SortedByDensity(meter));
        }

        lines.Add("");
        lines.Add($"{GradientsHeader}, {GradientWidth} cells wide:");
        foreach (var ramp in BuiltInRamps.All)
            lines.Add("  " + RampName(ramp).PadRight(nameWidth) + "|" + Gradient(ramp.Ramp) + "|");

        return lines;
    }

    private static string RampName(BuiltInRamp ramp) => UiText.Get(ramp.NameKey);

    private static string Sorted(IEnumerable<Rune> glyphs, GlyphDensityMeter meter) =>
        string.Concat(glyphs.OrderBy(meter.GetDensity).Select(glyph => glyph.ToString()));

    private static string FallbackCharacters(IEnumerable<Rune> glyphs, GlyphDensityMeter meter)
    {
        var fallbacks = string.Concat(glyphs.Where(glyph => meter.GetSource(glyph) != GlyphSource.PrimaryFont)
            .Select(glyph => glyph.ToString()));
        return fallbacks.Length > 0 ? fallbacks : "none";
    }

    private static IEnumerable<string> DarknessRows(IEnumerable<Rune> glyphs, GlyphDensityMeter meter)
    {
        var entries = glyphs.OrderBy(meter.GetDensity)
            .Select(glyph =>
                string.Create(CultureInfo.InvariantCulture, $"{glyph} {meter.GetDensity(glyph) * 100,5:F1}"))
            .ToList();

        for (var start = 0; start < entries.Count; start += DarknessEntriesPerRow)
            yield return "  " + string.Join("   ", entries.Skip(start).Take(DarknessEntriesPerRow));
    }

    private static string Gradient(Ramp ramp)
    {
        var text = new StringBuilder();
        for (var column = 0; column < GradientWidth; column++)
        {
            var glyph = ramp.GlyphAt(column / (double)(GradientWidth - 1));
            text.Append(glyph?.ToString() ?? " ");
        }

        return text.ToString();
    }
}
