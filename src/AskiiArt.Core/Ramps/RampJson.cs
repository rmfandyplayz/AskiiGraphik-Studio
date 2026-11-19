// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// How a ramp file and the saved user ramps write a ramp (ADR 0052): its name, what its lightest level paints, and
/// its characters as one string, lightest first, so the file reads like the ramp.
/// </summary>
internal static class RampJson
{
    public const string NameKey = "name";
    public const string LightestKey = "lightest";
    public const string CharactersKey = "characters";

    private const string EmptyLightest = "empty";
    private const string SpaceLightest = "space";

    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",

        // Keeps block, braille and other non-ASCII characters readable instead of \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Write(JsonObject json, string name, Ramp ramp)
    {
        json[NameKey] = name;
        json[LightestKey] = ramp.ZeroLevel == RampZeroLevel.Space ? SpaceLightest : EmptyLightest;
        json[CharactersKey] = ramp.ToString();
    }

    /// <summary>The ramp <paramref name="json"/> holds, or null when anything about it can't be read.</summary>
    public static bool TryRead(JsonObject json, [NotNullWhen(true)] out NamedRamp? ramp)
    {
        ramp = null;
        if (ReadString(json, NameKey) is not { } name || name.Trim().Length == 0)
            return false;

        if (ReadString(json, CharactersKey) is not { } characters || !Ramp.Check(characters).IsValid)
            return false;

        RampZeroLevel zeroLevel;
        switch (ReadString(json, LightestKey))
        {
            case EmptyLightest:
                zeroLevel = RampZeroLevel.Empty;
                break;
            case SpaceLightest:
                zeroLevel = RampZeroLevel.Space;
                break;
            default:
                return false;
        }

        ramp = new NamedRamp(name.Trim(), Ramp.FromText(characters, zeroLevel));
        return true;
    }

    public static string? ReadString(JsonObject json, string key) =>
        json[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;
}
