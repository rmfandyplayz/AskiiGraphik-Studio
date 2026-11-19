// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.Core.Tests.GlyphSets;

public sealed class RecentGlyphsTests
{
    private readonly RecentGlyphs _recent = new();
    private int _changes;

    public RecentGlyphsTests()
    {
        _recent.Changed += (_, _) => _changes++;
    }

    [Fact]
    public void Use_PutsTheCharacterFirst_AndMovesOneUsedBefore()
    {
        _recent.Use(new Rune('a'));
        _recent.Use(new Rune('b'));
        _recent.Use(new Rune('c'));
        _recent.Use(new Rune('a'));

        Assert.Equal("acb", _recent.ToString());
        Assert.Equal(4, _changes);
    }

    [Fact]
    public void UsingTheNewestAgain_IsNoChange()
    {
        _recent.Use(new Rune('a'));
        _recent.Use(new Rune('a'));

        Assert.Equal(1, _changes);
    }

    [Fact]
    public void TheOldest_IsForgotten_PastTheCapacity()
    {
        for (var index = 0; index <= RecentGlyphs.Capacity; index++)
            _recent.Use(new Rune('A' + index));

        Assert.Equal(RecentGlyphs.Capacity, _recent.Glyphs.Count);
        Assert.Equal(new Rune('A' + RecentGlyphs.Capacity), _recent.Glyphs[0]);
        Assert.DoesNotContain(new Rune('A'), _recent.Glyphs);
    }

    [Fact]
    public void ACharacterASetCantHold_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => _recent.Use(new Rune(' ')));
        Assert.Empty(_recent.Glyphs);
    }

    [Fact]
    public void SavedCharacters_AreKept_WithoutRepeatsOrOnesASetCantHold_UpToTheCapacity()
    {
        var saved = "ab a\u0007cxxxdefghijklmnopqrstuvwxyz";

        var recent = new RecentGlyphs(saved.EnumerateRunes());

        Assert.Equal("abcxdefghijklmno", recent.ToString());
    }
}
