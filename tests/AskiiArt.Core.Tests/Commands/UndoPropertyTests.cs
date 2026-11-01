// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using CsCheck;

namespace AskiiArt.Core.Tests.Commands;

/// <summary>
/// The key undo invariant (ARCHITECTURE §19): after any sequence of commands, undoing them one by
/// one passes back through every earlier state to the original document, and redoing them all
/// returns to the final one. CsCheck prints a seed when this fails, so the case can be replayed.
/// </summary>
public sealed class UndoPropertyTests
{
    private const int KindCount = 11;

    private static Cell[] Glyphs { get; } =
        [Cell.Empty, TestCells.Glyph(' '), TestCells.Glyph('a'), TestCells.Glyph('#'), TestCells.Glyph('é')];

    private static Gen<(int Kind, int A, int B, int C)> Step { get; } =
        Gen.Select(Gen.Int[0, KindCount - 1], Gen.Int[0, 1000], Gen.Int[0, 1000], Gen.Int[0, 1000]);

    [Fact]
    public void AnyCommandSequence_UndoesBackThroughEveryStateAndRedoesToTheEnd()
    {
        Step.Array[0, 40].Sample(steps =>
        {
            var document = TestCells.CreateDocument(5, 4);
            TestCells.Paint(document.ActiveFrame.Layers[0], "hello", "·····", "world");
            var history = new CommandHistory(document, memoryBudgetInBytes: long.MaxValue);
            var group = new CoalescingGroup();

            // statesByPosition[n] is the document after n undo steps; merged commands replace the last state.
            var statesByPosition = new List<string> { TestCommands.Snapshot(document) };
            foreach (var step in steps)
            {
                if (step.Kind == KindCount - 1)
                {
                    group = new CoalescingGroup();
                    continue;
                }

                var command = CreateCommand(document, step);
                if (command is null)
                    continue;

                var useGroup = step.Kind == 0 && step.C % 2 == 0;
                history.Execute(command, useGroup ? group : null);

                if (history.UndoCount == statesByPosition.Count)
                    statesByPosition.Add(TestCommands.Snapshot(document));
                else
                    statesByPosition[history.UndoCount] = TestCommands.Snapshot(document);
            }

            Assert.Equal(statesByPosition.Count - 1, history.UndoCount);
            var finalState = TestCommands.Snapshot(document);

            while (history.CanUndo)
            {
                history.Undo();
                Assert.Equal(statesByPosition[history.UndoCount], TestCommands.Snapshot(document));
            }

            Assert.False(history.HasUnsavedChanges);

            while (history.CanRedo)
                history.Redo();

            Assert.Equal(finalState, TestCommands.Snapshot(document));
        }, iter: 300);
    }

    /// <summary>Turns random numbers into a command that's valid for the document's current state, or null to skip.</summary>
    private static IDocumentCommand? CreateCommand(Document document, (int Kind, int A, int B, int C) step)
    {
        var layers = document.ActiveFrame.Layers;
        var layer = layers[step.A % layers.Count];

        return step.Kind switch
        {
            0 or 1 or 2 => CreateCellEdit(document, layer, step.B, step.C),
            3 => new AddLayerCommand(step.B % (layers.Count + 1), new LayerProperties($"Layer {step.C}")),
            4 when layers.Count > 1 => new RemoveLayerCommand(layer.Id),
            5 => new MoveLayerCommand(layer.Id, step.B % layers.Count),
            6 => new SetLayerPropertiesCommand(layer.Id, ChangeProperties(layer.Properties, step.B)),
            7 => new ResizeCanvasCommand(new CanvasSize(1 + (step.B % 7), 1 + (step.C % 6)), (ResizeAnchor)(step.A % 9)),
            8 => CreateComposite(document, layer, step),
            9 => new SetCharacterSetCommand((CharacterSet)(step.B % 2)),
            _ => null
        };
    }

    private static CellEditCommand? CreateCellEdit(Document document, Layer layer, int cellCountSeed, int seed)
    {
        var random = new Random(seed);
        var glyphs = Glyphs.Where(cell => cell.IsEmpty || document.CharacterSet.Allows(cell.Glyph)).ToArray();
        var builder = new CellEditBuilder(document, layer.Id);
        var cellCount = 1 + (cellCountSeed % 6);
        for (var index = 0; index < cellCount; index++)
        {
            var point = new CellPoint(random.Next(document.CanvasSize.Width), random.Next(document.CanvasSize.Height));
            builder.Set(point, glyphs[random.Next(glyphs.Length)]);
        }

        return builder.Build();
    }

    private static CompositeCommand CreateComposite(Document document, Layer layer, (int Kind, int A, int B, int C) step)
    {
        var rename = new SetLayerPropertiesCommand(layer.Id, ChangeProperties(layer.Properties, step.B));
        var edit = CreateCellEdit(document, layer, step.B, step.C);
        var move = new MoveLayerCommand(layer.Id, step.C % document.ActiveFrame.Layers.Count);

        return edit is null ? new CompositeCommand([rename, move]) : new CompositeCommand([rename, edit, move]);
    }

    private static LayerProperties ChangeProperties(LayerProperties properties, int choice) => (choice % 3) switch
    {
        0 => properties with { IsVisible = !properties.IsVisible },
        1 => properties with { IsLocked = !properties.IsLocked },
        _ => properties with { Name = properties.Name + "*" }
    };
}
