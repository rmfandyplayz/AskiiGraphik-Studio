// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Canvas.Rendering;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Footprints;
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
        Caret: SKColors.Purple,
        SelectionLine: SKColors.Black,
        SelectionBackdrop: SKColors.Yellow);

    private static readonly SKColor _blue = new(0, 0, 200);
    private static readonly SKColor _green = new(0, 200, 0);

    private readonly TestCanvas _canvas = new(new CanvasSize(4, 2), colorMode: ColorMode.Rgb);
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
    public void Paint_GlyphWithATextColour_IsDrawnInThatColour()
    {
        Write(new CellPoint(1, 0), Cell.FromGlyph(new Rune('█'), Rgb(_green), CellColor.Unset));
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport);

        Assert.Equal(_green, CentreOf(bitmap, viewport.GetCellBounds(new CellPoint(1, 0))));
    }

    [Fact]
    public void Paint_AnsiTextColour_IsDrawnAsTheVgaPaletteShowsIt()
    {
        new SetColorModeCommand(ColorMode.Ansi).Apply(_canvas.Document);
        Write(new CellPoint(1, 0), Cell.FromGlyph(new Rune('█'), CellColor.FromAnsi(AnsiColor.BrightMagenta),
            CellColor.Unset));
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport);

        var vga = AnsiPalette.Vga[AnsiColor.BrightMagenta];
        Assert.Equal(new SKColor(vga.Red, vga.Green, vga.Blue),
            CentreOf(bitmap, viewport.GetCellBounds(new CellPoint(1, 0))));
    }

    [Fact]
    public void Paint_Background_FillsTheWholeCellBehindItsGlyph()
    {
        // The upper half block inks exactly the top half of its cell, leaving the bottom half to the background.
        Write(new CellPoint(1, 0), Cell.FromGlyph(new Rune('▀'), CellColor.Unset, Rgb(_blue)));
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport);

        var cell = viewport.GetCellBounds(new CellPoint(1, 0));
        var middle = cell.X + cell.Width / 2;
        Assert.Equal(SKColors.Red, bitmap.GetPixel(middle, cell.Y + 1));
        Assert.Equal(_blue, bitmap.GetPixel(middle, cell.Bottom - 1));
        Assert.Equal(_blue, bitmap.GetPixel(cell.Right - 1, cell.Bottom - 1));
        Assert.Equal(_blue, bitmap.GetPixel(cell.X, cell.Bottom - 1));
        Assert.Equal(SKColors.White, bitmap.GetPixel(cell.Right, cell.Bottom - 1));
        Assert.Equal(SKColors.White, bitmap.GetPixel(cell.X - 1, cell.Bottom - 1));
        Assert.Equal(SKColors.White, bitmap.GetPixel(middle, cell.Bottom));
    }

    [Fact]
    public void Paint_CellWithOnlyABackground_FillsItsCellAndDrawsNoGlyph()
    {
        Write(new CellPoint(2, 1), Cell.FromBackground(Rgb(_blue)));
        var viewport = CreateViewport();

        using var scene = Build(viewport);
        using var bitmap = PaintScene(scene);

        Assert.Empty(scene.Glyphs);
        Assert.All(Pixels(bitmap, viewport.GetCellBounds(new CellPoint(2, 1))), pixel => Assert.Equal(_blue, pixel));
    }

    [Fact]
    public void Build_WithoutBackgrounds_HasNoBackgroundImage()
    {
        Write(new CellPoint(0, 0), 'A');

        using var scene = Build(CreateViewport());

        Assert.Null(scene.Backgrounds);
    }

    [Fact]
    public void Paint_Backgrounds_LandOnTheirCellsWhenTheCanvasIsScrolled()
    {
        Write(new CellPoint(0, 0), Cell.FromBackground(Rgb(SKColors.Black)));
        Write(new CellPoint(1, 0), Cell.FromBackground(Rgb(_blue)));
        Write(new CellPoint(2, 0), Cell.FromBackground(Rgb(_green)));

        // Two cells wide, scrolled so the first cell is out of view and the second is cut off on the left.
        var viewport = new CanvasViewport(_canvas.ViewModel.Composite.Size, CellSize,
            new PixelSize(CellSize.Width * 2, CellSize.Height * 2), margin: 0, new PixelPoint(CellSize.Width + 3, 0));
        using var bitmap = Paint(viewport);

        Assert.Equal(CellRect.FromEdges(1, 0, 4, 2), viewport.VisibleCells);
        Assert.Equal(_blue, bitmap.GetPixel(0, 0));
        var third = viewport.GetCellBounds(new CellPoint(2, 0));
        Assert.Equal(_blue, bitmap.GetPixel(third.X - 1, third.Bottom - 1));
        Assert.Equal(_green, bitmap.GetPixel(third.X, third.Y));
        Assert.Equal(_green, bitmap.GetPixel(third.Right - 1, third.Bottom - 1));
        Assert.Equal(SKColors.White, bitmap.GetPixel(third.Right, third.Y));
        Assert.Equal(SKColors.White, bitmap.GetPixel(0, third.Bottom));
    }

    [Fact]
    public void Paint_MissingGlyphFlag_ShowsOverABackground()
    {
        var noFontHasIt = Rune.GetRuneAt(char.ConvertFromUtf32(0x10FFFD), 0);
        Write(new CellPoint(0, 1), Cell.FromGlyph(noFontHasIt, CellColor.Unset, Rgb(_blue)));
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport);

        var cell = viewport.GetCellBounds(new CellPoint(0, 1));
        Assert.Equal(SKColors.Pink, bitmap.GetPixel(cell.X + 1, cell.Y + 1));
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
    public void Paint_BrushOutline_GoesAroundTheTip_AndLeavesItsMiddleAlone()
    {
        var viewport = CreateViewport();
        var square = BrushFootprint.Create(new BrushTip(2, BrushShape.Square, BrushProportions.Mathematical), 1);

        using var scene = Build(viewport, hoveredCell: new CellPoint(1, 0),
            brush: new BrushOutline(square, new CellPoint(1, 0)));
        using var bitmap = PaintScene(scene);

        var topLeft = viewport.GetCellBounds(new CellPoint(1, 0));
        var bottomRight = viewport.GetCellBounds(new CellPoint(2, 1));
        Assert.Null(scene.HoveredCell);
        Assert.Equal(4, scene.BrushOutline.Count);
        Assert.Equal(SKColors.Purple, bitmap.GetPixel(topLeft.X, topLeft.Y + topLeft.Height / 2));
        Assert.Equal(SKColors.Purple, bitmap.GetPixel(bottomRight.Right - 1, bottomRight.Y + 1));
        Assert.Equal(SKColors.White, bitmap.GetPixel(topLeft.Right, topLeft.Bottom));
        Assert.Equal(SKColors.White, bitmap.GetPixel(topLeft.X - 1, topLeft.Y + topLeft.Height / 2));
    }

    [Fact]
    public void Build_BrushOutline_KeepsItsEdgesOffTheCanvas()
    {
        var viewport = CreateViewport();
        var circle = BrushFootprint.Create(new BrushTip(3, BrushShape.Circle, BrushProportions.Mathematical), 1);

        using var scene = Build(viewport, brush: new BrushOutline(circle, new CellPoint(-1, -1)));

        Assert.Equal(circle.Edges.Count, scene.BrushOutline.Count);
        Assert.Contains(scene.BrushOutline, strip => strip.X < viewport.CanvasBounds.X);
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
    public void Paint_SelectionOutline_RunsInsideTheSelectedCells_AndLeavesTheirMiddleAlone()
    {
        var viewport = CreateViewport();
        var selection = SelectionMask.Rectangle(new CanvasSize(4, 2), CellRect.FromEdges(1, 0, 3, 2));

        using var scene = Build(viewport, selection: selection);
        using var bitmap = PaintScene(scene);

        var topLeft = viewport.GetCellBounds(new CellPoint(1, 0));
        var bottomRight = viewport.GetCellBounds(new CellPoint(2, 1));
        Assert.Equal(4, scene.SelectionOutlines.Single().Strips.Count);
        Assert.Equal([SKColors.Black, SKColors.Yellow], OutlineColours(bitmap, topLeft.X, topLeft.Y, topLeft.Width));
        Assert.Equal(SKColors.White, CentreOf(bitmap, topLeft));
        Assert.Equal(SKColors.White, bitmap.GetPixel(topLeft.Right - 1, topLeft.Y + topLeft.Height / 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(topLeft.X - 1, topLeft.Y + topLeft.Height / 2));
        Assert.NotEqual(SKColors.White, bitmap.GetPixel(bottomRight.Right - 1, bottomRight.Y + bottomRight.Height / 2));
    }

    [Fact]
    public void Paint_SelectionDashes_AreCountedFromTheCanvasCorner()
    {
        var viewport = CreateViewport();

        using var scene = Build(viewport, selection: SelectionMask.All(new CanvasSize(4, 2)));
        using var bitmap = PaintScene(scene);

        var corner = viewport.CanvasBounds;
        Assert.Equal(SKColors.Black, bitmap.GetPixel(corner.X, corner.Y));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(corner.X + 3, corner.Y));
        Assert.Equal(SKColors.Yellow, bitmap.GetPixel(corner.X + 4, corner.Y));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(corner.X + 8, corner.Y));
        Assert.Equal(SKColors.Yellow, bitmap.GetPixel(corner.X, corner.Y + 4));
    }

    [Fact]
    public void Paint_SelectionDashes_RunClockwiseAroundTheSelection()
    {
        var viewport = CreateViewport();

        using var scene = Build(viewport, selection: SelectionMask.All(new CanvasSize(4, 2)), selectionDashOffset: 1);
        using var bitmap = PaintScene(scene);

        // Before they run, each edge has a dash over its first four pixels from the canvas's corner, then a gap. One
        // pixel on, the top edge's dash has moved right, the right edge's down, the bottom edge's left and the left
        // edge's up.
        var corner = viewport.CanvasBounds;
        Assert.Equal(SKColors.Yellow, bitmap.GetPixel(corner.X, corner.Y + 3));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(corner.X + 4, corner.Y));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(corner.Right - 1, corner.Y + 4));
        Assert.Equal(SKColors.Yellow, bitmap.GetPixel(corner.X + 3, corner.Bottom - 1));
    }

    [Fact]
    public void Build_SelectionOutline_KeepsOnlyWhatIsInView()
    {
        // A viewport just big enough for the top-left cell, with no margin.
        var viewport = new CanvasViewport(_canvas.ViewModel.Composite.Size, CellSize, CellSize, margin: 0, default);

        using var scene = Build(viewport, selection: SelectionMask.All(new CanvasSize(4, 2)));

        var strips = scene.SelectionOutlines.Single().Strips;
        Assert.Equal(2, strips.Count);
        Assert.All(strips,
            strip => Assert.True(strip.Rect.Right <= CellSize.Width && strip.Rect.Bottom <= CellSize.Height));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_NoSelection_DrawsNoOutline(bool hasEmptyMask)
    {
        var selection = hasEmptyMask ? SelectionMask.Empty(new CanvasSize(4, 2)) : null;

        using var scene = Build(CreateViewport(), selection: selection);

        Assert.Empty(scene.SelectionOutlines);
    }

    [Fact]
    public void Paint_ToolPath_DrawsALineThroughItsPoints_OverABackdrop()
    {
        var viewport = CreateViewport();
        var origin = viewport.CanvasOrigin;
        var cell = viewport.CellSize;
        CanvasPoint[] path = [new(0.5, 0.5), new(3.5, 0.5), new(3.5, 1.5)];

        using var scene = _builder.Build(_canvas.ViewModel.Composite, _atlas, viewport, _colors,
            new CanvasOverlays(false, null, SelectionThickness: 2, ToolPath: path));
        using var bitmap = PaintScene(scene);

        Assert.Equal(new Point(origin.X + 3.5 * cell.Width, origin.Y + 0.5 * cell.Height), scene.ToolPath[1]);
        var lineY = origin.Y + cell.Height / 2;
        var middleX = origin.X + 2 * cell.Width;
        Assert.Equal(SKColors.Black, bitmap.GetPixel(middleX, lineY));
        Assert.Equal(SKColors.Yellow, bitmap.GetPixel(middleX, lineY - 2));
        Assert.Equal(SKColors.White, bitmap.GetPixel(middleX, lineY - 5));
    }

    [Fact]
    public void Build_NoToolPath_DrawsNoLine()
    {
        using var scene = Build(CreateViewport());

        Assert.Empty(scene.ToolPath);
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

    // The distinct colours along a row of pixels, in the order they first appear.
    private static SKColor[] OutlineColours(SKBitmap bitmap, int x, int y, int length) =>
        [.. Enumerable.Range(x, length).Select(column => bitmap.GetPixel(column, y)).Distinct()];

    private static SKColor CentreOf(SKBitmap bitmap, PixelRect rect) =>
        bitmap.GetPixel(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    private static CellColor Rgb(SKColor color) =>
        CellColor.FromRgb(new RgbColor(color.Red, color.Green, color.Blue));

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
        CellPoint? caret = null, BrushOutline? brush = null, SelectionMask? selection = null,
        int selectionDashOffset = 0) =>
        _builder.Build(_canvas.ViewModel.Composite, _atlas, viewport, _colors,
            new CanvasOverlays(isGridVisible, hoveredCell, caret, CaretThickness: 2, brush,
                SelectionDisplay.For(selection, null), SelectionDashOffset: selectionDashOffset));

    private SKBitmap Paint(CanvasViewport viewport, bool isGridVisible = false)
    {
        using var scene = Build(viewport, isGridVisible);
        return PaintScene(scene);
    }

    private void Write(CellPoint point, char glyph) => Write(point, glyph.ToString());

    private void Write(CellPoint point, string glyph) => Write(point, Cell.FromGlyph(Rune.GetRuneAt(glyph, 0)));

    private void Write(CellPoint point, Cell cell)
    {
        var document = _canvas.ViewModel.Composite.Document;
        var builder = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);
        builder.Set(point, cell);
        builder.Build()!.Apply(document);
    }
}
