// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Clipboard;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tests.Selection;

namespace AskiiArt.Core.Tests.Clipboard;

public sealed class CellClipFormatTests
{
    [Fact]
    public void WhatsWritten_ReadsBack_WithItsCellsColoursShapeAndOrigin()
    {
        var document = TestCells.CreateDocument(4, 3, ColorMode.Rgb);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "abcd", "efgh", "ijkl");
        var colored = TestCells.Glyph('Z', TestCells.Rgb(10, 20, 30), TestCells.Rgb(200, 100, 0));
        layer.SetCell(new CellPoint(2, 1), colored);
        var clip = CellClip.Copy(layer, TestMasks.Parse("····", "·###", "··#·"));

        var read = CellClipFormat.TryRead(CellClipFormat.Write(clip, CharacterSet.Unicode, ColorMode.Rgb));

        Assert.NotNull(read);
        Assert.Equal(clip.Size, read.Size);
        Assert.Equal(clip.Origin, read.Origin);
        Assert.Equal(clip.Shape, read.Shape);
        Assert.Equal(colored, read.GetCell(new CellPoint(1, 0)));
        Assert.Equal(TestCells.Glyph('k'), read.GetCell(new CellPoint(1, 1)));
        Assert.True(read.GetCell(new CellPoint(0, 1)).IsEmpty);
    }

    [Fact]
    public void ATextClip_ReadsBack_WithNoOrigin()
    {
        var clip = PastedText.ToClip("a b")!;

        var read = CellClipFormat.TryRead(CellClipFormat.Write(clip, CharacterSet.Unicode, ColorMode.Monochrome));

        Assert.NotNull(read);
        Assert.Null(read.Origin);
        Assert.Equal("a b", read.ToText("\n"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("{}")]
    [InlineData("{\"version\":2,\"origin\":null,\"shape\":[],\"cells\":\"\"}")]
    [InlineData("{\"version\":1,\"origin\":null,\"shape\":[],\"cells\":\"not base64!\"}")]
    [InlineData("{\"version\":1,\"origin\":null,\"shape\":[],\"cells\":\"AAAA\"}")]
    [InlineData("{\"version\":\"one\"}")]
    public void AnythingElse_ReadsAsNothing(string text)
    {
        Assert.Null(CellClipFormat.TryRead(text));
    }
}
