// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
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
/// The canvas's view of one document: what it draws (the composite, font and atlases), how
/// (zoom, grid, the cell under the pointer), and the tool that edits it. Zoom and grid are view
/// settings, not document changes, so they don't go through commands or undo.
/// </summary>
internal sealed class CanvasViewModel : ObservableObject, IDisposable
{
    /// <summary>The font size at 100% zoom, in logical pixels, until documents store their own (step 1.6).</summary>
    public const double DefaultFontSize = 16;

    public CanvasViewModel(EditingSession session, ToolRegistry tools, LoadedFont font, GlyphAtlasCache atlases)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(tools);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(atlases);

        Session = session;
        Composite = new CompositeCache(session.Document, CompositingPolicy.Default);
        Composite.Changed += OnCompositeChanged;
        session.Document.Changed += OnDocumentChanged;
        Font = font;
        Atlases = atlases;

        // The text tool is the only tool until M3, so it's always active.
        ActiveTool = tools.Get(TextTool.Id).Create(session);
    }

    public EditingSession Session { get; }

    public ITool ActiveTool { get; }

    public CompositeCache Composite { get; }

    public LoadedFont Font { get; }

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
        ActiveTool.Deactivate();
        Session.Document.Changed -= OnDocumentChanged;
        Composite.Changed -= OnCompositeChanged;
        Composite.Dispose();
    }

    private void OnCompositeChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(CanvasSizeText));

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        if (e.SettingsChanged)
            OnPropertyChanged(nameof(CharacterSetText));
    }
}
