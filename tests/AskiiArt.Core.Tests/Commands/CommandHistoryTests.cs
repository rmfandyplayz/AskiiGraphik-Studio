// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;

namespace AskiiArt.Core.Tests.Commands;

public sealed class CommandHistoryTests
{
    [Fact]
    public void ExecuteUndoAndRedo_MoveTheDocumentBetweenStates()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layer = document.ActiveFrame.Layers[0];
        var history = new CommandHistory(document);

        history.Execute(TestCommands.Paint(document, layer.Id, "abc"));
        Assert.Equal(["abc"], TestCells.Read(layer));
        Assert.Equal((1, 0), (history.UndoCount, history.RedoCount));

        history.Undo();
        Assert.Equal(["···"], TestCells.Read(layer));
        Assert.Equal((0, 1), (history.UndoCount, history.RedoCount));

        history.Redo();
        Assert.Equal(["abc"], TestCells.Read(layer));
        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void UndoOrRedo_WithNothingToDo_Throws()
    {
        var history = new CommandHistory(TestCells.CreateDocument(2, 2));

        Assert.Throws<InvalidOperationException>(history.Undo);
        Assert.Throws<InvalidOperationException>(history.Redo);
    }

    [Fact]
    public void Execute_AfterAnUndo_DropsTheRedoSteps()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        history.Execute(TestCommands.Paint(document, layerId, "a"));
        history.Execute(TestCommands.Paint(document, layerId, "~b"));
        history.Undo();

        history.Execute(TestCommands.Paint(document, layerId, "~~c"));

