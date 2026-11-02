// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Compositing;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Compositing;

public sealed class CompositorTests
{
    [Fact]
    public void TheTopmostOccupiedCellWins_AndEmptyCellsShowTheLayerBelow()
    {
        var document = CreateLayered(["abc", "def"], ["X··", "·Y·"]);

        Assert.Equal(["Xbc", "dYf"], CompositeAll(document));
    }

    [Fact]
    public void ASpace_HidesTheLayerBelowByDefault()
    {
        var document = CreateLayered(["abc"], ["· ·"]);

        Assert.Equal(["a c"], CompositeAll(document));
    }

    [Fact]
    public void ASpace_ShowsTheLayerBelow_WhenItsLayerTreatsSpacesAsEmpty()
    {
        var document = CreateLayered(["abc"], ["   "]);
        SetSpaceHandling(document.ActiveFrame.Layers[1], SpaceHandling.TreatedAsEmpty);

        Assert.Equal(["abc"], CompositeAll(document));
    }

    [Fact]
    public void TheSpaceOption_OnlyAffectsItsOwnLayer()
    {
        var document = CreateLayered(["aaa"], ["  b"], [" c "]);
        SetSpaceHandling(document.ActiveFrame.Layers[2], SpaceHandling.TreatedAsEmpty);

        // The top layer's spaces show the middle layer, whose own space still hides the bottom layer.
        Assert.Equal([" cb"], CompositeAll(document));
    }

    [Fact]
    public void ASpaceWithNothingBelow_IsEmpty_WhenSpacesAreTreatedAsEmpty()
    {
        var document = CreateLayered([" "]);

        Assert.Equal([" "], CompositeAll(document));

        SetSpaceHandling(document.ActiveFrame.Layers[0], SpaceHandling.TreatedAsEmpty);
        Assert.Equal(["·"], CompositeAll(document));
    }

    [Fact]
    public void HiddenLayers_AreSkipped()
    {
        var document = CreateLayered(["abc"], ["XYZ"]);
        var top = document.ActiveFrame.Layers[1];
        top.SetProperties(top.Properties with { IsVisible = false });

        Assert.Equal(["abc"], CompositeAll(document));
    }

    [Fact]
    public void WhenEveryLayerIsEmpty_TheResultIsEmpty()
    {
        var document = CreateLayered(["··"], ["··"]);

        Assert.Equal(["··"], CompositeAll(document));
    }

    [Fact]
    public void CompositeRegion_CompositesOnlyThatRegion()
    {
        var document = CreateLayered(["abcd", "efgh", "ijkl"], ["····", "··X·", "····"]);
        var destination = new Cell[4];

        Compositor.CompositeRegion(document.ActiveFrame, new CellRect(1, 1, 2, 2), destination);

        Assert.Equal(["fX", "jk"], TestCells.Describe(destination, 2));
    }

    [Fact]
    public void CompositeRegion_OverwritesWhateverTheDestinationHeld()
    {
        var document = CreateLayered(["··"]);
        var destination = new[] { TestCells.Glyph('?'), TestCells.Glyph('?') };

        Compositor.CompositeRegion(document.ActiveFrame, document.CanvasSize.Bounds, destination);

        Assert.Equal(["··"], TestCells.Describe(destination, 2));
    }

    [Fact]
    public void CompositeRegion_AgreesWithCompositeCell()
    {
        var random = new Random(42);
        var document = TestCells.CreateDocument(9, 7);
        TestCells.AddLayer(document);
        TestCells.AddLayer(document);
        var glyphs = new[] { Cell.Empty, Cell.Empty, TestCells.Glyph(' '), TestCells.Glyph('#'), TestCells.Glyph('.') };
        foreach (var layer in document.ActiveFrame.Layers)
        {
            for (var index = 0; index < document.CanvasSize.CellCount; index++)
                layer.SetCell(index, glyphs[random.Next(glyphs.Length)]);
        }

        var middle = document.ActiveFrame.Layers[1];
        middle.SetProperties(middle.Properties with { IsVisible = false });

        foreach (var spaceHandling in Enum.GetValues<SpaceHandling>())
        {
            SetSpaceHandling(document.ActiveFrame.Layers[^1], spaceHandling);
            var region = new CellRect(1, 2, 7, 4);
            var destination = new Cell[region.Area];
            Compositor.CompositeRegion(document.ActiveFrame, region, destination);

            for (var y = 0; y < region.Height; y++)
            {
                for (var x = 0; x < region.Width; x++)
                {
                    var expected =
                        Compositor.CompositeCell(document.ActiveFrame, new CellPoint(region.X + x, region.Y + y));
                    Assert.Equal(expected, destination[(y * region.Width) + x]);
                }
            }
        }
    }

    [Fact]
    public void CompositeRegion_RejectsARegionOutsideTheCanvasOrAShortDestination()
    {
        var document = TestCells.CreateDocument(3, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Compositor.CompositeRegion(document.ActiveFrame, new CellRect(2, 2, 2, 2), new Cell[4]));
        Assert.Throws<ArgumentException>(() =>
            Compositor.CompositeRegion(document.ActiveFrame, new CellRect(0, 0, 2, 2), new Cell[3]));
    }

    [Fact]
    public void CompositeCell_RejectsAPointOutsideTheCanvas()
    {
        var document = TestCells.CreateDocument(3, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Compositor.CompositeCell(document.ActiveFrame, new CellPoint(3, 0)));
    }

    [Fact]
    public void ThePolicy_OnlyTreatsU0020AsASpace()
    {
        var spacesAsEmpty = CompositingPolicy.For(SpaceHandling.TreatedAsEmpty);
        Assert.False(spacesAsEmpty.HidesLayersBelow(TestCells.Glyph(' ')));
        Assert.True(spacesAsEmpty.HidesLayersBelow(TestCells.Glyph(' ')));
        Assert.False(spacesAsEmpty.HidesLayersBelow(Cell.Empty));
        Assert.False(CompositingPolicy.For(SpaceHandling.HidesLayersBelow).HidesLayersBelow(Cell.Empty));
    }

    /// <summary>Creates a document with one layer per array, bottom first, painted with <see cref="TestCells.Paint"/>.</summary>
    private static Document CreateLayered(params string[][] layers)
    {
        var document = TestCells.CreateDocument(layers[0][0].Length, layers[0].Length);
        TestCells.Paint(document.ActiveFrame.Layers[0], layers[0]);
        foreach (var rows in layers.Skip(1))
            TestCells.Paint(TestCells.AddLayer(document), rows);

        return document;
    }

    private static void SetSpaceHandling(Layer layer, SpaceHandling spaceHandling) =>
        layer.SetProperties(layer.Properties with { SpaceHandling = spaceHandling });

    private static string[] CompositeAll(Document document)
    {
        var destination = new Cell[document.CanvasSize.CellCount];
        Compositor.CompositeRegion(document.ActiveFrame, document.CanvasSize.Bounds, destination);

        var perCell = TestCells.Describe(destination, document.CanvasSize.Width);
        for (var y = 0; y < document.CanvasSize.Height; y++)
        {
            for (var x = 0; x < document.CanvasSize.Width; x++)
            {
                var cell = Compositor.CompositeCell(document.ActiveFrame, new CellPoint(x, y));
                Assert.Equal(destination[(y * document.CanvasSize.Width) + x], cell);
            }
        }

        return perCell;
    }
}
