// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Canvas.Rendering;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Imaging.Glyphs;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// The scene builder and painter together, drawing into a plain Skia bitmap: the same code the
/// canvas control runs on the render thread, minus Avalonia.
/// </summary>
public sealed class CanvasRenderingTests : IDisposable
{
    private const float PixelSize = 16;

    private static readonly CanvasColors _colors = new(
        Pasteboard: SKColors.Gray,
        Paper: SKColors.White,
        Ink: SKColors.Red,
        GridLine: SKColors.Blue,
        Hover: new SKColor(0, 255, 0, 128),
        FallbackGlyph: SKColors.Orange,
        PlaceholderGlyph: SKColors.Pink,
        Caret: SKColors.Purple);

    private readonly TestCanvas _canvas = new(new CanvasSize(4, 2));
    private readonly CanvasSceneBuilder _builder = new();
    private readonly GlyphAtlas _atlas;

    public CanvasRenderingTests()
    {
        _atlas = _canvas.Atlases.GetAtlas(_canvas.Font, PixelSize, GlyphRenderOptions.Default);
    }

    private PixelSize CellSize => new(_atlas.Metrics.Width, _atlas.Metrics.Height);

    [Fact]
    public void Paint_EmptyCanvas_ShowsPaperOnThePasteboard()
    {
        using var bitmap = Paint(CreateViewport());

        var canvasBounds = CreateViewport().CanvasBounds;
        Assert.Equal(SKColors.Gray, bitmap.GetPixel(0, 0));
        Assert.Equal(SKColors.White, bitmap.GetPixel(canvasBounds.X + 1, canvasBounds.Y + 1));
    }

    [Fact]
    public void Paint_Glyph_IsDrawnInTheInkColourInsideItsCell()
    {
        Write(new CellPoint(1, 0), '█');
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport);

