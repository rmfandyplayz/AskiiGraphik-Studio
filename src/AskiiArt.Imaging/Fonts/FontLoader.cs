// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using SkiaSharp;

namespace AskiiArt.Imaging.Fonts;

/// <summary>Loads fonts from the app, the system or a file (ARCHITECTURE §11, ADR 0013).</summary>
public static class FontLoader
{
    /// <summary>Loads one of the <see cref="BundledFonts"/>.</summary>
    public static LoadedFont LoadBundled(BundledFont font)
    {
        ArgumentNullException.ThrowIfNull(font);

        using var stream = font.OpenFontStream();
        using var data = SKData.Create(stream);
        var typeface = SKTypeface.FromData(data)
                       ?? throw new FontLoadException($"The bundled font '{font.FamilyName}' could not be read.");
        return new LoadedFont(typeface, FontOrigin.Bundled);
    }

    /// <summary>Loads the first font in a TTF, OTF or font collection file.</summary>
    /// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
    /// <exception cref="FontLoadException">The file isn't a font Skia can read.</exception>
    public static LoadedFont LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("The font file doesn't exist.", path);

        using var data = SKData.Create(path)
                         ?? throw new FontLoadException($"The font file '{path}' could not be read.");
        var typeface = SKTypeface.FromData(data)
                       ?? throw new FontLoadException($"The file '{path}' is not a font that can be loaded.");
        return new LoadedFont(typeface, FontOrigin.File);
    }

    /// <summary>
    /// Loads the regular style of an installed font family, or returns null when no installed
    /// family has that name.
    /// </summary>
    public static LoadedFont? LoadSystem(string familyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        var typeface = SKFontManager.Default.MatchFamily(familyName, SKFontStyle.Normal);
        if (typeface is null)
            return null;

        if (string.Equals(typeface.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
            return new LoadedFont(typeface, FontOrigin.System);

        // Some platforms substitute a different family for an unknown name; that's not a match.
        typeface.Dispose();
        return null;
    }

    /// <summary>
    /// The names of the installed font families, sorted and without duplicates. This only reads
    /// names; checking whether a family is monospace happens when it's loaded.
    /// </summary>
    public static IReadOnlyList<string> GetSystemFamilyNames() =>
    [
        .. SKFontManager.Default.GetFontFamilies()
            // macOS lists hidden system families whose names start with a dot.
            .Where(name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith('.'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
    ];
}
