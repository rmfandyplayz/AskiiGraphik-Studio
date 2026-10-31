// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Hosting;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;

namespace AskiiArt.App.Tests;

public sealed class AppTests
{
    [AvaloniaFact]
    public void Initialize_SetsTheMacOSApplicationMenu()
    {
        // Avalonia's macOS menu exporter reads the application menu once, after Initialize and before
        // OnFrameworkInitializationCompleted, and never looks again. A menu set any later is ignored and
        // the user sees Avalonia's default "About Avalonia" menu instead.
        using var services = ServicesWithMenuLayout(MenuLayout.CreateDefault(isMacOS: true, includeDebugMenu: false));
        var app = new App(services);

        app.Initialize();

        var applicationMenu = NativeMenu.GetMenu(app);
        Assert.NotNull(applicationMenu);
        var about = Assert.IsType<NativeMenuItem>(Assert.Single(applicationMenu.Items));
        Assert.Equal(UiText.GetWithAppName(nameof(Strings.MenuAboutAppFormat)), about.Header);
    }

    [AvaloniaFact]
    public void Initialize_OnWindowsAndLinux_SetsNoApplicationMenu()
    {
        using var services = ServicesWithMenuLayout(MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false));
        var app = new App(services);

        app.Initialize();

        Assert.Null(NativeMenu.GetMenu(app));
    }

    private static ServiceProvider ServicesWithMenuLayout(MenuLayout layout)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IActionProvider>(
            new ShellActions(new FakeAppShell(), new AppDataLocations("root"), includeDebugActions: false));
        services.AddSingleton<ActionRegistry>();
        services.AddSingleton(layout);
        services.AddSingleton<MainWindowBuilder>();
        return services.BuildServiceProvider();
    }
}
