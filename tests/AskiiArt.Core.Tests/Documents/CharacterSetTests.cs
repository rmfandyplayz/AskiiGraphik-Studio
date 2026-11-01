// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Documents;

public sealed class CharacterSetTests
{
    [Theory]
    [InlineData(0x20)] // space
    [InlineData('A')]
    [InlineData('~')]
    [InlineData(0xE9)] // é
    [InlineData(0x2500)] // ─
    [InlineData(0x6F22)] // 漢
    [InlineData(0x1F600)] // 😀
    [InlineData(0xE000)] // private use, as in icon fonts
    public void Unicode_AllowsCharactersThatFillACell(int codePoint)
    {
        Assert.True(CharacterSet.Unicode.Allows(new Rune(codePoint)));
    }

    [Theory]
    [InlineData(0x09)] // tab
    [InlineData(0x0A)] // line feed
    [InlineData(0x7F)] // delete
    [InlineData(0x0301)] // combining acute accent
    [InlineData(0x200D)] // zero-width joiner
    [InlineData(0x200F)] // right-to-left mark
    [InlineData(0x2028)] // line separator
    public void Unicode_RejectsControlCharactersAndCharactersThatOnlyModifyOthers(int codePoint)
    {
        Assert.False(CharacterSet.Unicode.Allows(new Rune(codePoint)));
    }

    [Theory]
    [InlineData(0x20, true)]
    [InlineData('a', true)]
    [InlineData('~', true)]
    [InlineData(0x1F, false)]
    [InlineData(0x7F, false)]
    [InlineData(0xE9, false)]
    [InlineData(0x2500, false)]
    public void Ascii_AllowsPrintableAsciiOnly(int codePoint, bool allowed)
    {
        Assert.Equal(allowed, CharacterSet.Ascii.Allows(new Rune(codePoint)));
    }

    [Fact]
    public void NewDocument_IsUnicodeUnlessToldOtherwise()
    {
        Assert.Equal(CharacterSet.Unicode, TestCells.CreateDocument(2, 2).CharacterSet);
        Assert.Equal(CharacterSet.Ascii,
            Document.Create(new CanvasSize(2, 2), "Layer", CharacterSet.Ascii).CharacterSet);
    }

    [Fact]
    public void CellEditBuilder_RejectsAGlyphTheDocumentDoesNotAllow()
    {
        var document = Document.Create(new CanvasSize(2, 2), "Layer", CharacterSet.Ascii);
        var builder = new CellEditBuilder(document, document.ActiveFrame.Layers[0].Id);

        builder.Set(new CellPoint(0, 0), TestCells.Glyph('a'));
        builder.Set(new CellPoint(1, 0), Cell.Empty);

        Assert.Throws<ArgumentException>(() => builder.Set(new CellPoint(1, 1), TestCells.Glyph('é')));
    }
}
