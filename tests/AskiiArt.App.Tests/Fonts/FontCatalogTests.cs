// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.App.Tests.Fonts;

public sealed class FontCatalogTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("askiiart-fonts-").FullName;

    /// <summary>The Fira Code file copied next to the tests, which isn't bundled in <see cref="CreateCatalog"/>.</summary>
    internal static string FiraCodeFile { get; } =
        Path.Combine(AppContext.BaseDirectory, "TestFonts", "FiraCode-Regular.ttf");

    [Fact]
    public void IsAvailable_IsTrueForBundledAndLoadedFonts_AndFalseForOthers()
    {
        using var catalog = CreateCatalog();

        Assert.True(catalog.IsAvailable(FontReference.Default));
        Assert.False(catalog.IsAvailable(new FontReference("Fira Code")));

        catalog.AddFile(FiraCodeFile);
        Assert.True(catalog.IsAvailable(new FontReference("fira code")));
    }

    [Fact]
    public void Choices_ListBundledFirst_ThenInstalled_WithoutRepeats()
    {
        using var catalog = new FontCatalog(BundledFonts.All, () => ["Courier New", "JetBrains Mono", "Arial"], _ => null);

        var choices = catalog.GetChoices();

        Assert.Equal(["JetBrains Mono", "Roboto Mono", "IBM Plex Mono", "Fira Code", "Courier New", "Arial"],
            choices.Select(choice => choice.Font.FamilyName));
        Assert.All(choices.Take(4), choice => Assert.Equal(FontSource.Bundled, choice.Source));
        Assert.All(choices.Skip(4), choice => Assert.Equal(FontSource.System, choice.Source));
    }

    [Fact]
    public void Resolve_LoadsAFontOnce_AndFallsBackToTheDefault()
    {
        using var catalog = CreateCatalog();

        var first = catalog.Resolve(new FontReference("jetbrains mono"));

        Assert.Same(catalog.DefaultFont, first);
        Assert.Same(catalog.DefaultFont, catalog.Resolve(new FontReference("Not Installed Anywhere")));
    }

    [Fact]
    public void AddFile_WithANewFamily_AddsItToTheChoices()
    {
        using var catalog = CreateCatalog();

        var (font, wasAdded) = catalog.AddFile(FiraCodeFile);

        Assert.True(wasAdded);
        Assert.Equal(new FontReference("Fira Code"), font);
        Assert.Contains(new FontChoice(font, FontSource.File), catalog.GetChoices());
        Assert.Equal("Fira Code", catalog.Resolve(font).FamilyName);
        Assert.Equal(FontOrigin.File, catalog.Resolve(font).Origin);
    }

    [Fact]
    public void AddFile_WithAFamilyAlreadyAvailable_KeepsTheOneAlreadyThere()
    {
        using var catalog = new FontCatalog(BundledFonts.All, () => [], _ => null);
        var bundled = catalog.Resolve(new FontReference("Fira Code"));

        var (font, wasAdded) = catalog.AddFile(FiraCodeFile);

        Assert.False(wasAdded);
        Assert.Same(bundled, catalog.Resolve(font));
        Assert.DoesNotContain(catalog.GetChoices(), choice => choice.Source == FontSource.File);
    }

    [Fact]
    public void AddFile_ThatIsNotAFont_Throws()
    {
        using var catalog = CreateCatalog();
        var path = Path.Combine(_folder, "notes.ttf");
        File.WriteAllText(path, "not a font");

        Assert.Throws<FontLoadException>(() => catalog.AddFile(path));
    }

    [Fact]
    public void IsMonospace_IsTrueForTheBundledFonts()
    {
        using var catalog = new FontCatalog(BundledFonts.All, () => [], _ => null);

        Assert.All(BundledFonts.All, font => Assert.True(catalog.IsMonospace(new FontReference(font.FamilyName))));
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>A catalog with only the default font bundled and nothing installed.</summary>
    internal static FontCatalog CreateCatalog() => new([BundledFonts.Default], () => [], _ => null);
}
