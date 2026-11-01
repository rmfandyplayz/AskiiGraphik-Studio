// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Documents;
using AskiiArt.App.Errors;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
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

        // It has to be here: Avalonia's macOS menu exporter reads the application menu once, right after
        // Initialize and before OnFrameworkInitializationCompleted, and ignores any menu set later.
        if (_services is not null)
            SetApplicationMenu(_services);
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

        // Mac apps keep running with no window open (ADR 0023).
        var keepsRunningWithoutWindow = OperatingSystem.IsMacOS();
        if (keepsRunningWithoutWindow)
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var windows = new MainWindowLifetime(services.GetRequiredService<MainWindowBuilder>(),
            services.GetRequiredService<ActiveDocument>(), PlatformCommandModifiers(), keepsRunningWithoutWindow);
        windows.WindowChanged += (_, _) => desktop.MainWindow = windows.Window;
        if (keepsRunningWithoutWindow)
            OpenWindowsFromTheDock(services, windows);

        windows.ShowWindow();
    }

    // Clicking the Dock icon opens a window when none is open, and the Dock menu's items open one
    // first; the menu bar can't offer them with no window (ADR 0023).
    private void OpenWindowsFromTheDock(IServiceProvider services, MainWindowLifetime windows)
    {
        if (this.TryGetFeature<IActivatableLifetime>() is { } activatable)
        {
            activatable.Activated += (_, e) =>
            {
                if (e.Kind == ActivationKind.Reopen)
                    windows.ShowWindow();
            };
        }

        var layout = services.GetRequiredService<MenuLayout>();
        var menus = new NativeMenuBuilder(services.GetRequiredService<ActionRegistry>(), PlatformCommandModifiers());
        NativeDock.SetMenu(this, menus.BuildDockMenu(layout.DockMenu, windows.RunInWindow));
    }

    private void SetApplicationMenu(IServiceProvider services)
    {
        var windowBuilder = services.GetRequiredService<MainWindowBuilder>();

        if (windowBuilder.BuildApplicationMenu(PlatformCommandModifiers()) is { } applicationMenu)
            NativeMenu.SetMenu(this, applicationMenu);
    }

    // Ctrl on Windows and Linux, Cmd on macOS, as Avalonia reports it for this platform.
    private KeyModifiers PlatformCommandModifiers()
    {
        if (PlatformSettings?.HotkeyConfiguration is { } hotkeys)
            return hotkeys.CommandModifiers;

        return OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
    }
}
