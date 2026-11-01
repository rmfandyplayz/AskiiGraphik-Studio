// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Errors;
using AskiiArt.App.Fonts;
using AskiiArt.App.Hosting.Logging;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
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
        services.AddSingleton<IActionProvider>(provider => new DocumentActions(
            provider.GetRequiredService<ActiveDocument>(),
            provider.GetRequiredService<DocumentFiles>(),
            provider.GetRequiredService<IDocumentDialogs>(),
            provider.GetRequiredService<IAppShell>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<ActionRegistry>();

        // Tools register the way a future plugin would (ARCHITECTURE §8.1).
        services.AddSingleton(TextTool.Definition);
        services.AddSingleton<ToolRegistry>();

        // The document being edited. The app starts with an empty one, as New would make it.
        services.AddSingleton(_ =>
            new ActiveDocument(new EditingSession(NewDocumentSettings.Default.CreateDocument())));
        services.AddSingleton<IDocumentDialogs, AvaloniaDocumentDialogs>();
        services.AddSingleton<DocumentFiles>();
        services.AddSingleton<WindowTitle>();

        // Fonts and glyphs.
        services.AddSingleton<FontCatalog>();
        services.AddSingleton<IFallbackFontProvider, SystemFallbackFontProvider>();
        services.AddSingleton<GlyphAtlasCache>();
        services.AddSingleton<CanvasViewModel>();

        services.AddSingleton(MenuLayout.CreateDefault(OperatingSystem.IsMacOS(), includeDebugMenu: BuildInfo.IsDebugBuild));
        services.AddSingleton(ToolbarLayout.CreateDefault());
        services.AddSingleton<MainWindowBuilder>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
