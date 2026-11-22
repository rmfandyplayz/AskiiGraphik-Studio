// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.Core.Tests.Clipboard;

public sealed class PastePlacementTests
{
    private static readonly CanvasSize _canvas = new(20, 10);
    private static readonly CanvasPoint _viewCentre = new(10, 5);

    // Two cells wide and three tall, copied from (4, 2), with something in its bottom-right cell only.
    private readonly CellClip _clip;

    public PastePlacementTests()
    {
        var document = TestCells.CreateDocument(20, 10);
        TestCells.Paint(document.ActiveFrame.Layers[0], "····", "····", "····", "····", "·····x");
        _clip = CellClip.Copy(document.ActiveFrame.Layers[0], SelectionMask.Rectangle(_canvas, new CellRect(4, 2, 2, 3)));
    }

    [Theory]
    [InlineData(7.5, 3.5, 7, 2)]
    [InlineData(7.9, 3.9, 7, 2)]
    [InlineData(7.1, 3.1, 6, 2)]
    public void OverTheCanvas_ItsMiddleGoesToThePointer(double x, double y, int left, int top)
    {
        Assert.Equal(new CellPoint(left, top), PastePlacement.TopLeft(_clip, _canvas, new CanvasPoint(x, y), _viewCentre));
    }

    [Fact]
    public void JustOffTheCanvas_ItStillGoesToThePointer_WhileACharacterLandsOnIt()
    {
        Assert.Equal(new CellPoint(-1, -2), PastePlacement.TopLeft(_clip, _canvas, new CanvasPoint(-0.4, -0.4), _viewCentre));
    }

    [Fact]
    public void TooFarOffTheCanvasForACharacterToLand_ItGoesWhereItWasCopiedFrom()
    {
        Assert.Equal(new CellPoint(4, 2), PastePlacement.TopLeft(_clip, _canvas, new CanvasPoint(-1.6, 5), _viewCentre));
    }

    [Fact]
    public void WithThePointerElsewhere_ItGoesWhereItWasCopiedFrom()
    {
        Assert.Equal(new CellPoint(4, 2), PastePlacement.TopLeft(_clip, _canvas, null, _viewCentre));
    }

    [Fact]
    public void TextFromAnotherApp_WithThePointerElsewhere_GoesInTheMiddleOfTheView()
    {
        var text = PastedText.ToClip("abc\ndef")!;

        Assert.Equal(new CellPoint(9, 4), PastePlacement.TopLeft(text, _canvas, null, _viewCentre));
    }

    [Fact]
    public void CopiedFromABiggerCanvas_OffThisOne_ItGoesInTheMiddleOfTheView()
    {
        var small = new CanvasSize(3, 2);

        Assert.Equal(new CellPoint(9, 4), PastePlacement.TopLeft(_clip, small, null, _viewCentre));
    }
}
