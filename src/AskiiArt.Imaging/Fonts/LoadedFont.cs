// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using SkiaSharp;

namespace AskiiArt.Imaging.Fonts;

/// <summary>
/// A font that's ready to measure and draw. Create one with <see cref="FontLoader"/>. Whoever
/// loads a font owns it and disposes it once nothing draws with it any more.
/// </summary>
public sealed class LoadedFont : IDisposable
{
    private readonly SKTypeface _typeface;
    private bool _isDisposed;

    internal LoadedFont(SKTypeface typeface, FontOrigin origin)
    {
        _typeface = typeface;
        Origin = origin;
        FamilyName = typeface.FamilyName;
    }

    /// <summary>The family name stored in the font, for example "JetBrains Mono".</summary>
    public string FamilyName { get; }

    /// <summary>Where the font came from.</summary>
    public FontOrigin Origin { get; }

    internal SKTypeface Typeface
    {
        get
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _typeface;
        }
    }

    /// <summary>Whether the font has its own glyph for <paramref name="rune"/>.</summary>
    public bool HasGlyph(Rune rune) => Typeface.ContainsGlyph(rune.Value);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _typeface.Dispose();
    }

    /// <inheritdoc/>
    public override string ToString() => $"{FamilyName} ({Origin})";
}
