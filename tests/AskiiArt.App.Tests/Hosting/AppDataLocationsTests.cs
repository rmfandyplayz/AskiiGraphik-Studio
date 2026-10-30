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
