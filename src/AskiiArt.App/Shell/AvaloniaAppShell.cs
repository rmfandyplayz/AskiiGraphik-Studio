// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Hosting;
using AskiiArt.App.Shell.Dialogs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Shell;

/// <summary>
/// <see cref="IAppShell"/> for the real desktop app, built on Avalonia's application lifetime.
/// </summary>
internal sealed partial class AvaloniaAppShell(AppDataLocations locations, ILogger<AvaloniaAppShell> logger) : IAppShell
{
    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public bool CanShowDialogs => MainWindow is { IsVisible: true };

    public void RequestShutdown()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.TryShutdown();
    }

    public async Task OpenFolderAsync(string folderPath)
    {
        if (MainWindow is not { } window)
            return;

        // The folder may not exist yet (for example a fresh install before anything was logged).
        Directory.CreateDirectory(folderPath);

        var opened = await window.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(folderPath));
        if (!opened)
            LogCouldNotOpenFolder(folderPath);
    }

    public async Task ShowAboutAsync()
    {
        if (MainWindow is not { } owner)
            return;

        var dialog = new AboutWindow { DataContext = new AboutViewModel() };
        await dialog.ShowDialog(owner);
    }

    public async Task<UnexpectedErrorChoice> ShowUnexpectedErrorAsync()
    {
        if (MainWindow is not { } owner)
            return UnexpectedErrorChoice.Continue;

        var openLogFolder = new AsyncRelayCommand(() => OpenFolderAsync(locations.LogFolder));
        var dialog = new UnexpectedErrorWindow { DataContext = new UnexpectedErrorViewModel(openLogFolder) };
        return await dialog.ShowDialog<UnexpectedErrorChoice>(owner);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The OS could not open the folder {FolderPath}")]
    private partial void LogCouldNotOpenFolder(string folderPath);
}
