// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.GlyphSets;

/// <summary>One of the app's own character sets, which can't be changed, only duplicated (ADR 0053).</summary>
/// <param name="Id">A stable id, for example "askiiart.glyphs.arrows". Never shown to the user.</param>
/// <param name="NameKey">The App's resource key for the set's localized name.</param>
/// <param name="Glyphs">Its characters.</param>
public sealed record BuiltInGlyphSet(string Id, string NameKey, GlyphSet Glyphs);

/// <summary>
/// The app's own character sets (ADR 0053): Unicode blocks that are useful for text art and fill one cell each, in
/// the order the Characters window lists them. Blocks of wide characters, such as CJK, are left out (ARCHITECTURE §6).
/// </summary>
public static class BuiltInGlyphSets
{
    /// <summary>The printable ASCII characters, apart from the space.</summary>
    public static BuiltInGlyphSet BasicLatin { get; } = Block("basicLatin", "CharacterSetBasicLatin", 0x21, 0x7E);

    /// <summary>Every built-in set, in the order they're listed.</summary>
    public static IReadOnlyList<BuiltInGlyphSet> All { get; } =
    [
        BasicLatin,
        Block("latin1", "CharacterSetLatin1", 0xA1, 0xFF),
        Block("latinExtendedA", "CharacterSetLatinExtendedA", 0x100, 0x17F),
        Block("greek", "CharacterSetGreek", 0x370, 0x3FF),
        Block("cyrillic", "CharacterSetCyrillic", 0x400, 0x4FF),
        Block("punctuation", "CharacterSetPunctuation", 0x2010, 0x205E),
        Block("currency", "CharacterSetCurrency", 0x20A0, 0x20C0),
        Block("letterlike", "CharacterSetLetterlike", 0x2100, 0x214F),
        Block("numberForms", "CharacterSetNumberForms", 0x2150, 0x218B),
        Block("arrows", "CharacterSetArrows", 0x2190, 0x21FF),
        Block("math", "CharacterSetMath", 0x2200, 0x22FF),
        Block("boxDrawing", "CharacterSetBoxDrawing", 0x2500, 0x257F),
        Block("blocks", "CharacterSetBlocks", 0x2580, 0x259F),
        Block("geometric", "CharacterSetGeometric", 0x25A0, 0x25FF),
        Block("symbols", "CharacterSetSymbols", 0x2600, 0x26FF),
        Block("dingbats", "CharacterSetDingbats", 0x2700, 0x27BF),
        Block("braille", "CharacterSetBraille", 0x2800, 0x28FF),
        Block("legacyComputing", "CharacterSetLegacyComputing", 0x1FB00, 0x1FBFF)
    ];

    /// <summary>The built-in set with <paramref name="id"/>, or null when there's none.</summary>
    public static BuiltInGlyphSet? Find(string id) => All.FirstOrDefault(set => set.Id == id);

    private static BuiltInGlyphSet Block(string name, string nameKey, int first, int last) =>
        new("askiiart.glyphs." + name, nameKey, GlyphSet.FromRange(first, last));
}
