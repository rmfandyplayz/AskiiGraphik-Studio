// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Tests.Fonts;

public sealed class BundledFontsTests
{
    [Fact]
    public void All_ListsTheFourFontsFromTheArchitectureWithTheDefaultFirst()
    {
        Assert.Equal(["JetBrains Mono", "Roboto Mono", "IBM Plex Mono", "Fira Code"],
            BundledFonts.All.Select(font => font.FamilyName));
        Assert.Same(BundledFonts.JetBrainsMono, BundledFonts.Default);
        Assert.Same(BundledFonts.Default, BundledFonts.All[0]);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void LoadBundled_LoadsTheFontWithItsFamilyName(string familyName)
    {
        using var font = TestFonts.Load(familyName);

        Assert.Equal(familyName, font.FamilyName);
        Assert.Equal(FontOrigin.Bundled, font.Origin);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void LicenseFilePath_PointsToTheOpenFontLicenseCopiedNextToTheApp(string familyName)
    {
        var bundled = TestFonts.Bundled(familyName);

        Assert.Equal("OFL-1.1", bundled.License);
        Assert.True(File.Exists(bundled.LicenseFilePath), bundled.LicenseFilePath);
        Assert.Contains("SIL Open Font License", File.ReadAllText(bundled.LicenseFilePath), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void BundledFont_HasEveryPrintableAsciiGlyph(string familyName)
    {
        using var font = TestFonts.Load(familyName);

        Rune[] missing =
        [
            .. Enumerable.Range(' ', '~' - ' ' + 1)
                .Select(codePoint => new Rune(codePoint))
                .Where(rune => !font.HasGlyph(rune))
        ];

        Assert.Empty(missing);
    }

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void BundledFont_PassesTheMonospaceCheck(string familyName)
    {
        using var font = TestFonts.Load(familyName);

        var result = MonospaceCheck.Run(font);

        Assert.True(result.IsMonospace, string.Join(" ", result.MismatchedGlyphs));
    }
}
