// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Canvas.Rendering;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Imaging.Glyphs;
using Avalonia;
using SkiaSharp;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// The selection overlay (ADR 0061) drawn into a plain Skia bitmap: the hatching, the faint and bright outlines, and
/// a tool's closing line and markers.
/// </summary>
public sealed class SelectionOverlayRenderingTests : IDisposable
{
    private const float PixelSize = 16;

    private static readonly CanvasSize _size = new(4, 2);

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

    private static readonly SelectionLook _look = new(
        HatchLine: SKColors.Blue,
        HatchBackdrop: SKColors.Cyan,
        SubtractLine: SKColors.Red,
        SubtractBackdrop: new SKColor(128, 0, 0),
        IntersectLine: SKColors.Green,
        IntersectBackdrop: new SKColor(0, 100, 0),
        SubtractHatchLine: SKColors.Red,
        SubtractHatchBackdrop: new SKColor(128, 0, 0),
        IntersectHatchLine: SKColors.Green,
        IntersectHatchBackdrop: new SKColor(0, 100, 0),
        HatchBandWidth: 2,
        HatchSpeed: 1,
        SubtractHatchSpeed: 1,
        IntersectHatchSpeed: 1,
        HatchStepsPerPixel: 4,
        FaintOpacity: 0.5,
        VanishingOpacity: 0.25,
        LostHatchOpacity: 1,
        GridUnderHatchOpacity: 0.4,
        GridUnderLostHatchOpacity: 0.7,
        ClosingLineOpacity: 1,
        CornerMarkerRadius: 3,
        FinishMarkerRadius: 10,
        CornerMarkerOpacity: 1);

    private readonly TestCanvas _canvas = new(_size);
    private readonly CanvasSceneBuilder _builder = new();
    private readonly GlyphAtlas _atlas;

