// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Settings;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AskiiArt.App.Shell.Placement;

/// <summary>
/// Opens the main window where it was last time, on the same screen, at the same size, and maximized or full screen
/// if it was (ADR 0059). It's saved when the window closes, as the layout is. If the screen it was on has gone, it
/// opens in the middle of the main screen instead.
/// </summary>
internal sealed class RememberedWindowPlacement
{
    internal const string SettingKey = "mainWindow";

    private readonly Window _window;
    private readonly IAppSettings _settings;

    // Where the window last was while neither maximized nor minimized, so that a maximized window comes back to it
    // when restored.
    private PixelPoint _position;
    private Size _size;
    private bool _isMaximized;
    private bool _isFullScreen;
    private bool _isKnown;
    private bool _isNoteDue;

    private RememberedWindowPlacement(Window window, IAppSettings settings)
    {
        _window = window;
        _settings = settings;
    }

    /// <summary>
    /// Places <paramref name="window"/> as it was saved last time, and saves where it is when it closes. Call it before
    /// the window shows.
    /// </summary>
    public static void Follow(Window window, IAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(settings);

        var placement = new RememberedWindowPlacement(window, settings);
        placement.Restore();
        placement.Track();
    }

    private void Restore()
    {
        if (_settings.GetText(SettingKey) is not { } text || WindowPlacement.FromJson(text) is not { } saved)
            return;

        var screens = _window.Screens.All
            .Select(screen => new ScreenArea(screen.WorkingArea, screen.Scaling, screen.IsPrimary))
            .ToList();

        Size size;
        if (saved.OnScreens(screens) is { } placed)
        {
            _window.WindowStartupLocation = WindowStartupLocation.Manual;
            _window.Position = placed.Position;
            size = placed.Size;
        }
        else
        {
            _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var primary = screens.FirstOrDefault(screen => screen.IsPrimary);
            size = screens.Count > 0 ? saved.SizeWithin(primary) : saved.Size;
        }

        _window.Width = size.Width;
        _window.Height = size.Height;
        if (saved.IsFullScreen)
            _window.WindowState = WindowState.FullScreen;
        else if (saved.IsMaximized)
            _window.WindowState = WindowState.Maximized;

        _position = _window.Position;
        _size = size;
        _isMaximized = saved.IsMaximized;
        _isFullScreen = saved.IsFullScreen;
        _isKnown = true;
    }

    private void Track()
    {
        _window.Opened += (_, _) => NoteLater();
        _window.PositionChanged += (_, _) => NoteLater();
        _window.PropertyChanged += (_, change) =>
        {
            if (change.Property == TopLevel.ClientSizeProperty || change.Property == Window.WindowStateProperty)
                NoteLater();
        };
        _window.Closed += (_, _) => Save();
    }

    // Later, because a window that's being maximized can report its new position before it reports being maximized.
    private void NoteLater()
    {
        if (_isNoteDue)
            return;

        _isNoteDue = true;
        Dispatcher.UIThread.Post(Note, DispatcherPriority.Background);
    }

    private void Note()
    {
        _isNoteDue = false;
        if (_window.PlatformImpl is null)
            return;

        // Minimized, it keeps what was noted before, so it comes back as it was then. Full screen, it also remembers
        // whether it was maximized before, which is how it comes back out of full screen.
        switch (_window.WindowState)
        {
            case WindowState.FullScreen:
                _isFullScreen = true;
                break;
            case WindowState.Maximized:
                _isMaximized = true;
                _isFullScreen = false;
                break;
            case WindowState.Normal:
                _position = _window.Position;
                _size = _window.ClientSize;
                _isMaximized = false;
                _isFullScreen = false;
                _isKnown = true;
                break;
        }
    }

    private void Save()
    {
        if (_isKnown)
            _settings.SetText(SettingKey, new WindowPlacement(_position, _size, _isMaximized, _isFullScreen).ToJson());
    }
}
