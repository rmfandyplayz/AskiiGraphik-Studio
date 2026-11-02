// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Fonts;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.App.Tests.Fonts;

public sealed class FontCatalogTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("askiiart-fonts-").FullName;

    /// <summary>The Fira Code file copied next to the tests, which isn't bundled in <see cref="CreateCatalog()"/>.</summary>
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

    [Fact]
    public void AddFile_IsStillThereAfterARestart()
    {
        var settings = new FakeAppSettings();
        using (var firstRun = CreateCatalog(settings))
            firstRun.AddFile(FiraCodeFile);

        using var secondRun = CreateCatalog(settings);

        var font = new FontReference("Fira Code");
        Assert.Contains(new FontChoice(font, FontSource.File), secondRun.GetChoices());
        Assert.True(secondRun.IsAvailable(font));
        Assert.Equal(FontOrigin.File, secondRun.Resolve(font).Origin);
    }

    [Fact]
    public void AddFile_WithAFamilyAlreadyAvailable_IsNotRemembered()
    {
        var settings = new FakeAppSettings();
        using var catalog = new FontCatalog(BundledFonts.All, () => [], _ => null, new RememberedFontFiles(settings));

        catalog.AddFile(FiraCodeFile);

        Assert.Empty(new RememberedFontFiles(settings).Load());
    }

    [Fact]
    public void AddFile_ThatIsNotAFont_IsNotRemembered()
    {
        var settings = new FakeAppSettings();
        using var catalog = CreateCatalog(settings);
        var path = Path.Combine(_folder, "notes.ttf");
        File.WriteAllText(path, "not a font");

        Assert.Throws<FontLoadException>(() => catalog.AddFile(path));

        Assert.Empty(new RememberedFontFiles(settings).Load());
    }

    [Fact]
    public void ARememberedFileThatIsGone_IsLeftOut_AndComesBackWithTheFile()
    {
        var settings = new FakeAppSettings();
        var path = Path.Combine(_folder, "font.ttf");
        File.Copy(FiraCodeFile, path);
        using (var firstRun = CreateCatalog(settings))
            firstRun.AddFile(path);

        File.Delete(path);
        using (var whileGone = CreateCatalog(settings))
        {
            Assert.False(whileGone.IsAvailable(new FontReference("Fira Code")));
            Assert.DoesNotContain(whileGone.GetChoices(), choice => choice.Source == FontSource.File);
        }

        File.Copy(FiraCodeFile, path);
        using var afterReturning = CreateCatalog(settings);

        Assert.True(afterReturning.IsAvailable(new FontReference("Fira Code")));
    }

    [Fact]
    public void ARememberedFileThatIsDamaged_IsLeftOut()
    {
        var settings = new FakeAppSettings();
        var path = Path.Combine(_folder, "damaged.ttf");
        File.WriteAllText(path, "not a font");
        new RememberedFontFiles(settings).Add(path);

        using var catalog = CreateCatalog(settings);

        Assert.DoesNotContain(catalog.GetChoices(), choice => choice.Source == FontSource.File);
    }

    [Fact]
    public void ARememberedFileWhoseFamilyIsNowAvailable_UsesTheOneAlreadyThere()
    {
        var settings = new FakeAppSettings();
        new RememberedFontFiles(settings).Add(FiraCodeFile);

        using var catalog = new FontCatalog(BundledFonts.All, () => [], _ => null, new RememberedFontFiles(settings));

        Assert.DoesNotContain(catalog.GetChoices(), choice => choice.Source == FontSource.File);
        Assert.Equal(FontOrigin.Bundled, catalog.Resolve(new FontReference("Fira Code")).Origin);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    /// <summary>A catalog with only the default font bundled and nothing installed.</summary>
    internal static FontCatalog CreateCatalog() => new([BundledFonts.Default], () => [], _ => null);

    private static FontCatalog CreateCatalog(FakeAppSettings settings) =>
        new([BundledFonts.Default], () => [], _ => null, new RememberedFontFiles(settings));
}
