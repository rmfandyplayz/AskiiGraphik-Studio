// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Hosting;

/// <summary>
/// The folders where the app keeps per-user files.
/// </summary>
/// <param name="RootFolder">Where per-machine files go, such as logs.</param>
/// <param name="SettingsRootFolder">Where settings go. Null means the same folder as the logs.</param>
internal sealed record AppDataLocations(string RootFolder, string? SettingsRootFolder = null)
{
    // Users' existing logs and settings live under this name, so it must not change once the app
    // is released, even if the display name does.
    public const string FolderName = "AskiiGraphiks Studio";

    public string LogFolder => Path.Combine(RootFolder, "Logs");

    public string SettingsFile => Path.Combine(SettingsRootFolder ?? RootFolder, "settings.json");

    /// <summary>
    /// Logs go in the per-machine (non-roaming) app-data folder: %LOCALAPPDATA% on Windows,
    /// ~/Library/Application Support on macOS and $XDG_DATA_HOME (~/.local/share) on Linux. Settings
    /// go where ARCHITECTURE §15 says: %APPDATA% on Windows, ~/Library/Application Support on macOS
    /// and $XDG_CONFIG_HOME (~/.config) on Linux.
    /// </summary>
    public static AppDataLocations ForCurrentUser() => new(
        Path.Combine(ExistingFolder(Environment.SpecialFolder.LocalApplicationData), FolderName),
        Path.Combine(ExistingFolder(SettingsFolder), FolderName));

    /// <summary>
    /// On macOS, .NET's roaming folder is ~/.config, which Mac apps don't use, and its local one is
    /// the Application Support folder they do (ADR 0026).
    /// </summary>
    public static Environment.SpecialFolder SettingsFolder => OperatingSystem.IsMacOS()
        ? Environment.SpecialFolder.LocalApplicationData
        : Environment.SpecialFolder.ApplicationData;

    // GetFolderPath returns an empty string when the OS has no such folder (for example a Linux
    // account without a home directory). The temp folder still lets the app run.
    private static string ExistingFolder(Environment.SpecialFolder folder)
    {
        var path = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.Create);
        return string.IsNullOrEmpty(path) ? Path.GetTempPath() : path;
    }
}