    public SelectionOverlayRenderingTests()
    {
        _atlas = _canvas.Atlases.GetAtlas(_canvas.Font, PixelSize, GlyphRenderOptions.Default);
    }

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void CellsThatArentSelected_AreHatched_AndSelectedOnesAreNot()
    {
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(Rectangle(2, 0, 2, 2), null), Look: _look));

        Assert.Contains(InsideOf(bitmap, viewport, new CellPoint(0, 1)), IsBluish);
        Assert.All(InsideOf(bitmap, viewport, new CellPoint(3, 0)), pixel => Assert.Equal(SKColors.White, pixel));
    }

    [Fact]
    public void TheHoveredCell_ShowsOverTheHatching_WithNoStripesThroughIt()
    {
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(false, new CellPoint(0, 1),
            Selection: SelectionDisplay.For(Rectangle(3, 0, 1, 1), null), Look: _look));

        // Half-strength green over white paper, the same in every pixel.
        var hovered = InsideOf(bitmap, viewport, new CellPoint(0, 1));
        Assert.All(hovered, pixel => Assert.Equal(hovered[0], pixel));
        Assert.False(IsBluish(hovered[0]), hovered[0].ToString());
        Assert.Contains(InsideOf(bitmap, viewport, new CellPoint(1, 1)), IsBluish);
    }

    [Fact]
    public void TheHatching_IsAZebra_OfDarkAndLightStripesWithNoGapsBetween()
    {
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(Rectangle(3, 0, 1, 1), null), Look: _look));

        // Blue and cyan stripes, opaque in this look, with every pixel between them some of each and none of the paper.
        var pixels = InsideOf(bitmap, viewport, new CellPoint(0, 1));
        Assert.All(pixels, pixel => Assert.True(pixel.Red < 8 && pixel.Blue > 247, pixel.ToString()));
        Assert.Contains(pixels, pixel => pixel == SKColors.Blue);
        Assert.Contains(pixels, pixel => pixel == SKColors.Cyan);
    }

    [Fact]
    public void TheHatching_RisesToTheRight()
    {
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(Rectangle(3, 0, 1, 1), null), Look: _look));

        var cell = Inside(viewport, new CellPoint(0, 1));
        Assert.True(Matches(bitmap, cell, rising: true) > Matches(bitmap, cell, rising: false));
    }

    [Fact]
    public void TheHatching_StaysPutOnTheArt_WhileTheCanvasMoves()
    {
        var display = SelectionDisplay.For(Rectangle(3, 0, 1, 1), null);
        var here = CreateViewport();
        var there = CreateViewport(width: 203);

        using var first = Paint(here, new CanvasOverlays(false, null, Selection: display, Look: _look));
        using var second = Paint(there, new CanvasOverlays(false, null, Selection: display, Look: _look));

        Assert.NotEqual(here.CanvasOrigin, there.CanvasOrigin);
        Assert.Equal(InsideOf(first, here, new CellPoint(0, 1)), InsideOf(second, there, new CellPoint(0, 1)));
    }

    [Fact]
    public void CellsASubtractWouldDeselect_AreHatchedTheOtherWay_InItsColour()
    {
        var viewport = CreateViewport();
        var preview = new SelectionPreview(SelectionMask.All(_size), SelectionCombineMode.Subtract,
            Rectangle(0, 0, 2, 2));

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(preview.Before, preview), Look: _look));

        var lost = InsideOf(bitmap, viewport, new CellPoint(0, 1));
        Assert.Contains(lost, pixel => pixel.Red > pixel.Green + 40 && pixel.Red > pixel.Blue + 40);
        Assert.DoesNotContain(lost, IsBluish);
        var cell = Inside(viewport, new CellPoint(0, 1));
        Assert.True(Matches(bitmap, cell, rising: false) > Matches(bitmap, cell, rising: true));
    }

    [Fact]
    public void AGestureThatWouldDeselectEverything_StillHatchesTheRest()
    {
        var preview = new SelectionPreview(Rectangle(0, 0, 2, 2), SelectionCombineMode.Subtract,
            SelectionMask.All(_size));
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(preview.Before, preview), Look: _look));

        Assert.Contains(InsideOf(bitmap, viewport, new CellPoint(3, 1)), IsBluish);
        Assert.Contains(InsideOf(bitmap, viewport, new CellPoint(0, 1)), pixel => pixel.Red > pixel.Blue + 40);
    }

    [Fact]
    public void AGestureWithNothingSelectedBeforeOrAfter_HatchesNothing()
    {
        var nothing = SelectionMask.Empty(_size);
        var preview = new SelectionPreview(nothing, SelectionCombineMode.Intersect, Rectangle(0, 0, 2, 2));

        using var scene = Build(CreateViewport(), new CanvasOverlays(true, null,
            Selection: SelectionDisplay.For(nothing, preview), Look: _look));

        Assert.Null(scene.UnselectedHatch);
        Assert.Null(scene.LostHatch);
        Assert.Null(scene.GridShade);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheHatching_MovesAlongARowByItsShift(bool isLost)
    {
        var preview = new SelectionPreview(Rectangle(0, 0, 3, 2), SelectionCombineMode.Subtract,
            Rectangle(0, 0, 1, 2));
        var display = SelectionDisplay.For(preview.Before, preview);
        var viewport = CreateViewport();
        var cell = isLost ? new CellPoint(0, 1) : new CellPoint(3, 1);
        var moved = isLost ? new SelectionMotion(0, 0, 2) : new SelectionMotion(0, 2, 0);

        using var still = Paint(viewport, new CanvasOverlays(false, null, Selection: display, Look: _look));
        using var shifted = Paint(viewport, new CanvasOverlays(false, null, Selection: display,
            SelectionMotion: moved, Look: _look));

        var rect = Inside(viewport, cell);
        Assert.NotEqual(InsideOf(still, viewport, cell), InsideOf(shifted, viewport, cell));
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right - 2; x++)
                Assert.Equal(still.GetPixel(x + 2, y), shifted.GetPixel(x, y));
        }
    }

    [Fact]
    public void HatchingMovedPartOfAPixel_SpreadsEachLineOverThePixelsItLiesAcross()
    {
        var display = SelectionDisplay.For(Rectangle(0, 0, 1, 1), null);
        var viewport = CreateViewport();
        var cell = new CellPoint(2, 1);

        using var still = Paint(viewport, new CanvasOverlays(false, null, Selection: display, Look: _look));
        using var halfway = Paint(viewport, new CanvasOverlays(false, null, Selection: display,
            SelectionMotion: new SelectionMotion(0, 0.5, 0), Look: _look));

        // Each pixel is half its own colour and half its right-hand neighbour's, give or take rounding.
        var rect = Inside(viewport, cell);
        var blended = 0;
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right - 1; x++)
            {
                var (here, next, actual) = (still.GetPixel(x, y), still.GetPixel(x + 1, y), halfway.GetPixel(x, y));
                Assert.InRange(actual.Red, Halfway(here.Red, next.Red) - 2, Halfway(here.Red, next.Red) + 2);
                Assert.InRange(actual.Green, Halfway(here.Green, next.Green) - 2, Halfway(here.Green, next.Green) + 2);
                Assert.InRange(actual.Blue, Halfway(here.Blue, next.Blue) - 2, Halfway(here.Blue, next.Blue) + 2);
                if (actual != here && actual != next)
                    blended++;
            }
        }

        Assert.True(blended > 0);
    }

    [Fact]
    public void TheGrid_ShowsLessUnderTheHatching_AndLessStillUnderTheHatchingOverCellsAboutToBeDeselected()
    {
        var preview = new SelectionPreview(Rectangle(0, 0, 3, 2), SelectionCombineMode.Subtract,
            Rectangle(0, 0, 1, 2));
        var unhatched = _look with
        {
            HatchLine = SKColors.Transparent,
            HatchBackdrop = SKColors.Transparent,
            SubtractLine = SKColors.Transparent,
            SubtractBackdrop = SKColors.Transparent,
            SubtractHatchLine = SKColors.Transparent,
            SubtractHatchBackdrop = SKColors.Transparent
        };
        var viewport = CreateViewport();

        using var bitmap = Paint(viewport, new CanvasOverlays(true, null,
            Selection: SelectionDisplay.For(preview.Before, preview), Look: unhatched));

        // The grid line along the second row's top, half way across a cell, where no outline runs.
        int GridAt(int column)
        {
            var bounds = viewport.GetCellBounds(new CellPoint(column, 1));
            return 255 - bitmap.GetPixel(bounds.X + bounds.Width / 2, bounds.Y).Red;
        }

        var full = GridAt(1);
        Assert.True(full > 200);
        Assert.InRange(GridAt(0), full * 0.6, full * 0.8);
        Assert.InRange(GridAt(3), full * 0.3, full * 0.5);
    }

    [Fact]
    public void ALookWithoutHatching_HatchesNothing()
    {
        using var scene = Build(CreateViewport(), new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(Rectangle(2, 0, 2, 2), null)));

        Assert.Null(scene.UnselectedHatch);
    }

    [Fact]
    public void FaintOutlines_AreFadedAndStill_AndTheBrightOneRuns()
    {
        var preview = new SelectionPreview(Rectangle(0, 0, 2, 1), SelectionCombineMode.Add, Rectangle(1, 0, 2, 2));

        using var scene = Build(CreateViewport(), new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(preview.Before, preview), SelectionMotion: new SelectionMotion(3, 0, 0),
            Look: _look));

        var passes = scene.SelectionOutlines;
        Assert.Equal(3, passes.Count);
        Assert.All(passes.Take(2), pass => Assert.Equal(0, pass.DashOffset));
        Assert.Equal(SKColors.Black.WithAlpha(64), passes[0].Line);
        Assert.Equal(SKColors.Yellow.WithAlpha(64), passes[0].Backdrop);
        Assert.Equal(SKColors.Black.WithAlpha(128), passes[1].Line);
        Assert.Equal(SKColors.Yellow.WithAlpha(128), passes[1].Backdrop);
        Assert.Equal(3, passes[2].DashOffset);
        Assert.Equal(SKColors.Black, passes[2].Line);
    }

    [Theory]
    [InlineData(SelectionCombineMode.Subtract, 255, 0, 0)]
    [InlineData(SelectionCombineMode.Intersect, 0, 128, 0)]
    public void SubtractAndIntersect_RunTheirOutlinesInTheirOwnColours(SelectionCombineMode mode, byte red,
        byte green, byte blue)
    {
        var preview = new SelectionPreview(Rectangle(0, 0, 2, 2), mode, Rectangle(1, 0, 2, 2));

        using var scene = Build(CreateViewport(), new CanvasOverlays(false, null,
            Selection: SelectionDisplay.For(preview.Before, preview), Look: _look));

        Assert.Equal(new SKColor(red, green, blue), scene.SelectionOutlines[^1].Line);
    }

    [Fact]
    public void AClosingLine_JoinsTheLastPointToTheFirst_OnlyWhenItsShown()
    {
        var viewport = CreateViewport();
        CanvasPoint[] path = [new(0.5, 0.5), new(3.5, 0.5), new(3.5, 1.5)];

        using var shown = Build(viewport, new CanvasOverlays(false, null, ToolPath: path, ShowsToolPathClosing: true,
            Look: _look));
        using var hidden = Build(viewport, new CanvasOverlays(false, null, ToolPath: path, Look: _look));
        using var tooShort = Build(viewport, new CanvasOverlays(false, null, ToolPath: path[..2],
            ShowsToolPathClosing: true, Look: _look));
        using var bitmap = PaintScene(shown);

        Assert.Equal((shown.ToolPath[2], shown.ToolPath[0]), shown.ToolPathClosingLine);
        Assert.Null(hidden.ToolPathClosingLine);
        Assert.Null(tooShort.ToolPathClosingLine);
        var middle = DevicePoint(viewport, new CanvasPoint(2, 1));
        Assert.NotEqual(SKColors.White, bitmap.GetPixel((int)middle.X, (int)middle.Y));
    }

    [Theory]
    [InlineData(ToolMarkerKind.Finish, 0, 0, 0, 255, 255, 0)]
    [InlineData(ToolMarkerKind.FinishUnderPointer, 255, 255, 0, 0, 0, 0)]
    public void TheFinishingMarker_IsADotOnADisc_SwappedRoundWhileThePointerIsOnIt(ToolMarkerKind kind,
        byte dotRed, byte dotGreen, byte dotBlue, byte discRed, byte discGreen, byte discBlue)
    {
        var viewport = CreateViewport();
        var at = new CanvasPoint(2, 1);

        using var bitmap = Paint(viewport, new CanvasOverlays(false, null, ToolMarkers: [new ToolMarker(at, kind)],
            Look: _look));

        var centre = DevicePoint(viewport, at);
        Assert.Equal(new SKColor(dotRed, dotGreen, dotBlue), bitmap.GetPixel((int)centre.X, (int)centre.Y));
        Assert.Equal(new SKColor(discRed, discGreen, discBlue), bitmap.GetPixel((int)centre.X + 5, (int)centre.Y));
    }

    [Fact]
    public void ACornersMarker_IsASmallDisc()
    {
        var viewport = CreateViewport();
        var at = new CanvasPoint(2, 1);
        ToolMarker[] markers = [new(at, ToolMarkerKind.Corner)];

        using var drawn = Paint(viewport, new CanvasOverlays(false, null, ToolMarkers: markers, Look: _look));
        using var withoutALook = Paint(viewport, new CanvasOverlays(false, null, ToolMarkers: markers));

        var centre = DevicePoint(viewport, at);
        Assert.Equal(SKColors.Yellow, drawn.GetPixel((int)centre.X, (int)centre.Y));
        Assert.Equal(SKColors.White, drawn.GetPixel((int)centre.X + 5, (int)centre.Y));
        Assert.Equal(SKColors.White, withoutALook.GetPixel((int)centre.X, (int)centre.Y));
    }

    private static bool IsBluish(SKColor pixel) => pixel.Blue > pixel.Red + 40;

    private static int Halfway(byte first, byte second) => (first + second) / 2;

    // How many pixels in the rect have the same colour as their neighbour up and to the right (rising) or down and
    // to the right (falling): along a hatching's lines, they all do.
    private static int Matches(SKBitmap bitmap, PixelRect rect, bool rising)
    {
        var step = rising ? -1 : 1;
        var matches = 0;
        for (var y = rect.Y + 1; y < rect.Bottom - 1; y++)
        {
            for (var x = rect.X; x < rect.Right - 1; x++)
            {
                if (bitmap.GetPixel(x, y) == bitmap.GetPixel(x + 1, y + step))
                    matches++;
            }
        }

        return matches;
    }

    // A cell's pixels, leaving out the one-pixel strip around its edge where an outline may run.
    private static PixelRect Inside(CanvasViewport viewport, CellPoint cell)
    {
        var bounds = viewport.GetCellBounds(cell);
        return new PixelRect(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2);
    }

    private static SKColor[] InsideOf(SKBitmap bitmap, CanvasViewport viewport, CellPoint cell)
    {
        var rect = Inside(viewport, cell);
        var pixels = new List<SKColor>();
        for (var y = rect.Y; y < rect.Bottom; y++)
        {
            for (var x = rect.X; x < rect.Right; x++)
                pixels.Add(bitmap.GetPixel(x, y));
        }

        return [.. pixels];
    }

    private static Point DevicePoint(CanvasViewport viewport, CanvasPoint point) =>
        new(viewport.CanvasOrigin.X + point.X * viewport.CellSize.Width,
            viewport.CanvasOrigin.Y + point.Y * viewport.CellSize.Height);

    private static SelectionMask Rectangle(int left, int top, int width, int height) =>
        SelectionMask.Rectangle(_size, new CellRect(left, top, width, height));

    // The canvas is smaller than the viewport, so it sits in the middle: a wider viewport moves it.
    private CanvasViewport CreateViewport(int width = 200) =>
        new(_size, new PixelSize(_atlas.Metrics.Width, _atlas.Metrics.Height), new PixelSize(width, 100), margin: 10,
            default);

    private CanvasScene Build(CanvasViewport viewport, CanvasOverlays overlays) =>
        _builder.Build(_canvas.ViewModel.Composite, _atlas, viewport, _colors, overlays);

    private SKBitmap Paint(CanvasViewport viewport, CanvasOverlays overlays)
    {
        using var scene = Build(viewport, overlays);
        return PaintScene(scene);
    }

    private static SKBitmap PaintScene(CanvasScene scene)
    {
        var size = scene.Viewport.ViewportSize;
        var bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        CanvasScenePainter.Paint(canvas, scene);
        return bitmap;
    }
}
