// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AskiiArt.App.Documents;
using AskiiArt.App.Fonts;
using AskiiArt.App.Resources;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Text;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's view of the active document: what it draws (the composite, font and atlases), how
/// (zoom, grid, the cell under the pointer), and the tool that edits it. Zoom and grid are view
/// settings, not document changes, so they don't go through commands or undo. When New replaces
/// the document, all of these switch to the new one.
/// </summary>
internal sealed class CanvasViewModel : ObservableObject, IDisposable
{
    /// <summary>The font size at 100% zoom, in logical pixels. It's the same for every document (ADR 0019).</summary>
    public const double DefaultFontSize = 16;

    private readonly ActiveDocument _activeDocument;
    private readonly ToolRegistry _tools;
    private readonly FontCatalog _fonts;

    public CanvasViewModel(ActiveDocument activeDocument, ToolRegistry tools, FontCatalog fonts, GlyphAtlasCache atlases)
    {
        ArgumentNullException.ThrowIfNull(activeDocument);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(fonts);
        ArgumentNullException.ThrowIfNull(atlases);

        _activeDocument = activeDocument;
        _tools = tools;
        _fonts = fonts;
        Atlases = atlases;

        Attach(activeDocument.Session);
        activeDocument.SessionChanged += OnSessionChanged;
    }

    public EditingSession Session { get; private set; }

    public ITool ActiveTool { get; private set; }

    public CompositeCache Composite { get; private set; }

    /// <summary>The document's font, or the default font when it isn't available.</summary>
    public LoadedFont Font { get; private set; }

    public GlyphAtlasCache Atlases { get; }

    public double FontSize { get; } = DefaultFontSize;

    /// <summary>The zoom, where 1 is 100%. Kept between <see cref="ZoomLevels.Minimum"/> and <see cref="ZoomLevels.Maximum"/>.</summary>
    public double ZoomFactor
    {
        get;
        set
        {
            if (SetProperty(ref field, Math.Clamp(value, ZoomLevels.Minimum, ZoomLevels.Maximum)))
                OnPropertyChanged(nameof(ZoomText));
        }
    } = ZoomLevels.ActualSize;

    public bool IsGridVisible
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>The cell under the pointer, reported by the canvas control.</summary>
    public CellPoint? HoveredCell
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HoveredCellText));
        }
    }

    public bool CanZoomIn => ZoomLevels.StepIn(ZoomFactor) > ZoomFactor;

    public bool CanZoomOut => ZoomLevels.StepOut(ZoomFactor) < ZoomFactor;

    public string ZoomText => string.Format(CultureInfo.CurrentCulture, Strings.StatusZoomFormat, ZoomFactor * 100);

    public string HoveredCellText
    {
        get
        {
            if (HoveredCell is not { } cell)
                return string.Empty;

            return string.Format(CultureInfo.CurrentCulture, Strings.StatusCellFormat, cell.X, cell.Y);
        }
    }

    public string CanvasSizeText => string.Format(CultureInfo.CurrentCulture, Strings.StatusCanvasSizeFormat,
        Composite.Size.Width, Composite.Size.Height);

    public string FontText => Font.FamilyName;

    public string CharacterSetText => Session.Document.CharacterSet switch
    {
        CharacterSet.Ascii => Strings.StatusCharacterSetAscii,
        _ => Strings.StatusCharacterSetUnicode
    };

    public void ZoomIn() => ZoomFactor = ZoomLevels.StepIn(ZoomFactor);

    public void ZoomOut() => ZoomFactor = ZoomLevels.StepOut(ZoomFactor);

    public void ZoomToActualSize() => ZoomFactor = ZoomLevels.ActualSize;

    public void ToggleGrid() => IsGridVisible = !IsGridVisible;

    public void Dispose()
    {
        _activeDocument.SessionChanged -= OnSessionChanged;
        Detach();
    }

    // The text tool is the only tool until M3, so it's always active.
    [MemberNotNull(nameof(Session), nameof(Composite), nameof(ActiveTool), nameof(Font))]
    private void Attach(EditingSession session)
    {
        Session = session;
        Composite = new CompositeCache(session.Document, CompositingPolicy.Default);
        Composite.Changed += OnCompositeChanged;
        session.Document.Changed += OnDocumentChanged;
        Font = _fonts.Resolve(session.Document.Font);
        ActiveTool = _tools.Get(TextTool.Id).Create(session);
    }

    private void Detach()
    {
        ActiveTool.Deactivate();
        Session.Document.Changed -= OnDocumentChanged;
        Composite.Changed -= OnCompositeChanged;
        Composite.Dispose();
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        Detach();
        Attach(_activeDocument.Session);

        OnPropertyChanged(nameof(Session));
        OnPropertyChanged(nameof(Composite));
        OnPropertyChanged(nameof(Font));
        OnPropertyChanged(nameof(ActiveTool));
        OnPropertyChanged(nameof(CanvasSizeText));
        OnPropertyChanged(nameof(CharacterSetText));
        OnPropertyChanged(nameof(FontText));
    }

    private void OnCompositeChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(CanvasSizeText));

    // A font change (or undoing one) redraws the canvas with the new font's cell size.
    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (!e.SettingsChanged)
            return;

        OnPropertyChanged(nameof(CharacterSetText));

        var font = _fonts.Resolve(Session.Document.Font);
        if (font == Font)
            return;

        Font = font;
        OnPropertyChanged(nameof(Font));
        OnPropertyChanged(nameof(FontText));
    }
}
