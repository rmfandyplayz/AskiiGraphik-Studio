// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.GlyphSets;

/// <summary>What's wrong with some text as a character set's characters (<see cref="GlyphSet.Check"/>).</summary>
public enum GlyphSetTextProblem
{
    /// <summary>Nothing: the text makes a character set.</summary>
    None,

    /// <summary>There are no characters, apart from spaces and line breaks.</summary>
    Empty,

    /// <summary>A character can't fill a cell, such as a tab or a combining accent.</summary>
    Unusable
}

/// <summary>Whether some text can make a character set, and if not, why.</summary>
/// <param name="Problem">The first thing wrong, or <see cref="GlyphSetTextProblem.None"/>.</param>
/// <param name="Glyph">The character it's about, for <see cref="GlyphSetTextProblem.Unusable"/>.</param>
public readonly record struct GlyphSetTextCheck(GlyphSetTextProblem Problem, Rune Glyph)
{
    /// <summary>Whether the text makes a character set.</summary>
    public bool IsValid => Problem == GlyphSetTextProblem.None;
}

/// <summary>
/// Characters to choose from in the Characters window (ADR 0053), in order, each once. The user calls it a character
/// set; the code says glyph set, because <see cref="CharacterSet"/> is a document's choice of Unicode or ASCII. Every
/// glyph fills a cell and shows ink, so it's one the Character Brush can paint: a space, which shows nothing, can't be
/// one. A glyph set is an immutable value, and its name belongs to wherever it's kept.
/// </summary>
public sealed class GlyphSet : IEquatable<GlyphSet>
{
    private readonly Rune[] _glyphs;

    /// <summary>Creates a set of <paramref name="glyphs"/>, in order. A glyph that's there again is left out.</summary>
    /// <exception cref="ArgumentException">There are no glyphs, or one can't be in a set (<see cref="CanHold"/>).
    /// </exception>
    public GlyphSet(IEnumerable<Rune> glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);

        _glyphs = [.. glyphs.Distinct()];
        if (_glyphs.Length == 0)
            throw new ArgumentException("A character set needs at least one character.", nameof(glyphs));

        foreach (var glyph in _glyphs)
        {
            if (!CanHold(glyph))
                throw new ArgumentException($"A character set can't hold U+{glyph.Value:X4}.", nameof(glyphs));
        }
    }

    /// <summary>The glyphs, in order.</summary>
    public IReadOnlyList<Rune> Glyphs => _glyphs;

    /// <summary>
    /// Whether <paramref name="glyph"/> can be in a set, and so painted by the Character Brush: whether it fills a
    /// cell on its own (ARCHITECTURE §6) and isn't a space or another kind of blank.
    /// </summary>
    public static bool CanHold(Rune glyph) => CharacterSet.Unicode.Allows(glyph) && !Rune.IsWhiteSpace(glyph);

    /// <summary>
    /// Whether the characters in <paramref name="text"/> can make a set, and if not, the first thing wrong, so the
    /// Characters window can say why. Spaces and line breaks only separate characters, so they're skipped, as are
    /// characters that are there again.
    /// </summary>
    public static GlyphSetTextCheck Check(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var any = false;
        foreach (var glyph in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(glyph))
                continue;

            if (!CanHold(glyph))
                return new GlyphSetTextCheck(GlyphSetTextProblem.Unusable, glyph);

            any = true;
        }

        return new GlyphSetTextCheck(any ? GlyphSetTextProblem.None : GlyphSetTextProblem.Empty, default);
    }

    /// <summary>The set of the characters in <paramref name="text"/>, skipping spaces, line breaks and repeats.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="Check"/> finds something wrong.</exception>
    public static GlyphSet FromText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new GlyphSet(text.EnumerateRunes().Where(glyph => !Rune.IsWhiteSpace(glyph)));
    }

    /// <summary>
    /// Every character from <paramref name="first"/> to <paramref name="last"/> that Unicode assigns and a set can
    /// hold, such as a Unicode block's. Private use characters are left out, as they mean nothing without a font
    /// that gives them one.
    /// </summary>
    /// <exception cref="ArgumentException">The range holds none.</exception>
    public static GlyphSet FromRange(int first, int last)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(first, last);

        var glyphs = Enumerable.Range(first, last - first + 1)
            .Where(Rune.IsValid)
            .Select(value => new Rune(value))
            .Where(glyph => CanHold(glyph) && Rune.GetUnicodeCategory(glyph) is not
                (UnicodeCategory.OtherNotAssigned or UnicodeCategory.PrivateUse));
        return new GlyphSet(glyphs);
    }

    /// <summary>The glyphs as text, in order.</summary>
    public override string ToString() => string.Concat(_glyphs.Select(glyph => glyph.ToString()));

    /// <inheritdoc/>
    public bool Equals(GlyphSet? other) => other is not null && _glyphs.AsSpan().SequenceEqual(other._glyphs);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as GlyphSet);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var glyph in _glyphs)
            hash.Add(glyph);

        return hash.ToHashCode();
    }
}
