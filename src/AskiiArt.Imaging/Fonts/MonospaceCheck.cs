// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// Checks whether a font's printable ASCII glyphs all have the same advance (ARCHITECTURE §11).
/// A font that fails is still allowed; the user gets a warning and the grid uses the reference
/// advance.
/// </summary>
public static class MonospaceCheck
{
    private const char FirstPrintableAscii = ' ';
    private const char LastPrintableAscii = '~';

    // Monospace fonts store identical advances, so any real difference is far bigger than this.
    private const float ToleranceInEms = 0.01f;

    // Measuring at the font's own design size keeps the numbers in font units.
    private const float MeasuringPixelSize = 1000;

    /// <summary>Measures every printable ASCII glyph the font has against <see cref="CellMetrics.ReferenceCharacter"/>.</summary>
    public static MonospaceCheckResult Run(LoadedFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        using var skFont = CellMetrics.CreateMeasuringFont(font, MeasuringPixelSize);
        var advances = new List<GlyphAdvance>();
        for (var character = FirstPrintableAscii; character <= LastPrintableAscii; character++)
        {
            if (font.Typeface.ContainsGlyph(character))
                advances.Add(new GlyphAdvance(new Rune(character), skFont.MeasureText(character.ToString())));
        }

        return Evaluate(advances, MeasuringPixelSize);
    }

    /// <summary>Compares measured advances; separate from <see cref="Run"/> so the rule can be tested with made-up numbers.</summary>
    internal static MonospaceCheckResult Evaluate(IReadOnlyList<GlyphAdvance> advances, float emSize)
    {
        if (advances.Count == 0)
            return new MonospaceCheckResult([]);

        var reference = new Rune(CellMetrics.ReferenceCharacter);
        var referenceAdvance = advances.FirstOrDefault(glyph => glyph.Rune == reference)?.Advance
                               ?? MostCommonAdvance(advances);
        var tolerance = emSize * ToleranceInEms;

        return new MonospaceCheckResult(
        [
            .. advances
                .Where(glyph => MathF.Abs(glyph.Advance - referenceAdvance) > tolerance)
                .Select(glyph => glyph.Rune)
        ]);
    }

    private static float MostCommonAdvance(IReadOnlyList<GlyphAdvance> advances) =>
        advances.GroupBy(glyph => glyph.Advance).MaxBy(group => group.Count())!.Key;

    internal sealed record GlyphAdvance(Rune Rune, float Advance);
}
