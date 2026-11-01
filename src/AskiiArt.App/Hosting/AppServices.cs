// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Editing;
using AskiiArt.App.Errors;
using AskiiArt.App.Hosting.Logging;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AskiiArt.App.Hosting;

/// <summary>
/// The composition root: the one place that decides which implementation backs each service.
/// </summary>
internal static class AppServices
{
    public static ServiceProvider Create(AppDataLocations locations)
    {
        var services = new ServiceCollection();

        services.AddSingleton(locations);
        services.AddFileLogging(locations.LogFolder, BuildInfo.IsDebugBuild ? LogLevel.Debug : LogLevel.Information);

        services.AddSingleton<IAppShell, AvaloniaAppShell>();
        services.AddSingleton<UnexpectedErrorHandler>();

        services.AddSingleton<IActionProvider>(provider => new ShellActions(
            provider.GetRequiredService<IAppShell>(),
            locations,
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<IActionProvider, CanvasActions>();
        services.AddSingleton<IActionProvider, EditActions>();
        services.AddSingleton<ActionRegistry>();

        // Tools register the way a future plugin would (ARCHITECTURE §8.1).
        services.AddSingleton(TextTool.Definition);
        services.AddSingleton<ToolRegistry>();

        // The document being edited: a temporary sample until New and Open exist (steps 1.6 and 1.7).
        services.AddSingleton(_ => new EditingSession(SampleDocument.Create(Strings.DefaultLayerName)));

        // Fonts and glyphs.
        services.AddSingleton(_ => FontLoader.LoadBundled(BundledFonts.Default));
        services.AddSingleton<IFallbackFontProvider, SystemFallbackFontProvider>();
        services.AddSingleton<GlyphAtlasCache>();
        services.AddSingleton<CanvasViewModel>();

        services.AddSingleton(MenuLayout.CreateDefault(OperatingSystem.IsMacOS(), includeDebugMenu: BuildInfo.IsDebugBuild));
        services.AddSingleton<MainWindowBuilder>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
