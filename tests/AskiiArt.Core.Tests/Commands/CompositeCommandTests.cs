// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tests.Commands;

public sealed class CompositeCommandTests
{
    [Fact]
    public void ApplyAndRevert_RunTheCommandsInOrderAndInReverse()
    {
        var log = new List<string>();
        var composite = new CompositeCommand([new LoggingCommand("first", log), new LoggingCommand("second", log)]);
        var document = TestCells.CreateDocument(2, 2);

        composite.Apply(document);
        composite.Revert(document);

        Assert.Equal(["apply first", "apply second", "revert second", "revert first"], log);
    }

    [Fact]
    public void Constructor_RejectsAnEmptyList()
    {
        Assert.Throws<ArgumentException>(() => new CompositeCommand([]));
    }

    [Fact]
    public void Apply_WhenACommandThrows_RevertsTheOnesBeforeIt()
    {
        var log = new List<string>();
        var composite = new CompositeCommand(
            [new LoggingCommand("first", log), new LoggingCommand("second", log), new LoggingCommand("broken", log, throwOnApply: true)]);

        Assert.Throws<InvalidOperationException>(() => composite.Apply(TestCells.CreateDocument(2, 2)));

        Assert.Equal(["apply first", "apply second", "revert second", "revert first"], log);
    }

    [Fact]
    public void AsOneUndoStep_ItCanAddALayerAndChangeIt()
    {
        var document = TestCells.CreateDocument(2, 1);
        var history = new CommandHistory(document);
        var before = TestCommands.Snapshot(document);
        var addLayer = new AddLayerCommand(1, new LayerProperties("Pasted"));
        var rename = new SetLayerPropertiesCommand(addLayer.LayerId, new LayerProperties("Renamed"));

        history.Execute(new CompositeCommand([addLayer, rename]));

        Assert.Equal("Renamed", document.GetLayer(addLayer.LayerId).Properties.Name);
        history.Undo();
        Assert.Equal(before, TestCommands.Snapshot(document));
    }

    [Fact]
    public void EstimatedMemory_IncludesEveryCommand()
    {
        var composite = new CompositeCommand([new LoggingCommand("a", []), new LoggingCommand("b", [])]);

        Assert.True(composite.EstimatedMemoryInBytes >= 2 * LoggingCommand.Size);
    }

    private sealed class LoggingCommand(string name, List<string> log, bool throwOnApply = false) : IDocumentCommand
    {
        public const long Size = 10;

        public long EstimatedMemoryInBytes => Size;

        public void Apply(Document document)
        {
            if (throwOnApply)
                throw new InvalidOperationException("Broken on purpose.");

            log.Add($"apply {name}");
        }

        public void Revert(Document document) => log.Add($"revert {name}");
    }
}
