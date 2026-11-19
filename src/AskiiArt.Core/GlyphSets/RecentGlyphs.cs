// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Core.GlyphSets;

/// <summary>
/// The characters the Character Brush painted with most recently, newest first, which the Characters window lists as
/// Recent (ADR 0053). Shared by every document, as the tool settings are. Used on the UI thread only.
/// </summary>
public sealed class RecentGlyphs
{
    /// <summary>The most characters it keeps. Using another one forgets the oldest.</summary>
    public const int Capacity = 16;

    private readonly List<Rune> _glyphs = [];

    /// <summary>Creates an empty list.</summary>
    public RecentGlyphs()
    {
    }

    /// <summary>
    /// Creates a list of <paramref name="glyphs"/>, newest first, such as the ones saved last time. Any that can't be
    /// painted, are there again or are past <see cref="Capacity"/> are left out.
    /// </summary>
    public RecentGlyphs(IEnumerable<Rune> glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        _glyphs.AddRange(glyphs.Where(GlyphSet.CanHold).Distinct().Take(Capacity));
    }

    /// <summary>Raised after the list changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The characters, newest first.</summary>
    public IReadOnlyList<Rune> Glyphs => _glyphs;

    /// <summary>Puts <paramref name="glyph"/> first, as the newest.</summary>
    /// <exception cref="ArgumentException"><paramref name="glyph"/> can't be painted (<see cref="GlyphSet.CanHold"/>).
    /// </exception>
    public void Use(Rune glyph)
    {
        if (!GlyphSet.CanHold(glyph))
            throw new ArgumentException($"U+{glyph.Value:X4} can't be painted.", nameof(glyph));

        if (_glyphs.Count > 0 && _glyphs[0] == glyph)
            return;

        _glyphs.Remove(glyph);
        _glyphs.Insert(0, glyph);
        if (_glyphs.Count > Capacity)
            _glyphs.RemoveAt(Capacity);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The characters as text, newest first, as the settings save them.</summary>
    public override string ToString() => string.Concat(_glyphs.Select(glyph => glyph.ToString()));
}
