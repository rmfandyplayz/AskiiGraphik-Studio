// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AskiiArt.Core.Formats;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Settings;

/// <summary>
/// The settings file: versioned JSON, written atomically (ARCHITECTURE §15, ADR 0026). Bad settings
/// never stop the app: a damaged file, or one from a newer version, is set aside next to the original,
/// the defaults apply, and <see cref="TakeDamageNotice"/> lets the app say so.
/// </summary>
internal sealed partial class AppSettingsFile(string path, ILogger<AppSettingsFile> logger) : IAppSettings
{
    public const int CurrentVersion = 1;

    private const string VersionKey = "version";

    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true, NewLine = "\n" };

    private JsonObject? _values;
    private bool _hasDamageNotice;

    public string? GetText(string key)
    {
        LoadOnce();
        return _values[key] is JsonValue value && value.TryGetValue(out string? text) ? text : null;
    }

    public void SetText(string key, string value)
    {
        LoadOnce();
        _values[key] = value;
        Save();
    }

    public bool TakeDamageNotice()
    {
        LoadOnce();
        var hadNotice = _hasDamageNotice;
        _hasDamageNotice = false;
        return hadNotice;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_values))]
    private void LoadOnce()
    {
        _values ??= Load();
    }

    private JsonObject Load()
    {
        string text;
        try
        {
            if (!File.Exists(path))
                return NewValues();

            text = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The file may be fine, so it isn't set aside. Saving later may fail too, and is logged.
            LogCouldNotRead(logger, e, path);
            return NewValues();
        }

        if (TryParse(text) is { } values)
            return values;

        SetAside();
        return NewValues();
    }

    private JsonObject? TryParse(string text)
    {
        try
        {
            if (JsonNode.Parse(text) is JsonObject values && HasReadableVersion(values))
                return values;

            LogUnreadableVersion(logger, path);
            return null;
        }
        catch (JsonException e)
        {
            LogDamaged(logger, e, path);
            return null;
        }
    }

    private static bool HasReadableVersion(JsonObject values) =>
        values[VersionKey] is JsonValue version && version.TryGetValue(out int number) &&
        number is >= 1 and <= CurrentVersion;

    private void SetAside()
    {
        _hasDamageNotice = true;
        try
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            File.Move(path, $"{path}.damaged-{stamp}", overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LogCouldNotSetAside(logger, e, path);
        }
    }

    private void Save()
    {
        try
        {
            if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } folder)
                Directory.CreateDirectory(folder);

            AtomicFile.Write(path, Encoding.UTF8.GetBytes(_values!.ToJsonString(_writeOptions)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LogCouldNotSave(logger, e, path);
        }
    }

    private static JsonObject NewValues() => new() { [VersionKey] = CurrentVersion };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't read the settings file {Path}. Using the defaults.")]
    private static partial void LogCouldNotRead(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The settings file {Path} isn't one this version can read.")]
    private static partial void LogUnreadableVersion(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The settings file {Path} is damaged.")]
    private static partial void LogDamaged(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't set aside the damaged settings file {Path}.")]
    private static partial void LogCouldNotSetAside(ILogger logger, Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Couldn't save the settings file {Path}.")]
    private static partial void LogCouldNotSave(ILogger logger, Exception exception, string path);
}
