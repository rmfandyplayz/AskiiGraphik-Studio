// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Tests.Fonts;

public sealed class FontLoaderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "AskiiArtTests", Guid.NewGuid().ToString("N"));

    public FontLoaderTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void LoadFromFile_FontFile_LoadsItWithFileOrigin()
    {
        var path = Path.Combine(_folder, "font.ttf");
        using (var file = File.Create(path))
        using (var resource = BundledFonts.RobotoMono.OpenFontStream())
            resource.CopyTo(file);

        using var font = FontLoader.LoadFromFile(path);

        Assert.Equal("Roboto Mono", font.FamilyName);
        Assert.Equal(FontOrigin.File, font.Origin);
    }

    [Fact]
    public void LoadFromFile_FileThatIsNotAFont_ThrowsFontLoadException()
    {
        var path = Path.Combine(_folder, "not-a-font.ttf");
        File.WriteAllText(path, "This is plain text, not a font.");

        Assert.Throws<FontLoadException>(() => FontLoader.LoadFromFile(path));
    }

    [Fact]
    public void LoadFromFile_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => FontLoader.LoadFromFile(Path.Combine(_folder, "missing.ttf")));
    }

    [Fact]
    public void LoadSystem_UnknownFamily_ReturnsNull()
    {
        Assert.Null(FontLoader.LoadSystem("No Such Font Family 7c1e"));
    }

    [Fact]
    public void GetSystemFamilyNames_IsSortedWithoutDuplicatesOrHiddenFamilies()
    {
        var names = FontLoader.GetSystemFamilyNames();

        Assert.Equal(names.Order(StringComparer.OrdinalIgnoreCase), names);
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(names, name => name.StartsWith('.'));
    }

    [Fact]
    public void LoadSystem_InstalledFamily_LoadsItWithSystemOrigin()
    {
        var names = FontLoader.GetSystemFamilyNames();
        Assert.SkipWhen(names.Count == 0, "No fonts are installed on this machine.");

        var loaded = names.Select(FontLoader.LoadSystem).FirstOrDefault(font => font is not null);
        using var font = Assert.IsType<LoadedFont>(loaded);

        Assert.Equal(FontOrigin.System, font.Origin);
        Assert.Contains(font.FamilyName, names, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadedFont_AfterDispose_ThrowsObjectDisposedException()
    {
        var font = FontLoader.LoadBundled(BundledFonts.Default);
        font.Dispose();

        Assert.Throws<ObjectDisposedException>(() => font.HasGlyph(new Rune('A')));
    }
}
