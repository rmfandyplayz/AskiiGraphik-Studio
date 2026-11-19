// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.App.Settings;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Formats;

namespace AskiiArt.App.Canvas;

/// <summary>
/// What the user last chose for the tools, kept in settings between runs (ADR 0052): every tool option, each tool
/// group's last tool, and the FG and BG colours. The app still starts on the Text tool. They're saved when the window
/// closes, as the layout is, and anything saved that's no use any more is ignored.
/// </summary>
internal sealed class RememberedToolState(IAppSettings settings)
{
    internal const string ToolOptionsKey = "toolOptions";
    internal const string LastToolsKey = "lastTools";
    internal const string ColorsKey = "colors";

    private const string ForegroundKey = "foreground";
    private const string BackgroundKey = "background";

    /// <summary>Gives <paramref name="canvas"/> what was saved last time.</summary>
    public void Restore(CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (settings.GetText(ToolOptionsKey) is { } toolOptions)
            canvas.ToolSettings.Restore(toolOptions);

        if (ReadObject(LastToolsKey) is { } lastTools)
            canvas.RestoreLastTools(ReadStrings(lastTools));

        if (ReadObject(ColorsKey) is { } colors)
            canvas.Colors.Set(ReadColor(colors, ForegroundKey), ReadColor(colors, BackgroundKey));
    }

    /// <summary>Saves what <paramref name="canvas"/> has now.</summary>
    public void Save(CanvasViewModel canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        settings.SetText(ToolOptionsKey, canvas.ToolSettings.ToJson());

        var lastTools = new JsonObject();
        foreach (var (groupId, toolId) in canvas.LastToolOfGroup)
            lastTools[groupId] = toolId;

        settings.SetText(LastToolsKey, lastTools.ToJsonString());

        var colors = new JsonObject
        {
            [ForegroundKey] = CellColorText.Write(canvas.Colors.Foreground),
            [BackgroundKey] = CellColorText.Write(canvas.Colors.Background),
        };
        settings.SetText(ColorsKey, colors.ToJsonString());
    }

    private JsonObject? ReadObject(string key)
    {
        if (settings.GetText(key) is not { } text)
            return null;

        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ReadStrings(JsonObject json)
    {
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, node) in json)
        {
            if (node is JsonValue value && value.TryGetValue(out string? text))
                strings[key] = text;
        }

        return strings;
    }

    // A colour that's missing or can't be read is none.
    private static CellColor ReadColor(JsonObject json, string key) =>
        json[key] is JsonValue value && value.TryGetValue(out string? text) && CellColorText.Parse(text) is { } color
            ? color
            : CellColor.Unset;
}
