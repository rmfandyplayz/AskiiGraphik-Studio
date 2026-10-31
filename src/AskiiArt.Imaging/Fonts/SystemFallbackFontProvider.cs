// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using SkiaSharp;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// Asks the operating system for an installed font that has the glyph (ADR 0013). Results depend
/// on the machine: a glyph found here may be missing on another computer.
/// </summary>
public sealed class SystemFallbackFontProvider : IFallbackFontProvider, IDisposable
{
    private readonly Lock _lock = new();

    // Remembers the answer for each code point (including "none"), so the OS is asked once.
    private readonly Dictionary<int, LoadedFont?> _fontsByCodePoint = [];

    // One LoadedFont per family, however many code points it answers for.
    private readonly Dictionary<string, LoadedFont> _fontsByFamily = new(StringComparer.Ordinal);
    private bool _isDisposed;

    /// <inheritdoc/>
    public LoadedFont? FindFontFor(Rune rune, LoadedFont primary)
    {
        ArgumentNullException.ThrowIfNull(primary);

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_fontsByCodePoint.TryGetValue(rune.Value, out var known))
                return known;

            var found = MatchInstalledFont(rune);
            _fontsByCodePoint[rune.Value] = found;
            return found;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            foreach (var font in _fontsByFamily.Values)
                font.Dispose();

            _fontsByFamily.Clear();
            _fontsByCodePoint.Clear();
        }
    }

    private LoadedFont? MatchInstalledFont(Rune rune)
    {
        var typeface = SKFontManager.Default.MatchCharacter(rune.Value);
        if (typeface is null)
            return null;

        // Some platforms answer with their default font even when it lacks the glyph.
        if (!typeface.ContainsGlyph(rune.Value))
        {
            typeface.Dispose();
            return null;
        }

        if (_fontsByFamily.TryGetValue(typeface.FamilyName, out var existing))
        {
            typeface.Dispose();
            return existing;
        }

        var font = new LoadedFont(typeface, FontOrigin.System);
        _fontsByFamily.Add(font.FamilyName, font);
        return font;
    }
}
