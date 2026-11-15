// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Painting;
using AskiiArt.App.Shell;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Exporting;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
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
        ActiveDocument = new ActiveDocument(
            new EditingSession(Document.Create(size ?? new CanvasSize(40, 20), "Layer", characterSet,
                colorMode: colorMode)));
        var tools = new ToolRegistry(
            [HandTool.Definition, TextTool.Definition, BrushTool.Definition, EraserTool.Definition]);
        ViewModel = new CanvasViewModel(ActiveDocument, tools, Colors, Fonts, Atlases);
    }

    /// <summary>The FG and BG colours the canvas's tools paint with.</summary>
    public PaintColors Colors { get; } = new();

    public ActiveDocument ActiveDocument { get; }

    public EditingSession Session => ActiveDocument.Session;

    public Document Document => Session.Document;

    public FontCatalog Fonts { get; }

    public LoadedFont Font => Fonts.DefaultFont;

    public GlyphAtlasCache Atlases { get; }

    public CanvasViewModel ViewModel { get; }

    public TextExporter TextExporter { get; } = new();

    /// <summary>What the Brush Options, Color and Swatches windows show, for this canvas's colours.</summary>
    public PaintColorsViewModel CreatePaintColorsViewModel() => new(Colors, ActiveDocument);

    /// <summary>Saving and opening for this canvas's document, asking through fakes.</summary>
    public DocumentFiles CreateDocumentFiles(IDocumentDialogs dialogs, IAppShell shell) =>
        new(ActiveDocument, dialogs, shell, Fonts, TextExporter, NullLogger<DocumentFiles>.Instance);

    public void Dispose()
    {
        ViewModel.Dispose();
        Atlases.Dispose();
        Fonts.Dispose();
    }

    private sealed class NoFallbackFonts : IFallbackFontProvider
    {
        public static NoFallbackFonts Instance { get; } = new();

        public LoadedFont? FindFontFor(Rune rune, LoadedFont primary) => null;
    }
}
