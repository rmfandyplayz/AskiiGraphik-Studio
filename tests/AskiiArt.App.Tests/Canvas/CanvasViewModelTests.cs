// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Canvas;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;

namespace AskiiArt.App.Tests.Canvas;

public sealed class CanvasViewModelTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(40, 20));

    private CanvasViewModel ViewModel => _canvas.ViewModel;

    [Fact]
    public void ZoomIn_ZoomOut_AndActualSize_StepThroughTheZoomLevels()
    {
        ViewModel.ZoomIn();
        Assert.Equal(1.25, ViewModel.ZoomFactor);

        ViewModel.ZoomOut();
        ViewModel.ZoomOut();
        Assert.Equal(0.75, ViewModel.ZoomFactor);

        ViewModel.ZoomToActualSize();
        Assert.Equal(1, ViewModel.ZoomFactor);
    }

    [Fact]
    public void ZoomFactor_StaysWithinTheZoomLevels()
    {
        ViewModel.ZoomFactor = 100;
        Assert.Equal(ZoomLevels.Maximum, ViewModel.ZoomFactor);
        Assert.False(ViewModel.CanZoomIn);

        ViewModel.ZoomFactor = 0;
        Assert.Equal(ZoomLevels.Minimum, ViewModel.ZoomFactor);
        Assert.False(ViewModel.CanZoomOut);
    }

    [Fact]
    public void StatusTexts_FollowZoomHoverAndCanvasSize()
    {
        var changed = new List<string?>();
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        ViewModel.ZoomFactor = 1.5;
        ViewModel.HoveredCell = new CellPoint(7, 3);
        new ResizeCanvasCommand(new CanvasSize(60, 30), ResizeAnchor.TopLeft).Apply(ViewModel.Composite.Document);

        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.StatusZoomFormat, 150), ViewModel.ZoomText);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.StatusCellFormat, 7, 3),
            ViewModel.HoveredCellText);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Strings.StatusCanvasSizeFormat, 60, 30),
            ViewModel.CanvasSizeText);
        Assert.Contains(nameof(CanvasViewModel.ZoomText), changed);
        Assert.Contains(nameof(CanvasViewModel.HoveredCellText), changed);
        Assert.Contains(nameof(CanvasViewModel.CanvasSizeText), changed);
    }

    [Fact]
    public void ANewDocument_SwitchesTheCompositeToolAndStatus()
    {
        var oldComposite = ViewModel.Composite;
        var oldTool = ViewModel.ActiveTool;
        var changed = new List<string?>();
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        var document = Document.Create(new CanvasSize(80, 25), "Layer", CharacterSet.Ascii);

        _canvas.ActiveDocument.Replace(new EditingSession(document));

        Assert.Same(document, ViewModel.Composite.Document);
        Assert.NotSame(oldComposite, ViewModel.Composite);
        Assert.NotSame(oldTool, ViewModel.ActiveTool);
        Assert.Same(_canvas.Session, ViewModel.Session);
        Assert.Equal(Strings.StatusCharacterSetAscii, ViewModel.CharacterSetText);
        Assert.Contains(nameof(CanvasViewModel.Composite), changed);
        Assert.Contains(nameof(CanvasViewModel.ActiveTool), changed);
        Assert.Contains(nameof(CanvasViewModel.CanvasSizeText), changed);
    }

    [Fact]
    public void FontText_ShowsTheDocumentsFont()
    {
        Assert.Equal("JetBrains Mono", ViewModel.FontText);

        _canvas.Session.History.Execute(new SetFontCommand(new FontReference("IBM Plex Mono")));

        Assert.Equal("IBM Plex Mono", ViewModel.FontText);
    }

    [Fact]
    public void HoveredCellText_IsEmptyWhenNoCellIsHovered()
    {
        ViewModel.HoveredCell = null;

        Assert.Equal(string.Empty, ViewModel.HoveredCellText);
    }

    [Fact]
    public void ToggleGrid_FlipsTheGrid()
    {
        ViewModel.ToggleGrid();
        Assert.True(ViewModel.IsGridVisible);

        ViewModel.ToggleGrid();
        Assert.False(ViewModel.IsGridVisible);
    }

    public void Dispose() => _canvas.Dispose();
}
