// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Tests.Canvas;

public sealed class CompositeCacheTests
{
    private readonly Document _document = Document.Create(new CanvasSize(6, 4), "Layer 1");
    private readonly CommandHistory _history;

    public CompositeCacheTests()
    {
        _history = new CommandHistory(_document);
    }

    [Fact]
    public void NewCache_HoldsTheCurrentComposite()
    {
        Write(BottomLayerId, new CellPoint(2, 1), 'A');

        using var cache = new CompositeCache(_document, CompositingPolicy.Default);

        AssertMatchesTheDocument(cache);
    }

    [Fact]
    public void CellEdit_UpdatesTheCacheAndRaisesChanged()
    {
        using var cache = new CompositeCache(_document, CompositingPolicy.Default);
        var changedCount = 0;
        cache.Changed += (_, _) => changedCount++;

        Write(BottomLayerId, new CellPoint(5, 3), 'Z');

        Assert.Equal(Cell.FromGlyph(new Rune('Z')), cache.GetCell(new CellPoint(5, 3)));
        Assert.Equal(1, changedCount);
        AssertMatchesTheDocument(cache);
    }

    [Fact]
    public void HidingALayer_RecompositesTheCellsItCovered()
    {
        Write(BottomLayerId, new CellPoint(1, 1), 'b');
        _history.Execute(new AddLayerCommand(1, new LayerProperties("Top")));
        var topLayerId = _document.ActiveFrame.Layers[1].Id;
        Write(topLayerId, new CellPoint(1, 1), 't');
        using var cache = new CompositeCache(_document, CompositingPolicy.Default);

        _history.Execute(new SetLayerPropertiesCommand(topLayerId, new LayerProperties("Top") { IsVisible = false }));

        Assert.Equal(Cell.FromGlyph(new Rune('b')), cache.GetCell(new CellPoint(1, 1)));
        AssertMatchesTheDocument(cache);
    }

    [Fact]
    public void UndoingALayerAdd_RecompositesEverything()
    {
        using var cache = new CompositeCache(_document, CompositingPolicy.Default);
        _history.Execute(new AddLayerCommand(1, new LayerProperties("Top")));
        Write(_document.ActiveFrame.Layers[1].Id, new CellPoint(0, 0), 'x');

        _history.Undo();
        _history.Undo();

        Assert.True(cache.GetCell(new CellPoint(0, 0)).IsEmpty);
        AssertMatchesTheDocument(cache);
    }

    [Fact]
    public void CanvasResize_ResizesTheCache()
    {
        Write(BottomLayerId, new CellPoint(0, 0), 'q');
        using var cache = new CompositeCache(_document, CompositingPolicy.Default);

        _history.Execute(new ResizeCanvasCommand(new CanvasSize(10, 2), ResizeAnchor.TopLeft));

        Assert.Equal(new CanvasSize(10, 2), cache.Size);
        Assert.Equal(10, cache.GetRow(1).Length);
        AssertMatchesTheDocument(cache);
    }

    [Fact]
    public void GetCell_OutsideTheCanvas_Throws()
    {
        using var cache = new CompositeCache(_document, CompositingPolicy.Default);

        Assert.Throws<ArgumentOutOfRangeException>(() => cache.GetCell(new CellPoint(6, 0)));
    }

    [Fact]
    public void Dispose_StopsFollowingTheDocument()
    {
        var cache = new CompositeCache(_document, CompositingPolicy.Default);
        var changedCount = 0;
        cache.Changed += (_, _) => changedCount++;

        cache.Dispose();
        Write(BottomLayerId, new CellPoint(0, 0), 'q');

        Assert.Equal(0, changedCount);
    }

    private Guid BottomLayerId => _document.ActiveFrame.Layers[0].Id;

    private void Write(Guid layerId, CellPoint point, char glyph)
    {
        var builder = new CellEditBuilder(_document, layerId);
        builder.Set(point, Cell.FromGlyph(new Rune(glyph)));
        _history.Execute(builder.Build()!);
    }

    private void AssertMatchesTheDocument(CompositeCache cache)
    {
        Assert.Equal(_document.CanvasSize, cache.Size);
        for (var y = 0; y < cache.Size.Height; y++)
        {
            for (var x = 0; x < cache.Size.Width; x++)
            {
                var point = new CellPoint(x, y);
                Assert.Equal(Compositor.CompositeCell(_document.ActiveFrame, point, CompositingPolicy.Default),
                    cache.GetCell(point));
            }
        }
    }
}
