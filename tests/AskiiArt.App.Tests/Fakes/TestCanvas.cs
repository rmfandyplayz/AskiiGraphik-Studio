// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;

namespace AskiiArt.App.Tests.Fakes;

/// <summary>
/// A canvas view model over a small empty document, with the text tool, the default bundled font
/// and no fallback fonts, so tests don't depend on what the machine has installed. Dispose it to
/// release the font and atlases.
/// </summary>
internal sealed class TestCanvas : IDisposable
{
    public TestCanvas(CanvasSize? size = null, CharacterSet characterSet = CharacterSet.Unicode)
    {
        Font = FontLoader.LoadBundled(BundledFonts.Default);
        Atlases = new GlyphAtlasCache(NoFallbackFonts.Instance);
        Session = new EditingSession(Document.Create(size ?? new CanvasSize(40, 20), "Layer", characterSet));
        ViewModel = new CanvasViewModel(Session, new ToolRegistry([TextTool.Definition]), Font, Atlases);
    }

    public EditingSession Session { get; }

    public Document Document => Session.Document;

    public LoadedFont Font { get; }

    public GlyphAtlasCache Atlases { get; }

    public CanvasViewModel ViewModel { get; }

    public void Dispose()
    {
        ViewModel.Dispose();
        Atlases.Dispose();
        Font.Dispose();
    }

    private sealed class NoFallbackFonts : IFallbackFontProvider
    {
        public static NoFallbackFonts Instance { get; } = new();

        public LoadedFont? FindFontFor(Rune rune, LoadedFont primary) => null;
    }
}
