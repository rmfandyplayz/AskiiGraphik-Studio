// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;

namespace AskiiArt.App.Tests;

public sealed class AppTests : IDisposable
{
    private readonly TestCanvas _canvas = new();

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

    public void Dispose() => _canvas.Dispose();

    private ServiceProvider ServicesWithMenuLayout(MenuLayout layout)
    {
        var services = new ServiceCollection();
        var shell = new FakeAppShell();
        var dialogs = new FakeDocumentDialogs();
        foreach (var provider in TestActions.CreateProviders(_canvas, shell, dialogs))
            services.AddSingleton(provider);

        services.AddSingleton(_canvas.ViewModel);
        services.AddSingleton(new WindowTitle(_canvas.ActiveDocument));
        services.AddSingleton(_canvas.CreateDocumentFiles(dialogs, shell));
        services.AddSingleton<IAppShell>(shell);
        services.AddSingleton<ActionRegistry>();
        services.AddSingleton(layout);
        services.AddSingleton(ToolbarLayout.CreateDefault());
        services.AddSingleton<MainWindowBuilder>();
        return services.BuildServiceProvider();
    }
}
