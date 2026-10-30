// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Errors;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AskiiArt.App;

internal sealed partial class App : Application
{
    private readonly IServiceProvider? _services;

    /// <summary>Used by the IDE previewer and by tests, which bring their own windows.</summary>
    public App()
    {
    }

    /// <summary>Used by the real app (Program.Main).</summary>
    public App(IServiceProvider services)
    {
        _services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Name = Strings.AppDisplayName;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_services is not null && ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            StartDesktopShell(_services, desktop);

        base.OnFrameworkInitializationCompleted();
    }

    private void StartDesktopShell(IServiceProvider services, IClassicDesktopStyleApplicationLifetime desktop)
    {
        var errors = services.GetRequiredService<UnexpectedErrorHandler>();
        Dispatcher.UIThread.UnhandledException += (_, e) => e.Handled = errors.HandleUiThreadException(e.Exception);

        var commandModifiers = PlatformCommandModifiers();
        var windowBuilder = services.GetRequiredService<MainWindowBuilder>();

        if (windowBuilder.BuildApplicationMenu(commandModifiers) is { } applicationMenu)
            NativeMenu.SetMenu(this, applicationMenu);

        desktop.MainWindow = windowBuilder.Build(commandModifiers);
    }

    // Ctrl on Windows and Linux, Cmd on macOS, as Avalonia reports it for this platform.
    private KeyModifiers PlatformCommandModifiers()
    {
        if (PlatformSettings?.HotkeyConfiguration is { } hotkeys)
            return hotkeys.CommandModifiers;

        return OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
    }
}
