// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Resources;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Fonts;
using AskiiArt.Imaging.Glyphs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas's view of one document: what it draws (the composite, font and atlases) and how
/// (zoom, grid, the cell under the pointer). Zoom and grid are view settings, not document
/// changes, so they don't go through commands or undo.
/// </summary>
internal sealed class CanvasViewModel : ObservableObject, IDisposable
{
    /// <summary>The font size at 100% zoom, in logical pixels, until documents store their own (step 1.6).</summary>
    public const double DefaultFontSize = 16;

    public CanvasViewModel(Document document, LoadedFont font, GlyphAtlasCache atlases)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(atlases);

        Composite = new CompositeCache(document, CompositingPolicy.Default);
        Composite.Changed += OnCompositeChanged;
        Font = font;
        Atlases = atlases;
    }

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

    public string HoveredCellText => HoveredCell is { } cell
        ? string.Format(CultureInfo.CurrentCulture, Strings.StatusCellFormat, cell.X, cell.Y)
        : string.Empty;

    public string CanvasSizeText => string.Format(CultureInfo.CurrentCulture, Strings.StatusCanvasSizeFormat,
        Composite.Size.Width, Composite.Size.Height);

    public void ZoomIn() => ZoomFactor = ZoomLevels.StepIn(ZoomFactor);

    public void ZoomOut() => ZoomFactor = ZoomLevels.StepOut(ZoomFactor);

    public void ZoomToActualSize() => ZoomFactor = ZoomLevels.ActualSize;

    public void ToggleGrid() => IsGridVisible = !IsGridVisible;

    public void Dispose()
    {
        Composite.Changed -= OnCompositeChanged;
        Composite.Dispose();
    }

    private void OnCompositeChanged(object? sender, EventArgs e) => OnPropertyChanged(nameof(CanvasSizeText));
}