        var cell = viewport.GetCellBounds(new CellPoint(1, 0));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(cell.X + cell.Width / 2, cell.Y + cell.Height / 2));
        var neighbour = viewport.GetCellBounds(new CellPoint(2, 0));
        Assert.Equal(SKColors.White, bitmap.GetPixel(neighbour.X + neighbour.Width / 2, neighbour.Y + neighbour.Height / 2));
    }

    [Fact]
    public void Build_GlyphNoFontHas_IsFlaggedAsAPlaceholder()
    {
        Write(new CellPoint(0, 1), char.ConvertFromUtf32(0x10FFFD));
        var viewport = CreateViewport();

        using var scene = Build(viewport);
        using var bitmap = PaintScene(scene);

        var cell = viewport.GetCellBounds(new CellPoint(0, 1));
        Assert.Equal([cell], scene.PlaceholderGlyphCells);
        Assert.Empty(scene.FallbackGlyphCells);
        Assert.Equal(SKColors.Pink, bitmap.GetPixel(cell.X + 1, cell.Y + 1));
    }

    [Fact]
    public void Build_SpacesAndEmptyCells_DrawNoGlyphs()
    {
        Write(new CellPoint(0, 0), ' ');

        using var scene = Build(CreateViewport());

        Assert.Empty(scene.Glyphs);
    }

    [Fact]
    public void Build_SameGlyphTwice_RasterizesItOnceAndBatchesBoth()
    {
        Write(new CellPoint(0, 0), 'A');
        Write(new CellPoint(3, 1), 'A');

        using var scene = Build(CreateViewport());

        var batch = Assert.Single(scene.Glyphs);
        Assert.Equal(2, batch.Count);
        Assert.Equal(1, _atlas.GlyphCount);
        Assert.True(batch.Sprites.Skip(batch.Count).All(sprite => sprite.IsEmpty));
    }

    [Fact]
    public void Build_OnlyReadsTheVisibleCells()
    {
        Write(new CellPoint(0, 0), 'A');
        Write(new CellPoint(3, 1), 'B');

        // A viewport just big enough for the top-left cell, with no margin.
        var viewport = new CanvasViewport(_canvas.ViewModel.Composite.Size, CellSize, CellSize, margin: 0, default);
        using var scene = Build(viewport);

        Assert.Equal(1, Assert.Single(scene.Glyphs).Count);
        Assert.Equal(1, _atlas.GlyphCount);
    }

    [Fact]
    public void Paint_HoveredCell_IsHighlightedOverThePaper()
    {
        var viewport = CreateViewport();

        using var scene = Build(viewport, hoveredCell: new CellPoint(2, 1));
        using var bitmap = PaintScene(scene);

        var cell = viewport.GetCellBounds(new CellPoint(2, 1));
        var pixel = bitmap.GetPixel(cell.X + 2, cell.Y + 2);
        Assert.NotEqual(SKColors.White, pixel);
        Assert.True(pixel.Green > pixel.Blue, $"Expected a green tint, got {pixel}.");
    }

    [Fact]
    public void Paint_Caret_OutlinesItsCellAndLeavesTheMiddleAlone()
    {
        var viewport = CreateViewport();

        using var scene = Build(viewport, caret: new CellPoint(1, 1));
        using var bitmap = PaintScene(scene);

        var cell = viewport.GetCellBounds(new CellPoint(1, 1));
        Assert.Equal(SKColors.Purple, bitmap.GetPixel(cell.X, cell.Y + cell.Height / 2));
        Assert.Equal(SKColors.Purple, bitmap.GetPixel(cell.Right - 1, cell.Y + cell.Height / 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(cell.X + cell.Width / 2, cell.Y + cell.Height / 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(cell.Right, cell.Y + cell.Height / 2));
    }

    [Fact]
    public void Paint_Grid_DrawsLinesOnCellEdgesOnlyWhenVisible()
    {
        var viewport = CreateViewport();
        var cell = viewport.GetCellBounds(new CellPoint(1, 0));
        var edge = new SKPointI(cell.X, cell.Y + cell.Height / 2);

        using var withGrid = Paint(viewport, isGridVisible: true);
        using var withoutGrid = Paint(viewport, isGridVisible: false);

        Assert.Equal(SKColors.Blue, withGrid.GetPixel(edge.X, edge.Y));
        Assert.Equal(SKColors.White, withoutGrid.GetPixel(edge.X, edge.Y));
    }

    [Fact]
    public void Scene_KeepsItsPageImageAliveAfterTheBuilderMovesOn()
    {
        Write(new CellPoint(0, 0), 'A');
        var viewport = CreateViewport();
        using var first = Build(viewport);

        // A new glyph changes the page, so the builder replaces its copy of it.
        Write(new CellPoint(1, 0), 'B');
        using var second = Build(viewport);

        Assert.NotSame(first.Glyphs[0].Page, second.Glyphs[0].Page);
        using var bitmap = PaintScene(first);
        var cell = viewport.GetCellBounds(new CellPoint(0, 0));
        Assert.Contains(SKColors.Red, Pixels(bitmap, cell));
    }

    public void Dispose()
    {
        _builder.ReleaseCachedPages();
        _canvas.Dispose();
    }

    private static SKColor[] Pixels(SKBitmap bitmap, PixelRect rect)
    {
        var pixels = new List<SKColor>();
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
                pixels.Add(bitmap.GetPixel(x, y));
        }

        return [.. pixels];
    }

    private static SKBitmap PaintScene(CanvasScene scene)
    {
        var size = scene.Viewport.ViewportSize;
        var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        CanvasScenePainter.Paint(canvas, scene);
        return bitmap;
    }

    private CanvasViewport CreateViewport() =>
        new(_canvas.ViewModel.Composite.Size, CellSize, new PixelSize(200, 100), margin: 10, default);

    private CanvasScene Build(CanvasViewport viewport, bool isGridVisible = false, CellPoint? hoveredCell = null,
        CellPoint? caret = null) =>
        _builder.Build(_canvas.ViewModel.Composite, _atlas, viewport, _colors,
            new CanvasOverlays(isGridVisible, hoveredCell, caret, CaretThickness: 2));

    private SKBitmap Paint(CanvasViewport viewport, bool isGridVisible = false)
    {
        using var scene = Build(viewport, isGridVisible);
        return PaintScene(scene);
    }

    private void Write(CellPoint point, char glyph) => Write(point, glyph.ToString());

    private void Write(CellPoint point, string glyph)
    {
        var document = _canvas.ViewModel.Composite.Document;
        var builder = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        builder.Set(point, Cell.FromGlyph(Rune.GetRuneAt(glyph, 0)));
        builder.Build()!.Apply(document);
    }
}
