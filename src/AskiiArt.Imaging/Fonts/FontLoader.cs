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
        return CreateUsable(typeface, FontOrigin.Bundled, $"The bundled font '{font.FamilyName}'");
    }

    /// <summary>Loads the first font in a TTF, OTF or font collection file.</summary>
    /// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
    /// <exception cref="FontLoadException">
    /// The file isn't a font Skia can read, or it's damaged in a way that would break drawing.
    /// </exception>
    public static LoadedFont LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("The font file doesn't exist.", path);

        using var data = SKData.Create(path)
                         ?? throw new FontLoadException($"The font file '{path}' could not be read.");
        var typeface = SKTypeface.FromData(data)
                       ?? throw new FontLoadException($"The file '{path}' is not a font that can be loaded.");
        return CreateUsable(typeface, FontOrigin.File, $"The font in '{path}'");
    }

    /// <summary>
    /// Loads the regular style of an installed font family, or returns null when no installed
    /// family has that name or its file is damaged.
    /// </summary>
    public static LoadedFont? LoadSystem(string familyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);

        var typeface = SKFontManager.Default.MatchFamily(familyName, SKFontStyle.Normal);
        if (typeface is null)
            return null;

        // Some platforms substitute a different family for an unknown name; that's not a match.
        if (!string.Equals(typeface.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
        {
            typeface.Dispose();
            return null;
        }

        var font = new LoadedFont(typeface, FontOrigin.System);
        if (FindProblem(font) is null)
            return font;

        font.Dispose();
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

    /// <summary>
    /// Why <paramref name="font"/> can't be drawn with, or null when it can. Skia accepts some
    /// damaged files, so this catches the damage that would break the app later: no name to
    /// store in a document, no glyphs, or a cell size that's zero, negative or enormous (which
    /// would make cells vanish or need glyph atlas pages too big to create).
    /// </summary>
    internal static string? FindProblem(LoadedFont font)
    {
        if (string.IsNullOrWhiteSpace(font.FamilyName))
            return "it has no family name";

        if (font.Typeface.GlyphCount <= 0)
            return "it has no glyphs";

        if (!CellMetrics.HasUsableSize(font))
            return "its cell size is broken";

        return null;
    }

    private static LoadedFont CreateUsable(SKTypeface typeface, FontOrigin origin, string description)
    {
        var font = new LoadedFont(typeface, origin);
        if (FindProblem(font) is not { } problem)
            return font;

        font.Dispose();
        throw new FontLoadException($"{description} can't be used: {problem}.");
    }
}
