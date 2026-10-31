// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Imaging.Fonts;

namespace AskiiArt.Imaging.Tests.Fonts;

// What the OS finds differs between machines, so these tests check rules that hold everywhere
// rather than which font comes back.
public sealed class SystemFallbackFontProviderTests
{
    public static TheoryData<int> CodePoints => [0x6F22, 0x1F600, 0x2603, 'A', 0x10FFFD];

    [Theory]
    [MemberData(nameof(CodePoints))]
    public void FindFontFor_ReturnsNullOrAFontThatReallyHasTheGlyph(int codePoint)
    {
        using var primary = FontLoader.LoadBundled(BundledFonts.Default);
        using var provider = new SystemFallbackFontProvider();
        var rune = new Rune(codePoint);

        var font = provider.FindFontFor(rune, primary);

        if (font is null)
            return;

        Assert.True(font.HasGlyph(rune));
        Assert.Equal(FontOrigin.System, font.Origin);
    }

    [Fact]
    public void FindFontFor_SameRuneTwice_GivesTheSameAnswer()
    {
        using var primary = FontLoader.LoadBundled(BundledFonts.Default);
        using var provider = new SystemFallbackFontProvider();
        var rune = new Rune(0x6F22);

        Assert.Same(provider.FindFontFor(rune, primary), provider.FindFontFor(rune, primary));
    }

    [Fact]
    public void FindFontFor_AfterDispose_ThrowsObjectDisposedException()
    {
        using var primary = FontLoader.LoadBundled(BundledFonts.Default);
        var provider = new SystemFallbackFontProvider();
        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => provider.FindFontFor(new Rune('A'), primary));
    }
}