        Assert.Equal((2, 0), (history.UndoCount, history.RedoCount));
        Assert.Equal(["a·c"], TestCells.Read(document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void EachStep_RaisesExactlyOneDocumentChangedEvent()
    {
        var document = TestCells.CreateDocument(5, 2);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        var eventCount = 0;
        document.Changed += (_, _) => eventCount++;

        history.Execute(TestCommands.Paint(document, layerId, "hello", "world"));
        history.Undo();
        history.Redo();

        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void StateChanged_IsRaisedAfterEveryOperation()
    {
        var document = TestCells.CreateDocument(2, 1);
        var history = new CommandHistory(document);
        var raised = 0;
        history.StateChanged += (_, _) => raised++;

        history.Execute(TestCommands.Paint(document, document.ActiveFrame.Layers[0].Id, "x"));
        history.Undo();
        history.Redo();
        history.MarkSaved();

        Assert.Equal(4, raised);
    }

    [Fact]
    public void ACommandThatThrows_LeavesTheHistoryUnchanged()
    {
        var document = TestCells.CreateDocument(2, 2);
        var history = new CommandHistory(document);

        Assert.Throws<KeyNotFoundException>(() => history.Execute(new RemoveLayerCommand(Guid.NewGuid())));

        Assert.False(history.CanUndo);
        Assert.False(history.HasUnsavedChanges);
    }

    [Fact]
    public void UnsavedChanges_FollowTheSavedPosition()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        Assert.False(history.HasUnsavedChanges);

        history.Execute(TestCommands.Paint(document, layerId, "a"));
        Assert.True(history.HasUnsavedChanges);

        history.Undo();
        Assert.False(history.HasUnsavedChanges);

        history.Redo();
        history.MarkSaved();
        Assert.False(history.HasUnsavedChanges);

        history.Execute(TestCommands.Paint(document, layerId, "~b"));
        Assert.True(history.HasUnsavedChanges);

        history.Undo();
        Assert.False(history.HasUnsavedChanges);

        history.Undo();
        Assert.True(history.HasUnsavedChanges);
    }

    [Fact]
    public void UnsavedChanges_StayTrueForever_WhenTheSavedStepWasDiscarded()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        history.Execute(TestCommands.Paint(document, layerId, "a"));
        history.MarkSaved();
        history.Undo();

        // Executing drops the redo step that led to the saved state.
        history.Execute(TestCommands.Paint(document, layerId, "~b"));
        history.Undo();

        Assert.True(history.HasUnsavedChanges);
    }

    [Fact]
    public void OverBudget_TheOldestStepsAreDropped()
    {
        var document = TestCells.CreateDocument(10, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var stepSize = TestCommands.Paint(document, layerId, "a").EstimatedMemoryInBytes;
        var history = new CommandHistory(document, memoryBudgetInBytes: stepSize * 2);

        history.Execute(TestCommands.Paint(document, layerId, "a"));
        history.Execute(TestCommands.Paint(document, layerId, "~b"));
        history.Execute(TestCommands.Paint(document, layerId, "~~c"));

        Assert.Equal(2, history.UndoCount);
        Assert.Equal(stepSize * 2, history.MemoryInBytes);

        history.Undo();
        history.Undo();
        Assert.False(history.CanUndo);
        Assert.Equal(["a·········"], TestCells.Read(document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void OverBudget_TheNewestStepIsKeptEvenWhenItAloneIsTooBig()
    {
        var document = TestCells.CreateDocument(10, 1);
        var history = new CommandHistory(document, memoryBudgetInBytes: 1);

        history.Execute(TestCommands.Paint(document, document.ActiveFrame.Layers[0].Id, "abc"));

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Equal(["··········"], TestCells.Read(document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void OverBudget_DroppingTheSavedStateMakesItUnreachable()
    {
        var document = TestCells.CreateDocument(10, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document, memoryBudgetInBytes: 1);

        history.Execute(TestCommands.Paint(document, layerId, "a"));
        history.Execute(TestCommands.Paint(document, layerId, "~b"));
        history.Undo();

        Assert.True(history.HasUnsavedChanges);
    }

    [Fact]
    public void Coalescing_MergesCommandsOfTheSameGroupIntoOneStep()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        var typing = new CoalescingGroup();

        history.Execute(TestCommands.Paint(document, layerId, "a"), typing);
        history.Execute(TestCommands.Paint(document, layerId, "~b"), typing);
        history.Execute(TestCommands.Paint(document, layerId, "~~c"), typing);

        Assert.Equal(1, history.UndoCount);
        history.Undo();
        Assert.Equal(["···"], TestCells.Read(document.ActiveFrame.Layers[0]));
        history.Redo();
        Assert.Equal(["abc"], TestCells.Read(document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void Coalescing_DoesNotMergeAcrossGroupsOrWithoutAGroup()
    {
        var document = TestCells.CreateDocument(4, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);

        history.Execute(TestCommands.Paint(document, layerId, "a"), new CoalescingGroup());
        history.Execute(TestCommands.Paint(document, layerId, "~b"), new CoalescingGroup());
        history.Execute(TestCommands.Paint(document, layerId, "~~c"));
        history.Execute(TestCommands.Paint(document, layerId, "~~~d"));

        Assert.Equal(4, history.UndoCount);
    }

    [Fact]
    public void Coalescing_StopsAfterUndoRedoOrSave()
    {
        var document = TestCells.CreateDocument(6, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        var typing = new CoalescingGroup();

        history.Execute(TestCommands.Paint(document, layerId, "a"), typing);
        history.MarkSaved();
        history.Execute(TestCommands.Paint(document, layerId, "~b"), typing);
        Assert.Equal(2, history.UndoCount);
        Assert.True(history.HasUnsavedChanges);

        history.Undo();
        history.Execute(TestCommands.Paint(document, layerId, "~~c"), typing);
        Assert.Equal(2, history.UndoCount);

        history.Undo();
        history.Redo();
        history.Execute(TestCommands.Paint(document, layerId, "~~~d"), typing);
        Assert.Equal(3, history.UndoCount);
    }

    [Fact]
    public void Coalescing_KeepsSeparateStepsWhenTheCommandsCantMerge()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layer = document.ActiveFrame.Layers[0];
        var history = new CommandHistory(document);
        var group = new CoalescingGroup();

        history.Execute(TestCommands.Paint(document, layer.Id, "a"), group);
        history.Execute(new SetLayerPropertiesCommand(layer.Id, layer.Properties with { IsLocked = true }), group);

        Assert.Equal(2, history.UndoCount);
    }

    [Fact]
    public void Coalescing_UpdatesTheMemoryTotal()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        var typing = new CoalescingGroup();

        history.Execute(TestCommands.Paint(document, layerId, "a"), typing);
        var oneCell = history.MemoryInBytes;
        history.Execute(TestCommands.Paint(document, layerId, "~b"), typing);

        Assert.True(history.MemoryInBytes > oneCell);
        Assert.Equal(TestCommands.Paint(document, layerId, "xy").EstimatedMemoryInBytes, history.MemoryInBytes);
    }

    [Fact]
    public void MarkSavedWithASavePoint_KeepsEditsMadeDuringTheSaveUnsaved()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        var group = new CoalescingGroup();
        history.Execute(TestCommands.Paint(document, layerId, "a"), group);

        var savePoint = history.BeginSave();
        history.Execute(TestCommands.Paint(document, layerId, "~b"), group);
        history.MarkSaved(savePoint);

        Assert.True(history.HasUnsavedChanges);

        // The edit was a step of its own rather than merged into the saved one, so undoing it
        // returns to exactly what was saved.
        history.Undo();
        Assert.False(history.HasUnsavedChanges);
        Assert.Equal(["a··"], TestCells.Read(document.ActiveFrame.Layers[0]));
    }

    [Fact]
    public void MarkSavedWithASavePoint_WithoutEditsInBetween_LeavesNothingUnsaved()
    {
        var document = TestCells.CreateDocument(3, 1);
        var history = new CommandHistory(document);
        history.Execute(TestCommands.Paint(document, document.ActiveFrame.Layers[0].Id, "a"));

        history.MarkSaved(history.BeginSave());

        Assert.False(history.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSavedWithASavePoint_WhoseStepWasReplaced_LeavesTheDocumentUnsaved()
    {
        var document = TestCells.CreateDocument(3, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var history = new CommandHistory(document);
        history.Execute(TestCommands.Paint(document, layerId, "a"));

        var savePoint = history.BeginSave();
        history.Undo();
        history.Execute(TestCommands.Paint(document, layerId, "b"));
        history.MarkSaved(savePoint);

        Assert.True(history.HasUnsavedChanges);
        history.Undo();
        Assert.True(history.HasUnsavedChanges);
    }

    [Fact]
    public void MarkSavedWithASavePoint_FollowsStepsDroppedToSaveMemory()
    {
        var document = TestCells.CreateDocument(10, 1);
        var layerId = document.ActiveFrame.Layers[0].Id;
        var stepSize = TestCommands.Paint(document, layerId, "a").EstimatedMemoryInBytes;
        var history = new CommandHistory(document, memoryBudgetInBytes: stepSize * 2);
        history.Execute(TestCommands.Paint(document, layerId, "a"));
        history.Execute(TestCommands.Paint(document, layerId, "~b"));

        var savePoint = history.BeginSave();
        history.Execute(TestCommands.Paint(document, layerId, "~~c"));
        history.MarkSaved(savePoint);

        Assert.True(history.HasUnsavedChanges);
        history.Undo();
        Assert.False(history.HasUnsavedChanges);
    }
}
