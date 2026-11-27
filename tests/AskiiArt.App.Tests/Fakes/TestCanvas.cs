// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Characters;
using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Painting;
using AskiiArt.App.Ramps;
using AskiiArt.App.Shell;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.GlyphSets;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Curves;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Shapes;
using AskiiArt.Core.Tools.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using Microsoft.Extensions.Logging.Abstractions;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>
/// A canvas view model over a small empty document, with the built-in tools, the bundled fonts only
/// and no fallback fonts, so tests don't depend on what the machine has installed. Dispose it to
/// release the fonts and atlases.
/// </summary>
internal sealed class TestCanvas : IDisposable
{
    public TestCanvas(CanvasSize? size = null, CharacterSet characterSet = CharacterSet.Unicode,
        ColorMode colorMode = ColorMode.Monochrome)
    {
        Fonts = new FontCatalog(BundledFonts.All, getSystemFamilyNames: () => [], loadSystemFont: _ => null);
        Atlases = new GlyphAtlasCache(NoFallbackFonts.Instance);
        Densities = new GlyphDensityCache(NoFallbackFonts.Instance);
        ActiveDocument = new ActiveDocument(
            new EditingSession(Document.Create(size ?? new CanvasSize(40, 20), "Layer", characterSet,
                colorMode: colorMode)));
        var tools = new ToolRegistry(
            [
                HandTool.Definition,
                MoveTool.Definition,
                RectangleSelectTool.Definition,
                EllipseSelectTool.Definition,
                LassoTool.Definition,
                PolygonalLassoTool.Definition,
                MagicWandTool.Definition,
                TextTool.Definition,
                BrushTool.Definition,
                ColorBrushTool.ForegroundDefinition,
                ColorBrushTool.BackgroundDefinition,
                CharacterBrushTool.Definition,
                EraserTool.Definition,
                SoftEraserTool.Definition,
                EraserTool.BackgroundDefinition,
                LineTool.Definition,
                CurveTool.Definition,
                BoxShapeTool.RectangleDefinition,
                BoxShapeTool.EllipseDefinition
            ],
            BuiltInToolGroups.All);
        ViewModel = new CanvasViewModel(ActiveDocument, tools, Colors, ToolSettings, Fonts, Atlases, Densities);
    }

    /// <summary>The FG and BG colours the canvas's tools paint with.</summary>
    public PaintColors Colors { get; } = new();

    /// <summary>
    /// The canvas's tools' option values, such as the brush size, the ramps and the recent characters, made as the app
    /// makes them but with nothing saved.
    /// </summary>
    public ToolSettings ToolSettings { get; } =
        new(RememberedRamps.CreateLibrary(new FakeAppSettings()), new RecentGlyphs());

    /// <summary>The user's character sets, made as the app makes them but with nothing saved.</summary>
    public GlyphSetLibrary GlyphSets { get; } = RememberedCharacters.CreateLibrary(new FakeAppSettings());

    public ActiveDocument ActiveDocument { get; }

    public EditingSession Session => ActiveDocument.Session;

    public Document Document => Session.Document;

    public FontCatalog Fonts { get; }

    public LoadedFont Font => Fonts.DefaultFont;

    public GlyphAtlasCache Atlases { get; }

    public GlyphDensityCache Densities { get; }

    public CanvasViewModel ViewModel { get; }

    public TextExporter TextExporter { get; } = new();

    public AnsiExporter AnsiExporter { get; } = new();

    /// <summary>What the Colors toolbar and the Color and Swatches windows show, for this canvas's colours.</summary>
    public PaintColorsViewModel CreatePaintColorsViewModel() => new(Colors, ActiveDocument);

    /// <summary>What the Tool Options toolbar shows, for this canvas's chosen tool.</summary>
    public ToolOptionsViewModel CreateToolOptionsViewModel() => new(ViewModel);

    /// <summary>
    /// What the Ramps window shows, for this canvas's ramps, measuring glyphs in its document's font as the app does.
    /// </summary>
    public RampsPanelViewModel CreateRampsPanelViewModel(IAppShell shell, IRampFileDialogs? dialogs = null) =>
        new(ToolSettings.Ramps, shell, dialogs ?? new FakeRampFileDialogs(),
            () => Densities.GetMeter(Fonts.Resolve(Document.Font)));

    /// <summary>
    /// What the Characters window shows, for this canvas's character sets. Unless <paramref name="canDraw"/> says
    /// otherwise, a font can draw every character, so tests don't depend on the fonts.
    /// </summary>
    public CharactersPanelViewModel CreateCharactersPanelViewModel(IAppShell shell,
        IGlyphSetFileDialogs? dialogs = null, Func<Rune, bool>? canDraw = null) =>
        new(ViewModel, GlyphSets, shell, dialogs ?? new FakeGlyphSetFileDialogs(), canDraw ?? (_ => true));

    /// <summary>Saving and opening for this canvas's document, asking through fakes.</summary>
    public DocumentFiles CreateDocumentFiles(IDocumentDialogs dialogs, IAppShell shell) =>
        new(ActiveDocument, dialogs, shell, Fonts, TextExporter, AnsiExporter, NullLogger<DocumentFiles>.Instance);

    public void Dispose()
    {
        ViewModel.Dispose();
        Atlases.Dispose();
        Densities.Dispose();
        Fonts.Dispose();
    }

    private sealed class NoFallbackFonts : IFallbackFontProvider
    {
        public static NoFallbackFonts Instance { get; } = new();

        public LoadedFont? FindFontFor(Rune rune, LoadedFont primary) => null;
    }
}
