// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Resources;
using AskiiArt.Core.GlyphSets;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Characters;

/// <summary>What a set in the Characters window is, which decides what can be done to it.</summary>
internal enum GlyphSetKind
{
    /// <summary>The characters the Character Brush painted with last, which only painting changes.</summary>
    Recent,

    /// <summary>One of the user's sets, which can be edited.</summary>
    User,

    /// <summary>One of the app's own sets, which can't be changed.</summary>
    BuiltIn
}

/// <summary>One set in the Characters window's dropdown: its name and its characters.</summary>
internal sealed class GlyphSetRow : ObservableObject
{
    /// <summary>Recent's id, which no library set has.</summary>
    public const string RecentId = "askiiart.glyphs.recent";

    private GlyphSetRow(string id, GlyphSetKind kind, string name, IReadOnlyList<Rune> glyphs)
    {
        Id = id;
        Kind = kind;
        Name = name;
        Glyphs = glyphs;
    }

    /// <summary>The set's id: a built-in or user set's id in the library, or <see cref="RecentId"/>.</summary>
    public string Id { get; }

    public GlyphSetKind Kind { get; }

    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>The set's characters, in order. Only Recent's can be none.</summary>
    public IReadOnlyList<Rune> Glyphs { get; private set; }

    /// <summary>The set's characters as text, as the editor shows them.</summary>
    public string Characters => string.Concat(Glyphs.Select(glyph => glyph.ToString()));

    public static GlyphSetRow ForRecent(RecentGlyphs recent) =>
        new(RecentId, GlyphSetKind.Recent, Strings.CharacterSetRecent, [.. recent.Glyphs]);

    public static GlyphSetRow For(BuiltInGlyphSet set) =>
        new(set.Id, GlyphSetKind.BuiltIn, UiText.Get(set.NameKey), set.Glyphs.Glyphs);

    public static GlyphSetRow For(UserGlyphSet set) => new(set.Id, GlyphSetKind.User, set.Name, set.Glyphs.Glyphs);

    /// <summary>Shows <paramref name="set"/>, the user set this row is for, as it is now.</summary>
    public void Update(UserGlyphSet set)
    {
        Name = set.Name;
        Glyphs = set.Glyphs.Glyphs;
    }

    /// <summary>Shows <paramref name="recent"/>'s characters as they are now, if this is Recent's row.</summary>
    public void Update(RecentGlyphs recent) => Glyphs = [.. recent.Glyphs];
}
