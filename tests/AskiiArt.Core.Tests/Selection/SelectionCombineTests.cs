// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Selection;

public sealed class SelectionCombineTests
{
    // Wider than one 64-bit word, so runs cross word boundaries.
    private static readonly CanvasSize _size = new(70, 6);

    private static readonly string[] _kinds = ["empty", "all", "rectangle", "random", "patch"];

    public static TheoryData<SelectionCombineMode, string, string> Cases()
    {
        var cases = new TheoryData<SelectionCombineMode, string, string>();
        foreach (var mode in Enum.GetValues<SelectionCombineMode>())
        {
            foreach (var selection in _kinds)
            {
                foreach (var shape in _kinds)
                    cases.Add(mode, selection, shape);
            }
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CombinedWith_KeepsEachCellByTheMode(SelectionCombineMode mode, string selectionKind, string shapeKind)
    {
        var selection = Make(selectionKind, seed: 1);
        var shape = Make(shapeKind, seed: 2);

        var combined = selection.CombinedWith(shape, mode);

        var expected = new SelectionMaskBuilder(_size);
        for (var y = 0; y < _size.Height; y++)
        {
            for (var x = 0; x < _size.Width; x++)
            {
                var cell = new CellPoint(x, y);
                if (Keeps(mode, selection.Contains(cell), shape.Contains(cell)))
                    expected.Add(cell);
            }
        }

        var expectedMask = expected.Build();
        Assert.Equal(expectedMask, combined);
        Assert.Equal(expectedMask.Bounds, combined.Bounds);
    }

    [Fact]
    public void Replace_IsTheShape()
    {
        var shape = Make("patch", seed: 3);

        Assert.Same(shape, Make("random", seed: 4).CombinedWith(shape, SelectionCombineMode.Replace));
    }

    [Fact]
    public void AddingWhatARectangleAlreadyHolds_KeepsTheRectangle()
    {
        var selection = SelectionMask.Rectangle(_size, new CellRect(1, 1, 30, 4));
        var shape = SelectionMask.Rectangle(_size, new CellRect(2, 2, 5, 2));

        Assert.Same(selection, selection.CombinedWith(shape, SelectionCombineMode.Add));
    }

    [Fact]
    public void SubtractingSomethingElsewhere_KeepsTheSelection()
    {
        var selection = SelectionMask.Rectangle(_size, new CellRect(0, 0, 3, 3));
        var shape = SelectionMask.Rectangle(_size, new CellRect(10, 0, 3, 3));

        Assert.Same(selection, selection.CombinedWith(shape, SelectionCombineMode.Subtract));
    }

    [Fact]
    public void AShapeOnACanvasOfAnotherSize_Throws()
    {
        var selection = SelectionMask.All(_size);
        var shape = SelectionMask.All(new CanvasSize(5, 5));

        Assert.Throws<ArgumentException>(() => selection.CombinedWith(shape, SelectionCombineMode.Add));
    }

    private static bool Keeps(SelectionCombineMode mode, bool inSelection, bool inShape) => mode switch
    {
        SelectionCombineMode.Replace => inShape,
        SelectionCombineMode.Add => inSelection || inShape,
        SelectionCombineMode.Subtract => inSelection && !inShape,
        _ => inSelection && inShape
    };

    private static SelectionMask Make(string kind, int seed) => kind switch
    {
        "empty" => SelectionMask.Empty(_size),
        "all" => SelectionMask.All(_size),
        "rectangle" => SelectionMask.Rectangle(_size, new CellRect(seed * 7, 1, 40, 3)),
        "random" => TestMasks.Random(_size, seed),
        _ => Patch(seed)
    };

    // Cells scattered over one part of the canvas, so the bounds are small.
    private static SelectionMask Patch(int seed)
    {
        var area = new CellRect(30 + seed * 5, 2, 25, 4);
        var random = new Random(seed);
        var builder = new SelectionMaskBuilder(_size, area);
        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                if (random.Next(2) == 0)
                    builder.Add(new CellPoint(x, y));
            }
        }

        return builder.Build();
    }
}
