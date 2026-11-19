// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.GlyphSets;

/// <summary>
/// A file holding one character set, to share it (ADR 0053), like a ramp file: readable JSON naming the format and its
/// version, then the set's name and its characters as one string.
/// </summary>
public static class GlyphSetFile
{
    /// <summary>The extension character set files have. It may change with the product's name, so it's kept here.
    /// </summary>
    public const string Extension = ".asciicharset";

    /// <summary>The newest format version, which this app writes.</summary>
    public const int CurrentVersion = 1;

    private const string FormatName = "askiiart.charset";

    /// <summary>The text of a file holding <paramref name="glyphs"/>, named <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public static string Write(string name, GlyphSet glyphs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(glyphs);

        return LibraryJson.WriteFile(FormatName, CurrentVersion, json => GlyphSetJson.Write(json, name.Trim(), glyphs));
    }

    /// <summary>Reads the set in <paramref name="text"/>, a character set file's contents.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="glyphs">The set and its name, when it could be read.</param>
    /// <param name="problem"><see cref="LibraryFileProblem.None"/>, or why it couldn't be read.</param>
    /// <returns>Whether it could be read.</returns>
    public static bool TryRead(string text, [NotNullWhen(true)] out NamedGlyphSet? glyphs,
        out LibraryFileProblem problem)
    {
        glyphs = null;
        problem = LibraryJson.ReadFile(text, FormatName, CurrentVersion, out var json);
        if (problem != LibraryFileProblem.None)
            return false;

        if (!GlyphSetJson.TryRead(json, out glyphs))
            problem = LibraryFileProblem.Damaged;

        return glyphs is not null;
    }
}

/// <summary>
/// The user's character sets as the settings save them (ADR 0053): a JSON array, oldest first, of each set's id and
/// what a character set file holds.
/// </summary>
public static class UserGlyphSetsJson
{
    private const string IdKey = "id";

    /// <summary>The JSON text of <paramref name="sets"/>.</summary>
    public static string Write(IEnumerable<UserGlyphSet> sets)
    {
        ArgumentNullException.ThrowIfNull(sets);

        var json = new JsonArray();
        foreach (var set in sets)
        {
            var entry = new JsonObject { [IdKey] = set.Id };
            GlyphSetJson.Write(entry, set.Name, set.Glyphs);
            json.Add(entry);
        }

        return json.ToJsonString(LibraryJson.WriteOptions);
    }

    /// <summary>
    /// The sets in <paramref name="text"/>, made by <see cref="Write"/>. A set that can't be read is left out, so one
    /// damaged entry doesn't cost the rest, and text that isn't an array holds none.
    /// </summary>
    public static IReadOnlyList<UserGlyphSet> Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        JsonArray? json;
        try
        {
            json = JsonNode.Parse(text) as JsonArray;
        }
        catch (JsonException)
        {
            return [];
        }

        if (json is null)
            return [];

        var sets = new List<UserGlyphSet>();
        foreach (var entry in json.OfType<JsonObject>())
        {
            if (LibraryJson.ReadString(entry, IdKey) is { } id && GlyphSetJson.TryRead(entry, out var set))
                sets.Add(new UserGlyphSet(id, set.Name, set.Glyphs));
        }

        return sets;
    }
}

/// <summary>How a character set file and the saved user sets write a set: its name and its characters as one string.
/// </summary>
internal static class GlyphSetJson
{
    public static void Write(JsonObject json, string name, GlyphSet glyphs)
    {
        json[LibraryJson.NameKey] = name;
        json[LibraryJson.CharactersKey] = glyphs.ToString();
    }

    /// <summary>The set <paramref name="json"/> holds, or false when anything about it can't be read.</summary>
    public static bool TryRead(JsonObject json, [NotNullWhen(true)] out NamedGlyphSet? glyphs)
    {
        glyphs = null;
        if (UserItemList<UserGlyphSet>.Trimmed(LibraryJson.ReadString(json, LibraryJson.NameKey)) is not { } name)
            return false;

        var characters = LibraryJson.ReadString(json, LibraryJson.CharactersKey);
        if (characters is null || !GlyphSet.Check(characters).IsValid)
            return false;

        glyphs = new NamedGlyphSet(name, GlyphSet.FromText(characters));
        return true;
    }
}
