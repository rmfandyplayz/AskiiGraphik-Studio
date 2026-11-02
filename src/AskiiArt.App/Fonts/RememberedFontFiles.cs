// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.App.Settings;

namespace AskiiArt.App.Fonts;

/// <summary>The paths of the font files the user has loaded, kept in settings between runs (ADR 0034).</summary>
internal sealed class RememberedFontFiles(IAppSettings settings)
{
    internal const string SettingKey = "fontFiles";

    /// <summary>The remembered paths, oldest first. A saved value that can't be read counts as none.</summary>
    public IReadOnlyList<string> Load()
    {
        if (settings.GetText(SettingKey) is not { } saved)
            return [];

        try
        {
            if (JsonNode.Parse(saved) is not JsonArray paths)
                return [];

            return
            [
                .. paths.OfType<JsonValue>()
                    .Where(value => value.TryGetValue<string>(out _))
                    .Select(value => value.GetValue<string>())
            ];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Add(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var fullPath = Path.GetFullPath(path);
        var paths = Load();
        if (paths.Contains(fullPath, StringComparer.Ordinal))
            return;

        var updated = new JsonArray();
        foreach (var remembered in paths.Append(fullPath))
            updated.Add(remembered);

        settings.SetText(SettingKey, updated.ToJsonString());
    }
}
