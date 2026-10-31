// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Imaging.Fonts;
using SkiaSharp;

namespace AskiiArt.Imaging.Tests.Glyphs;

// Writes artifacts/font-sheets/<font>.png for the owner to check by eye (step 1.3's test doc).
// CI uploads the folder from each operating system. The sheets aren't snapshot-compared: each
// OS's font engine anti-aliases slightly differently, so the pixels legitimately differ.
public sealed class FontSheetTests
{
    private static readonly float[] _pixelSizes = [16, 32];

    [Theory]
    [MemberData(nameof(TestFonts.BundledFamilyNames), MemberType = typeof(TestFonts))]
    public void Render_BundledFont_WritesAFontSheetPng(string familyName)
    {
        using var font = TestFonts.Load(familyName);
        using var fallbackFonts = new SystemFallbackFontProvider();
        var path = Path.Combine(FindRepositoryRoot(), "artifacts", "font-sheets", familyName.Replace(' ', '-') + ".png");

        using (var sheet = FontSheet.Render(font, fallbackFonts, _pixelSizes))
            FontSheet.SavePng(sheet, path);

        TestContext.Current.TestOutputHelper?.WriteLine(path);
        using var saved = SKBitmap.Decode(path);
        Assert.True(saved is { Width: > 0, Height: > 0 });
    }

    // The sheet lives in the repository's artifacts folder (ignored by git) so it's easy to find.
    private static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "AskiiArt.slnx")))
                return folder.FullName;
        }

        throw new InvalidOperationException("Couldn't find the repository root (no AskiiArt.slnx above the tests).");
    }
}
