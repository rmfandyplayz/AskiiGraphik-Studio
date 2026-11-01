// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;

namespace AskiiArt.Core.Documents;

/// <summary>
/// Which characters a document may hold, chosen per document (ARCHITECTURE §6, ADR 0016). A
/// document never holds a glyph its character set doesn't allow.
/// </summary>
public enum CharacterSet
{
    /// <summary>Any character that fills a cell on its own.</summary>
    Unicode = 0,

    /// <summary>Printable ASCII only: the space and U+0021 to U+007E.</summary>
    Ascii = 1
}

/// <summary>The rules behind each <see cref="CharacterSet"/>.</summary>
public static class CharacterSetRules
{
    private const int FirstPrintableAscii = 0x20;
    private const int LastPrintableAscii = 0x7E;

    /// <summary>Whether a cell of a document using <paramref name="characterSet"/> may hold <paramref name="glyph"/>.</summary>
    public static bool Allows(this CharacterSet characterSet, Rune glyph) => characterSet switch
    {
        CharacterSet.Unicode => FillsACellOnItsOwn(glyph),
        CharacterSet.Ascii => glyph.Value is >= FirstPrintableAscii and <= LastPrintableAscii,
        _ => throw new ArgumentOutOfRangeException(nameof(characterSet), characterSet, null)
    };

    // Control characters are never stored (§6). Neither are characters that only change or join
    // their neighbours (combining accents, zero-width joiners, direction marks) or break lines:
    // one alone in a cell would be invisible or drawn on a dotted circle.
    private static bool FillsACellOnItsOwn(Rune glyph) => Rune.GetUnicodeCategory(glyph) switch
    {
        UnicodeCategory.Control => false,
        UnicodeCategory.Format => false,
        UnicodeCategory.NonSpacingMark => false,
        UnicodeCategory.SpacingCombiningMark => false,
        UnicodeCategory.EnclosingMark => false,
        UnicodeCategory.LineSeparator => false,
        UnicodeCategory.ParagraphSeparator => false,
        _ => true
    };
}
