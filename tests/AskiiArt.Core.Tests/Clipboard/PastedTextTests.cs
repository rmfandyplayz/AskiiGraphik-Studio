// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tests.Selection;

namespace AskiiArt.Core.Tests.Clipboard;

public sealed class PastedTextTests
{
    [Fact]
    public void Lines_SplitAtEveryKindOfLineBreak()
    {
        Assert.Equal(["a", "b", "c", "d", "", "e"], Lines("a\r\nb\nc\rd\n\ne"));
    }

    [Fact]
    public void Lines_LeaveOutEmptyLinesAtTheEnd()
    {
        Assert.Equal(["a", "", "b"], Lines("a\n\nb\n\n\r\n"));
    }

    [Fact]
    public void Lines_TurnTabsIntoSpaces_ToTheNextMultipleOfEight()
    {
        Assert.Equal(["ab      c", "        d"], Lines("ab\tc\n\td"));
    }

    [Fact]
    public void Lines_LeaveOutEscapeCodesAndOtherControlCharacters()
    {
        Assert.Equal(["redplain", "linkok"], Lines("\u001b[31mred\u001b[0mplain\u0007\n\u001b]8;;https://a\u0007link\u001b]8;;\u001b\\ok"));
    }

    [Fact]
    public void ToClip_MakesARowPerLine_AsWideAsTheLongest_WithSpacesEmpty()
    {
        var clip = PastedText.ToClip("ab c\nd");

        Assert.NotNull(clip);
        Assert.Equal(new CanvasSize(4, 2), clip.Size);
        Assert.Null(clip.Origin);
        Assert.Equal(["####", "####"], TestMasks.Describe(clip.Shape));
        Assert.Equal(TestCells.Glyph('a'), clip.GetCell(new CellPoint(0, 0)));
        Assert.True(clip.GetCell(new CellPoint(2, 0)).IsEmpty);
        Assert.True(clip.GetCell(new CellPoint(1, 1)).IsEmpty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n \t\n")]
    [InlineData("\u001b[31m")]
    public void ToClip_OfTextWithNothingButSpaces_IsNull(string text)
    {
        Assert.Null(PastedText.ToClip(text));
    }

    private static string[] Lines(string text) =>
        [.. PastedText.Lines(text).Select(line => string.Concat(line.Select(rune => rune.ToString())))];
}
