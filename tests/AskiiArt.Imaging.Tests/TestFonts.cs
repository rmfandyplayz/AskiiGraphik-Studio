// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Tests;

/// <summary>Shared fonts and runes for Imaging tests.</summary>
internal static class TestFonts
{
    /// <summary>A private-use code point no real font draws.</summary>
    public static Rune NoFontHasThis { get; } = new(0x10FFFD);

    /// <summary>The family names of the bundled fonts, for theories.</summary>
    public static TheoryData<string> BundledFamilyNames => [.. BundledFonts.All.Select(font => font.FamilyName)];

    public static BundledFont Bundled(string familyName) =>
        BundledFonts.All.Single(font => font.FamilyName == familyName);

    public static LoadedFont Load(string familyName) => FontLoader.LoadBundled(Bundled(familyName));

    /// <summary>
    /// Finds a glyph <paramref name="source"/> has and <paramref name="primary"/> lacks, searching
    /// the ranges ASCII art uses most beyond ASCII itself.
    /// </summary>
    public static Rune FindRuneOnlyIn(LoadedFont source, LoadedFont primary)
    {
        (int First, int Last)[] ranges =
        [
            (0x2500, 0x259F), // box drawing and blocks
            (0x25A0, 0x25FF), // geometric shapes
            (0x2190, 0x21FF), // arrows
            (0x2200, 0x22FF), // maths operators
            (0x2800, 0x28FF), // braille
            (0x0370, 0x04FF) // Greek and Cyrillic
        ];

        foreach (var (first, last) in ranges)
        {
            for (var codePoint = first; codePoint <= last; codePoint++)
            {
                var rune = new Rune(codePoint);
                if (source.HasGlyph(rune) && !primary.HasGlyph(rune))
                    return rune;
            }
        }

        throw new InvalidOperationException(
            $"No glyph found that {source.FamilyName} has and {primary.FamilyName} lacks.");
    }
}

/// <summary>A fallback provider that only ever offers one font (or none).</summary>
internal sealed class FixedFallbackFontProvider(LoadedFont? font) : IFallbackFontProvider
{
    public static FixedFallbackFontProvider None { get; } = new(null);

    public LoadedFont? FindFontFor(Rune rune, LoadedFont primary) =>
        font is not null && font.HasGlyph(rune) ? font : null;
}
