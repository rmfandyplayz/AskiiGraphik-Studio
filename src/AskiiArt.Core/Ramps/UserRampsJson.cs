// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// The user's ramps as the settings save them (ADR 0052): a JSON array, oldest first, of each ramp's id and what a
/// ramp file holds.
/// </summary>
public static class UserRampsJson
{
    private const string IdKey = "id";

    /// <summary>The JSON text of <paramref name="ramps"/>.</summary>
    public static string Write(IEnumerable<UserRamp> ramps)
    {
        ArgumentNullException.ThrowIfNull(ramps);

        var json = new JsonArray();
        foreach (var ramp in ramps)
        {
            var entry = new JsonObject { [IdKey] = ramp.Id };
            RampJson.Write(entry, ramp.Name, ramp.Ramp);
            json.Add(entry);
        }

        return json.ToJsonString(LibraryJson.WriteOptions);
    }

    /// <summary>
    /// The ramps in <paramref name="text"/>, made by <see cref="Write"/>. A ramp that can't be read is left out, so
    /// one damaged entry doesn't cost the rest, and text that isn't an array holds none.
    /// </summary>
    public static IReadOnlyList<UserRamp> Read(string text)
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

        var ramps = new List<UserRamp>();
        foreach (var entry in json.OfType<JsonObject>())
        {
            if (LibraryJson.ReadString(entry, IdKey) is { } id && RampJson.TryRead(entry, out var ramp))
                ramps.Add(new UserRamp(id, ramp.Name, ramp.Ramp));
        }

        return ramps;
    }
}
