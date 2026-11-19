// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AskiiArt.Core.Libraries;

/// <summary>
/// How ramp files, character set files and the saved user items are written (ADRs 0052, 0053): readable JSON with
/// <c>\n</c> line endings. A file starts by naming its format and version, so a newer one can be refused, as documents
/// are.
/// </summary>
internal static class LibraryJson
{
    public const string NameKey = "name";
    public const string CharactersKey = "characters";

    private const string FormatKey = "format";
    private const string VersionKey = "version";

    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        NewLine = "\n",

        // Keeps block, braille and other non-ASCII characters readable instead of \u escapes.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string? ReadString(JsonObject json, string key) =>
        json[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    /// <summary>The text of a file of <paramref name="format"/>, holding what <paramref name="writeBody"/> writes.
    /// </summary>
    public static string WriteFile(string format, int version, Action<JsonObject> writeBody)
    {
        var json = new JsonObject { [FormatKey] = format, [VersionKey] = version };
        writeBody(json);
        return json.ToJsonString(WriteOptions) + "\n";
    }

    /// <summary>
    /// Reads <paramref name="text"/> as a file of <paramref name="format"/>, up to <paramref name="newestVersion"/>,
    /// and gives its JSON to read the rest of, or says why it can't be read.
    /// </summary>
    public static LibraryFileProblem ReadFile(string text, string format, int newestVersion, out JsonObject json)
    {
        ArgumentNullException.ThrowIfNull(text);

        json = [];
        try
        {
            if (JsonNode.Parse(text) is not JsonObject parsed)
                return LibraryFileProblem.WrongKind;

            json = parsed;
        }
        catch (JsonException)
        {
            return LibraryFileProblem.WrongKind;
        }

        if (ReadString(json, FormatKey) != format)
            return LibraryFileProblem.WrongKind;

        if (json[VersionKey] is not JsonValue version || !version.TryGetValue(out int number) || number < 1)
            return LibraryFileProblem.Damaged;

        return number > newestVersion ? LibraryFileProblem.NewerVersion : LibraryFileProblem.None;
    }
}
