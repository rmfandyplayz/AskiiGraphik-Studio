// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Errors;
using AskiiArt.App.Fonts;
using AskiiArt.App.Hosting.Logging;
using AskiiArt.App.Layers;
using AskiiArt.App.Painting;
using AskiiArt.App.Ramps;
using AskiiArt.App.Settings;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Options;
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

        services.AddSingleton<IAppSettings>(provider =>
            new AppSettingsFile(locations.SettingsFile, provider.GetRequiredService<ILogger<AppSettingsFile>>()));

        services.AddSingleton<IAppShell, AvaloniaAppShell>();
        services.AddSingleton<UnexpectedErrorHandler>();
        services.AddSingleton<ShellDocking>();

        services.AddSingleton<IActionProvider>(provider => new ShellActions(
            provider.GetRequiredService<IAppShell>(),
            locations,
            includeDebugActions: BuildInfo.IsDebugBuild,
            isMacOS: OperatingSystem.IsMacOS()));
        services.AddSingleton<IActionProvider>(provider => new CanvasActions(
            provider.GetRequiredService<CanvasViewModel>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<IActionProvider, ToolActions>();
        services.AddSingleton<IActionProvider, EditActions>();
        services.AddSingleton<IActionProvider, WindowMenuActions>();
        services.AddSingleton<IActionProvider, ToolbarMenuActions>();
        services.AddSingleton<IActionProvider, WindowFrameActions>();
        services.AddSingleton<IActionProvider>(provider => new WindowSampleActions(
            provider.GetRequiredService<ShellDocking>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<IActionProvider, LayerActions>();
        services.AddSingleton<IActionProvider, ColorModeActions>();
        services.AddSingleton<IActionProvider>(provider => new DocumentActions(
            provider.GetRequiredService<ActiveDocument>(),
            provider.GetRequiredService<DocumentFiles>(),
            provider.GetRequiredService<IDocumentDialogs>(),
            provider.GetRequiredService<IAppShell>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<IActionProvider>(provider => new RampActions(
            provider.GetRequiredService<ActiveDocument>(),
            provider.GetRequiredService<DocumentFiles>(),
            provider.GetRequiredService<FontCatalog>(),
            provider.GetRequiredService<GlyphDensityCache>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<ActionRegistry>();

        // Tools register the way a future plugin would (ARCHITECTURE §8.1).
        services.AddSingleton(HandTool.Definition);
        services.AddSingleton(TextTool.Definition);
        services.AddSingleton(BrushTool.Definition);
        services.AddSingleton(ColorBrushTool.ForegroundDefinition);
        services.AddSingleton(ColorBrushTool.BackgroundDefinition);
        services.AddSingleton(EraserTool.Definition);
        services.AddSingleton(BuiltInToolGroups.Hand);
        services.AddSingleton(BuiltInToolGroups.Text);
        services.AddSingleton(BuiltInToolGroups.Brush);
        services.AddSingleton(BuiltInToolGroups.Eraser);
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton<PaintColors>();
        services.AddSingleton<PaintColorsViewModel>();
        services.AddSingleton<ToolSettings>();
        services.AddSingleton<ToolOptionsViewModel>();

        // The document being edited. The app starts with an empty one, as New would make it.
        services.AddSingleton(_ =>
            new ActiveDocument(new EditingSession(NewDocumentSettings.Default.CreateDocument())));
        services.AddSingleton<IDocumentDialogs, AvaloniaDocumentDialogs>();
        services.AddSingleton<DocumentFiles>();
        services.AddSingleton<WindowTitle>();
        services.AddSingleton<HistoryPanelViewModel>();
        services.AddSingleton<LayersPanelViewModel>();

        // Export formats register the way a future plugin's would (ARCHITECTURE §16, §18). They
        // flatten with the same policy as the canvas.
        services.AddSingleton<TextExporter>();
        services.AddSingleton<IExporter>(provider => provider.GetRequiredService<TextExporter>());
        services.AddSingleton<AnsiExporter>();
        services.AddSingleton<IExporter>(provider => provider.GetRequiredService<AnsiExporter>());

        // Fonts and glyphs.
        services.AddSingleton<FontCatalog>();
        services.AddSingleton<IFallbackFontProvider, SystemFallbackFontProvider>();
        services.AddSingleton<GlyphAtlasCache>();
        services.AddSingleton<GlyphDensityCache>();
        services.AddSingleton<CanvasViewModel>();

        services.AddSingleton(MenuLayout.CreateDefault(OperatingSystem.IsMacOS(),
            includeDebugMenu: BuildInfo.IsDebugBuild));
        services.AddSingleton(ShellToolbars.CreateDefault());
        services.AddSingleton<MainWindowBuilder>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
