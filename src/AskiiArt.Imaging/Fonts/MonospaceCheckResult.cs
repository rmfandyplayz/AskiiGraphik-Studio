// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Imaging.Fonts;

/// <summary>The outcome of <see cref="MonospaceCheck.Run"/>.</summary>
/// <param name="MismatchedGlyphs">
/// The printable ASCII glyphs whose advance differs from the reference glyph's, in code point order.
/// </param>
public sealed record MonospaceCheckResult(IReadOnlyList<Rune> MismatchedGlyphs)
{
    /// <summary>Whether every printable ASCII glyph the font has is as wide as the reference glyph.</summary>
    public bool IsMonospace => MismatchedGlyphs.Count == 0;
}
