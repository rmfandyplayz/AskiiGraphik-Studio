// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Hosting;

namespace AskiiArt.App.Tests.Hosting;

public sealed class AppDataLocationsTests
{
    [Fact]
    public void LogFolder_IsInsideTheRootFolder()
    {
        var locations = new AppDataLocations(Path.Combine("some", "root"));

        Assert.Equal(Path.Combine("some", "root", "Logs"), locations.LogFolder);
    }

    [Fact]
    public void SettingsFile_IsInTheSettingsFolder_OrTheRootFolderWithoutOne()
    {
        Assert.Equal(Path.Combine("root", "settings.json"), new AppDataLocations("root").SettingsFile);
        Assert.Equal(Path.Combine("config", "settings.json"), new AppDataLocations("root", "config").SettingsFile);
    }

    [Fact]
    public void ForCurrentUser_PutsSettingsWhereArchitecture15Says()
    {
        var folder = Environment.GetFolderPath(AppDataLocations.SettingsFolder, Environment.SpecialFolderOption.Create);

        Assert.Equal(Path.Combine(folder, AppDataLocations.FolderName, "settings.json"),
            AppDataLocations.ForCurrentUser().SettingsFile);
    }

    [Fact]
    public void SettingsFolder_OnMacOS_IsApplicationSupport_NotDotConfig()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        Assert.EndsWith(Path.Combine("Library", "Application Support"),
            Environment.GetFolderPath(AppDataLocations.SettingsFolder), StringComparison.Ordinal);
    }

    [Fact]
    public void ForCurrentUser_IsAnAbsolutePathEndingInTheAppFolderName()
    {
        var locations = AppDataLocations.ForCurrentUser();

        Assert.True(Path.IsPathFullyQualified(locations.RootFolder));
        Assert.Equal(AppDataLocations.FolderName, Path.GetFileName(locations.RootFolder));
    }

    [Fact]
    public void ForCurrentUser_UsesTheLocalNotTheRoamingAppDataFolder()
    {
        // Create, like the app: without it, a fresh Linux CI runner can return "" for a missing folder.
        var localAppData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);

        Assert.Equal(Path.Combine(localAppData, AppDataLocations.FolderName),
            AppDataLocations.ForCurrentUser().RootFolder);
    }
}
