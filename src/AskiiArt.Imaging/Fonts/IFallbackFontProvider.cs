// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// Finds another font for a glyph the document's font doesn't have (ARCHITECTURE §11, ADR 0013).
/// </summary>
public interface IFallbackFontProvider
{
    /// <summary>
    /// Returns a font, other than <paramref name="primary"/>, that has a glyph for
    /// <paramref name="rune"/>, or null when none does. The provider owns the fonts it returns;
    /// callers must not dispose them.
    /// </summary>
    LoadedFont? FindFontFor(Rune rune, LoadedFont primary);
}
