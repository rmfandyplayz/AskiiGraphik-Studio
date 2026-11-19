// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// How a ramp file and the saved user ramps write a ramp (ADR 0052): its name, what its lightest level paints, and
/// its characters as one string, lightest first, so the file reads like the ramp.
/// </summary>
internal static class RampJson
{
    private const string LightestKey = "lightest";
    private const string EmptyLightest = "empty";
    private const string SpaceLightest = "space";

    public static void Write(JsonObject json, string name, Ramp ramp)
    {
        json[LibraryJson.NameKey] = name;
        json[LightestKey] = ramp.ZeroLevel == RampZeroLevel.Space ? SpaceLightest : EmptyLightest;
        json[LibraryJson.CharactersKey] = ramp.ToString();
    }

    /// <summary>The ramp <paramref name="json"/> holds, or null when anything about it can't be read.</summary>
    public static bool TryRead(JsonObject json, [NotNullWhen(true)] out NamedRamp? ramp)
    {
        ramp = null;
        if (UserItemList<UserRamp>.Trimmed(LibraryJson.ReadString(json, LibraryJson.NameKey)) is not { } name)
            return false;

        var characters = LibraryJson.ReadString(json, LibraryJson.CharactersKey);
        if (characters is null || !Ramp.Check(characters).IsValid)
            return false;

        RampZeroLevel zeroLevel;
        switch (LibraryJson.ReadString(json, LightestKey))
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

        ramp = new NamedRamp(name, Ramp.FromText(characters, zeroLevel));
        return true;
    }
}
