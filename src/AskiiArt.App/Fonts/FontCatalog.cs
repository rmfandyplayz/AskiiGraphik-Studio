// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.App.Fonts;

/// <summary>Where a font the user can pick comes from.</summary>
internal enum FontSource
{
    Bundled,

    /// <summary>A TTF or OTF file the user loaded since the app started.</summary>
    File,

    System
}

/// <summary>A font the user can pick.</summary>
internal sealed record FontChoice(FontReference Font, FontSource Source);

/// <summary>
/// Every font the app can draw with: the bundled fonts, fonts loaded from files since the app
/// started, and the installed ones (ARCHITECTURE §11, ADR 0019). It turns a document's font
/// reference into a loaded font, loading each font once and keeping it until the app closes,
/// because glyph atlases are cached per loaded font.
/// </summary>
/// <remarks>
/// Documents store font names, not files, so two fonts with the same family name can't be told
/// apart. When a file holds a family that's already available, the one already available wins.
/// </remarks>
internal sealed class FontCatalog : IDisposable
{
    private readonly IReadOnlyList<BundledFont> _bundledFonts;
    private readonly Func<IReadOnlyList<string>> _getSystemFamilyNames;
    private readonly Func<string, LoadedFont?> _loadSystemFont;
    private readonly Dictionary<FontReference, LoadedFont> _loadedFonts = [];
    private readonly Dictionary<FontReference, MonospaceCheckResult> _monospaceChecks = [];
    private readonly List<FontReference> _fileFonts = [];
    private IReadOnlyList<string>? _systemFamilyNames;
    private bool _isDisposed;

    public FontCatalog()
        : this(BundledFonts.All, FontLoader.GetSystemFamilyNames, FontLoader.LoadSystem)
    {
    }

    /// <summary>For tests, which don't depend on the fonts installed on the machine.</summary>
    /// <param name="bundledFonts">The bundled fonts to offer; they must include the default font.</param>
    /// <param name="getSystemFamilyNames">Lists the installed font families.</param>
    /// <param name="loadSystemFont">Loads an installed family by name, or returns null.</param>
    internal FontCatalog(IReadOnlyList<BundledFont> bundledFonts, Func<IReadOnlyList<string>> getSystemFamilyNames,
        Func<string, LoadedFont?> loadSystemFont)
    {
        _bundledFonts = bundledFonts;
        _getSystemFamilyNames = getSystemFamilyNames;
        _loadSystemFont = loadSystemFont;

        DefaultFont = Load(FontReference.Default) ??
                      throw new InvalidOperationException("The default font is missing.");
    }

    /// <summary>The font a document falls back to when its own font isn't available (§5.10).</summary>
    public LoadedFont DefaultFont { get; }

    /// <summary>Every font the user can pick: bundled first, then loaded from files, then installed.</summary>
    public IReadOnlyList<FontChoice> GetChoices()
    {
        _systemFamilyNames ??= _getSystemFamilyNames();

        var choices = new List<FontChoice>();
        choices.AddRange(_bundledFonts.Select(font =>
            new FontChoice(new FontReference(font.FamilyName), FontSource.Bundled)));
        choices.AddRange(_fileFonts.Select(font => new FontChoice(font, FontSource.File)));

        var listed = choices.Select(choice => choice.Font).ToHashSet();
        choices.AddRange(_systemFamilyNames
            .Select(name => new FontReference(name))
            .Where(font => !listed.Contains(font))
            .Select(font => new FontChoice(font, FontSource.System)));

        return choices;
    }

    /// <summary>The loaded font for <paramref name="font"/>, or <see cref="DefaultFont"/> when it isn't available.</summary>
    public LoadedFont Resolve(FontReference font)
    {
        ArgumentNullException.ThrowIfNull(font);

        return Load(font) ?? DefaultFont;
    }

    /// <summary>Whether <paramref name="font"/> is monospaced; true for a font that isn't available.</summary>
    public bool IsMonospace(FontReference font)
    {
        ArgumentNullException.ThrowIfNull(font);

        if (!_monospaceChecks.TryGetValue(font, out var result))
        {
            if (Load(font) is not { } loaded)
                return true;

            result = MonospaceCheck.Run(loaded);
            _monospaceChecks.Add(font, result);
        }

        return result.IsMonospace;
    }

    /// <summary>
    /// Loads the font in <paramref name="path"/> and adds it to the choices until the app closes.
    /// Returns the font to use and whether the file's own font was added; it isn't when a font
    /// with that family name was already available.
    /// </summary>
    /// <exception cref="FontLoadException">The file isn't a font that can be loaded.</exception>
    /// <exception cref="IOException">The file can't be read.</exception>
    public (FontReference Font, bool WasAdded) AddFile(string path)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var loaded = FontLoader.LoadFromFile(path);
        var font = new FontReference(loaded.FamilyName);
        if (Load(font) is not null)
        {
            loaded.Dispose();
            return (font, false);
        }

        _loadedFonts.Add(font, loaded);
        _fileFonts.Add(font);
        return (font, true);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        foreach (var font in _loadedFonts.Values)
            font.Dispose();

        _loadedFonts.Clear();
    }

    private LoadedFont? Load(FontReference font)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_loadedFonts.TryGetValue(font, out var loaded))
            return loaded;

        loaded = LoadUncached(font);
        if (loaded is not null)
            _loadedFonts.Add(font, loaded);

        return loaded;
    }

    private LoadedFont? LoadUncached(FontReference font)
    {
        var bundled = _bundledFonts.FirstOrDefault(candidate => new FontReference(candidate.FamilyName) == font);
        if (bundled is not null)
            return FontLoader.LoadBundled(bundled);

        return _loadSystemFont(font.FamilyName);
    }
}
