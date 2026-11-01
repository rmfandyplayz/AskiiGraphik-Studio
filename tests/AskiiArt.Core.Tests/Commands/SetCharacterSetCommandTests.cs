// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class SetCharacterSetCommandTests
{
    [Fact]
    public void SwitchingToAscii_EmptiesEveryNonAsciiCellInEveryLayer()
    {
        var document = TestCells.CreateDocument(4, 2);
        var bottom = document.ActiveFrame.Layers[0];
        var top = TestCells.AddLayer(document);
        TestCells.Paint(bottom, "aé─b", "····");
        TestCells.Paint(top, "····", "x·漢·");

        new CommandHistory(document).Execute(new SetCharacterSetCommand(CharacterSet.Ascii));

        Assert.Equal(CharacterSet.Ascii, document.CharacterSet);
        Assert.Equal(["a··b", "····"], TestCells.Read(bottom));
        Assert.Equal(["····", "x···"], TestCells.Read(top));
    }

    [Fact]
    public void Undo_RestoresTheCharacterSetAndTheRemovedGlyphs()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layer = document.ActiveFrame.Layers[0];
        TestCells.Paint(layer, "aé ");
        var history = new CommandHistory(document);
        history.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));

        history.Undo();

        Assert.Equal(CharacterSet.Unicode, document.CharacterSet);
        Assert.Equal(["aé "], TestCells.Read(layer));
    }

    [Fact]
    public void CountCellsToRemove_CountsNonAsciiCellsAcrossLayers()
    {
        var document = TestCells.CreateDocument(3, 1);
        TestCells.Paint(document.ActiveFrame.Layers[0], "aé·");
        TestCells.Paint(TestCells.AddLayer(document), "─··");

        Assert.Equal(2, SetCharacterSetCommand.CountCellsToRemove(document, CharacterSet.Ascii));
        Assert.Equal(0, SetCharacterSetCommand.CountCellsToRemove(document, CharacterSet.Unicode));
    }

    [Fact]
    public void SwitchingCharacterSet_RaisesOneChangeEventWithSettingsChanged()
    {
        var document = TestCells.CreateDocument(2, 1);
        TestCells.Paint(document.ActiveFrame.Layers[0], "éa");
        var events = new List<DocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);

        new CommandHistory(document).Execute(new SetCharacterSetCommand(CharacterSet.Ascii));

        var change = Assert.Single(events);
        Assert.True(change.SettingsChanged);
        Assert.Single(change.CellChanges);
    }
}
