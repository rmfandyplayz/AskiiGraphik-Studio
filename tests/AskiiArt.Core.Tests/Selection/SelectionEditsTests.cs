// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tests.Commands;

namespace AskiiArt.Core.Tests.Selection;

/// <summary>Changing the selection through the session: <see cref="SelectionEdits"/> and its command.</summary>
public sealed class SelectionEditsTests
{
    private static readonly CanvasSize _size = new(4, 3);

    private readonly EditingSession _session = new(TestCells.CreateDocument(4, 3));
    private readonly List<SessionSelectionChangedEventArgs> _changes = [];

    public SelectionEditsTests()
    {
        _session.Selection.Changed += (_, e) => _changes.Add(e);
    }

    private CommandHistory History => _session.History;

    [Fact]
    public void ANewSession_SelectsNothing()
    {
        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(SelectionMask.Empty(_size), _session.Selection.Mask);
    }

    [Fact]
    public void Select_IsAnUndoStep_ThatRaisesChanged()
    {
        var mask = SelectionMask.Rectangle(_size, new CellRect(1, 1, 2, 1));

        Assert.True(SelectionEdits.Select(_session, mask, UndoStepNames.RectangleSelect));

        Assert.Same(mask, _session.Selection.Mask);
        Assert.Equal(UndoStepNames.RectangleSelect, History.GetStepNameKey(0));
        var change = Assert.Single(_changes);
        Assert.True(change.OldMask.IsEmpty);
        Assert.Same(mask, change.NewMask);

        History.Undo();
        Assert.True(_session.Selection.IsEmpty);

        History.Redo();
        Assert.Same(mask, _session.Selection.Mask);
        Assert.Equal(3, _changes.Count);
    }

    [Fact]
    public void Select_TheSameCellsAgain_MakesNoStep()
    {
        SelectionEdits.Select(_session, SelectionMask.Rectangle(_size, new CellRect(0, 0, 2, 2)), "Step");

        Assert.False(SelectionEdits.Select(_session, TestMasks.Parse("##··", "##··", "····"), "Step"));

        Assert.Equal(1, History.UndoCount);
        Assert.Single(_changes);
    }

    [Fact]
    public void SelectAll_SelectsTheWholeCanvas_AsAStepNamedSelectAll()
    {
        Assert.True(SelectionEdits.SelectAll(_session));

        Assert.Equal(SelectionMask.All(_size), _session.Selection.Mask);
        Assert.Equal(UndoStepNames.SelectAll, History.GetStepNameKey(0));
        Assert.False(SelectionEdits.SelectAll(_session));
    }

    [Fact]
    public void Deselect_SelectsNothing_AsAStepNamedDeselect()
    {
        SelectionEdits.SelectAll(_session);

        Assert.True(SelectionEdits.Deselect(_session));

        Assert.True(_session.Selection.IsEmpty);
        Assert.Equal(UndoStepNames.Deselect, History.GetStepNameKey(1));
    }

    [Fact]
    public void Deselect_WithNothingSelected_MakesNoStep()
    {
        Assert.False(SelectionEdits.Deselect(_session));

        Assert.False(History.CanUndo);
    }

    [Fact]
    public void SelectingAMaskForAnotherCanvasSize_Throws_AndChangesNothing()
    {
        var mask = SelectionMask.All(new CanvasSize(9, 9));

        Assert.Throws<ArgumentException>(() => SelectionEdits.Select(_session, mask, "Step"));

        Assert.True(_session.Selection.IsEmpty);
        Assert.False(History.CanUndo);
    }

    [Fact]
    public void TheCommand_DoesNotChangeTheDocument_AndCanOnlyBeRevertedOnceApplied()
    {
        var command = new SetSelectionCommand(_session.Selection, SelectionMask.All(_size), "Step");

        Assert.False(command.ChangesDocument);
        Assert.Throws<InvalidOperationException>(() => command.Revert(_session.Document));
    }

    [Fact]
    public void TheCommand_CountsOnlyItsOwnMask()
    {
        var small = new SetSelectionCommand(_session.Selection, SelectionMask.All(_size), "Step");
        var bigger = new SetSelectionCommand(_session.Selection, TestMasks.Parse("#·#·", "····", "#·#·"), "Step");

        Assert.True(bigger.EstimatedMemoryInBytes > small.EstimatedMemoryInBytes);
    }

    [Fact]
    public void SelectionSteps_DontMakeTheDocumentUnsaved()
    {
        SelectionEdits.SelectAll(_session);
        SelectionEdits.Deselect(_session);

        Assert.False(History.HasUnsavedChanges);
    }

    [Fact]
    public void AnEditThenASelection_IsUnsaved_UntilTheEditIsUndone()
    {
        History.Execute(TestCommands.Paint(_session.Document, _session.ActiveLayer.Id, "a"));
        SelectionEdits.SelectAll(_session);
        Assert.True(History.HasUnsavedChanges);

        History.Undo();
        Assert.True(History.HasUnsavedChanges);

        History.Undo();
        Assert.False(History.HasUnsavedChanges);
    }

    [Fact]
    public void UndoingASelectionMadeAfterASave_StaysSaved()
    {
        History.Execute(TestCommands.Paint(_session.Document, _session.ActiveLayer.Id, "a"));
        History.MarkSaved();
        SelectionEdits.SelectAll(_session);

        History.Undo();
        Assert.False(History.HasUnsavedChanges);

        History.Undo();
        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public void ReplacingAnUndoneSelection_AfterASave_StaysSaved()
    {
        History.Execute(TestCommands.Paint(_session.Document, _session.ActiveLayer.Id, "a"));
        SelectionEdits.SelectAll(_session);
        History.MarkSaved();
        History.Undo();

        SelectionEdits.Select(_session, SelectionMask.Rectangle(_size, new CellRect(0, 0, 1, 1)), "Step");

        Assert.False(History.HasUnsavedChanges);
        History.Undo();
        Assert.False(History.HasUnsavedChanges);
    }

    [Fact]
    public void ReplacingAnUndoneEdit_WithASelection_IsUnsaved()
    {
        History.Execute(TestCommands.Paint(_session.Document, _session.ActiveLayer.Id, "a"));
        History.MarkSaved();
        History.Undo();

        SelectionEdits.SelectAll(_session);

        Assert.True(History.HasUnsavedChanges);
    }

    [Fact]
    public void DroppingSelectionStepsForMemory_LeavesTheDocumentSaved()
    {
        var history = new CommandHistory(_session.Document, memoryBudgetInBytes: 1);

        history.Execute(new SetSelectionCommand(_session.Selection, SelectionMask.All(_size), "Step"));
        history.Execute(new SetSelectionCommand(_session.Selection, SelectionMask.Empty(_size), "Step"));
        history.Execute(new SetSelectionCommand(_session.Selection, SelectionMask.All(_size), "Step"));

        Assert.Equal(1, history.StepCount);
        Assert.False(history.HasUnsavedChanges);
    }

    [Fact]
    public void DroppingAnEditForMemory_LeavesTheDocumentUnsaved()
    {
        var history = new CommandHistory(_session.Document, memoryBudgetInBytes: 1);

        history.Execute(TestCommands.Paint(_session.Document, _session.ActiveLayer.Id, "a"));
        history.Execute(new SetSelectionCommand(_session.Selection, SelectionMask.All(_size), "Step"));

        Assert.Equal(1, history.StepCount);
        Assert.True(history.HasUnsavedChanges);
    }
}
