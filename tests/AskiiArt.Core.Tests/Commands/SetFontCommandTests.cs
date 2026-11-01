// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class SetFontCommandTests
{
    [Fact]
    public void ANewDocument_UsesTheDefaultFont()
    {
        Assert.Equal(FontReference.Default, TestCells.CreateDocument(2, 2).Font);
        Assert.Equal("JetBrains Mono", FontReference.Default.FamilyName);
    }

    [Fact]
    public void Apply_ChangesTheFont_KeepsTheGlyphs_AndRaisesASettingsChange()
    {
        var document = TestCells.CreateDocument(3, 1);
        TestCells.Paint(document.ActiveFrame.Layers[0], "abc");
        DocumentChangedEventArgs? change = null;
        document.Changed += (_, e) => change = e;

        new CommandHistory(document).Execute(new SetFontCommand(new FontReference("Fira Code")));

        Assert.Equal(new FontReference("Fira Code"), document.Font);
        Assert.Equal(["abc"], TestCells.Read(document.ActiveFrame.Layers[0]));
        Assert.NotNull(change);
        Assert.True(change.SettingsChanged);
    }

    [Fact]
    public void Undo_PutsTheOldFontBack()
    {
        var document = Document.Create(new CanvasSize(2, 2), "Layer", font: new FontReference("Roboto Mono"));
        var history = new CommandHistory(document);
        history.Execute(new SetFontCommand(new FontReference("Fira Code")));

        history.Undo();

        Assert.Equal(new FontReference("Roboto Mono"), document.Font);
    }

    [Fact]
    public void FontReferences_CompareWithoutCase()
    {
        Assert.Equal(new FontReference("Fira Code"), new FontReference("FIRA CODE"));
        Assert.Equal(new FontReference("Fira Code").GetHashCode(), new FontReference("fira code").GetHashCode());
        Assert.Throws<ArgumentException>(() => new FontReference(" "));
    }
}
