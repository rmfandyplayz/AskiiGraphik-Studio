// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AskiiArt.Core.Ramps;

/// <summary>What's wrong with a file that was read as a ramp file.</summary>
public enum RampFileProblem
{
    /// <summary>Nothing: it was read.</summary>
    None,

    /// <summary>It isn't a ramp file at all.</summary>
    NotARampFile,

    /// <summary>It's a ramp file from a newer version of the app, which this one can't read.</summary>
    NewerVersion,

    /// <summary>It's a ramp file, but something in it can't be read.</summary>
    Damaged
}

/// <summary>
/// A file holding one ramp, to share it (ARCHITECTURE §7, ADR 0052): readable JSON naming the format and its version,
/// then the ramp's name, what its lightest level paints, and its characters, lightest first.
/// </summary>
public static class RampFile
{
    /// <summary>The extension ramp files have. It may change with the product's name, so it's kept here.</summary>
    public const string Extension = ".asciiramp";

    /// <summary>The newest format version, which this app writes.</summary>
    public const int CurrentVersion = 1;

    private const string FormatKey = "format";
    private const string FormatName = "askiiart.ramp";
    private const string VersionKey = "version";

    /// <summary>The text of a ramp file holding <paramref name="ramp"/>, named <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public static string Write(string name, Ramp ramp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(ramp);

        var json = new JsonObject { [FormatKey] = FormatName, [VersionKey] = CurrentVersion };
        RampJson.Write(json, name.Trim(), ramp);
        return json.ToJsonString(RampJson.WriteOptions) + "\n";
    }

    /// <summary>Reads the ramp in <paramref name="text"/>, a ramp file's contents.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="ramp">The ramp and its name, when it could be read.</param>
    /// <param name="problem"><see cref="RampFileProblem.None"/>, or why it couldn't be read.</param>
    /// <returns>Whether it could be read.</returns>
    public static bool TryRead(string text, [NotNullWhen(true)] out NamedRamp? ramp, out RampFileProblem problem)
    {
        problem = Read(text, out ramp);
        return ramp is not null;
    }

    private static RampFileProblem Read(string text, out NamedRamp? ramp)
    {
        ArgumentNullException.ThrowIfNull(text);

        ramp = null;
        JsonObject? json;
        try
        {
            json = JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return RampFileProblem.NotARampFile;
        }

        if (json is null || RampJson.ReadString(json, FormatKey) != FormatName)
            return RampFileProblem.NotARampFile;

        if (json[VersionKey] is not JsonValue version || !version.TryGetValue(out int number) || number < 1)
            return RampFileProblem.Damaged;

        if (number > CurrentVersion)
            return RampFileProblem.NewerVersion;

        return RampJson.TryRead(json, out ramp) ? RampFileProblem.None : RampFileProblem.Damaged;
    }
}
