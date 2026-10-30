// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Hosting;

/// <summary>
/// The folders where the app keeps per-user files.
/// </summary>
internal sealed record AppDataLocations(string RootFolder)
{
    // Users' existing logs (and later settings) live under this name, so it must not change once
    // the app is released, even if the display name does.
    public const string FolderName = "AskiiGraphiks Studio";

    public string LogFolder => Path.Combine(RootFolder, "Logs");

    /// <summary>
    /// The per-machine (non-roaming) app-data folder: %LOCALAPPDATA% on Windows,
    /// ~/Library/Application Support on macOS and $XDG_DATA_HOME (~/.local/share) on Linux.
    /// </summary>
    public static AppDataLocations ForCurrentUser()
    {
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        // GetFolderPath returns an empty string when the OS has no such folder (for example a
        // Linux account without a home directory). The temp folder still lets the app log.
        if (string.IsNullOrEmpty(localAppData))
            localAppData = Path.GetTempPath();

        return new AppDataLocations(Path.Combine(localAppData, FolderName));
    }
}
