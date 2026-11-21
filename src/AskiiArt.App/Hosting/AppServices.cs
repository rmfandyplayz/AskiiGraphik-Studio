// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Characters;
using AskiiArt.App.Documents;
using AskiiArt.App.Editing;
using AskiiArt.App.Errors;
using AskiiArt.App.Fonts;
using AskiiArt.App.Hosting.Logging;
using AskiiArt.App.Layers;
using AskiiArt.App.Painting;
using AskiiArt.App.Ramps;
using AskiiArt.App.Selection;
using AskiiArt.App.Settings;
using AskiiArt.App.Shell;
using AskiiArt.App.Shell.Docking;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Selecting;
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
        services.AddSingleton<IActionProvider, SelectionActions>();
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
            provider.GetRequiredService<RampsPanelViewModel>(),
            provider.GetRequiredService<ActiveDocument>(),
            provider.GetRequiredService<DocumentFiles>(),
            provider.GetRequiredService<FontCatalog>(),
            provider.GetRequiredService<GlyphDensityCache>(),
            includeDebugActions: BuildInfo.IsDebugBuild));
        services.AddSingleton<IActionProvider, CharacterActions>();
        services.AddSingleton<ActionRegistry>();

        // Tools register the way a future plugin would (ARCHITECTURE §8.1).
        services.AddSingleton(HandTool.Definition);
        services.AddSingleton(MoveTool.Definition);
        services.AddSingleton(RectangleSelectTool.Definition);
        services.AddSingleton(EllipseSelectTool.Definition);
        services.AddSingleton(LassoTool.Definition);
        services.AddSingleton(PolygonalLassoTool.Definition);
        services.AddSingleton(MagicWandTool.Definition);
        services.AddSingleton(TextTool.Definition);
        services.AddSingleton(BrushTool.Definition);
        services.AddSingleton(ColorBrushTool.ForegroundDefinition);
        services.AddSingleton(ColorBrushTool.BackgroundDefinition);
        services.AddSingleton(CharacterBrushTool.Definition);
        services.AddSingleton(EraserTool.Definition);
        services.AddSingleton(SoftEraserTool.Definition);
        services.AddSingleton(EraserTool.BackgroundDefinition);
        services.AddSingleton(BuiltInToolGroups.Hand);
        services.AddSingleton(BuiltInToolGroups.Move);
        services.AddSingleton(BuiltInToolGroups.Select);
        services.AddSingleton(BuiltInToolGroups.Text);
        services.AddSingleton(BuiltInToolGroups.Brush);
        services.AddSingleton(BuiltInToolGroups.Eraser);
        services.AddSingleton<ToolRegistry>();
        services.AddSingleton<PaintColors>();
        services.AddSingleton<PaintColorsViewModel>();
        services.AddSingleton(provider => RememberedRamps.CreateLibrary(provider.GetRequiredService<IAppSettings>()));
        services.AddSingleton(provider =>
            RememberedCharacters.CreateLibrary(provider.GetRequiredService<IAppSettings>()));
        services.AddSingleton(provider =>
            RememberedCharacters.CreateRecent(provider.GetRequiredService<IAppSettings>()));
        services.AddSingleton(provider => new ToolSettings(provider.GetRequiredService<RampLibrary>(),
            provider.GetRequiredService<RecentGlyphs>()));
        services.AddSingleton(provider =>
        {
            var docking = provider.GetRequiredService<ShellDocking>();
            return new ToolOptionsViewModel(provider.GetRequiredService<CanvasViewModel>(),
                () => docking.ShowWindow(ShellDockingIds.CharactersPanel));
        });
        services.AddSingleton<RememberedToolState>();
        services.AddSingleton<IRampFileDialogs, AvaloniaRampFileDialogs>();
        services.AddSingleton(provider =>
        {
            var activeDocument = provider.GetRequiredService<ActiveDocument>();
            var fonts = provider.GetRequiredService<FontCatalog>();
            var densities = provider.GetRequiredService<GlyphDensityCache>();
            return new RampsPanelViewModel(provider.GetRequiredService<RampLibrary>(),
                provider.GetRequiredService<IAppShell>(), provider.GetRequiredService<IRampFileDialogs>(),
                () => densities.GetMeter(fonts.Resolve(activeDocument.Session.Document.Font)));
        });

        services.AddSingleton<IGlyphSetFileDialogs, AvaloniaGlyphSetFileDialogs>();
        services.AddSingleton(provider =>
        {
            var canvas = provider.GetRequiredService<CanvasViewModel>();
            var densities = provider.GetRequiredService<GlyphDensityCache>();
            return new CharactersPanelViewModel(canvas, provider.GetRequiredService<GlyphSetLibrary>(),
                provider.GetRequiredService<IAppShell>(), provider.GetRequiredService<IGlyphSetFileDialogs>(),
                glyph => densities.GetMeter(canvas.Font).GetSource(glyph) != GlyphSource.Placeholder);
        });

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
