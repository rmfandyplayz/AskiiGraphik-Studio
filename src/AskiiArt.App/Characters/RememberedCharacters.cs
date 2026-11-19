// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Resources;
using AskiiArt.App.Settings;
using AskiiArt.Core.GlyphSets;

namespace AskiiArt.App.Characters;

/// <summary>
/// The user's character sets and the characters the Character Brush painted with last, kept in settings between runs
/// (ARCHITECTURE §15, ADR 0053). Each is saved whenever it changes.
/// </summary>
internal static class RememberedCharacters
{
    internal const string SetsSettingKey = "characterSets";
    internal const string RecentSettingKey = "recentCharacters";

    /// <summary>
    /// A library holding the sets saved last time, which saves them again whenever they change. A saved set that can't
    /// be read is left out. No set can take a built-in set's name, or Recent's.
    /// </summary>
    public static GlyphSetLibrary CreateLibrary(IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var saved = settings.GetText(SetsSettingKey) is { } text ? UserGlyphSetsJson.Read(text) : [];
        IEnumerable<string> reservedNames =
            [Strings.CharacterSetRecent, .. BuiltInGlyphSets.All.Select(set => UiText.Get(set.NameKey))];
        var library = new GlyphSetLibrary(reservedNames, saved);
        library.Changed += (_, _) => settings.SetText(SetsSettingKey, UserGlyphSetsJson.Write(library.UserSets));
        return library;
    }

    /// <summary>
    /// The characters saved last time, newest first, which are saved again whenever they change. A saved character
    /// that can't be painted is left out.
    /// </summary>
    public static RecentGlyphs CreateRecent(IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        IReadOnlyList<Rune> saved = settings.GetText(RecentSettingKey) is { } text ? [.. text.EnumerateRunes()] : [];
        var recent = new RecentGlyphs(saved);
        recent.Changed += (_, _) => settings.SetText(RecentSettingKey, recent.ToString());
        return recent;
    }
}
