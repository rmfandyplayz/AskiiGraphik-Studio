// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Editing;
using AskiiArt.App.Selection;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;

namespace AskiiArt.App.Tests.Selection;

/// <summary>Which commands a selection past the canvas's edges leaves available (ADR 0071).</summary>
public sealed class SelectionPastCanvasActionsTests : IDisposable
{
    private static readonly CanvasSize _size = new(6, 3);

    private readonly TestCanvas _canvas = new(_size);
    private readonly Dictionary<string, AppAction> _actions;

    public SelectionPastCanvasActionsTests()
    {
        _actions = TestActions.CreateProviders(_canvas, new FakeAppShell()).SelectMany(provider =>
            provider.CreateActions()).ToDictionary(action => action.Id);
    }

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void WithTheSelectionAllPastTheCanvas_OnlyTheCommandsThatDontNeedItsCellsAreAvailable()
    {
        SelectMovedBy(new CellPoint(-3, 0));

        Assert.True(IsAvailable(SelectionActionIds.Deselect));
        Assert.True(IsAvailable(SelectionActionIds.SelectAll));
        Assert.True(IsAvailable(SelectionActionIds.Invert));
        Assert.True(IsAvailable(EditActionIds.Paste));
        Assert.False(IsAvailable(EditActionIds.Copy));
        Assert.False(IsAvailable(EditActionIds.Cut));
        Assert.False(IsAvailable(SelectionActionIds.DeleteSelectedCells));
        Assert.False(IsAvailable(SelectionActionIds.Grow));
        Assert.False(IsAvailable(SelectionActionIds.Shrink));
        Assert.False(IsAvailable(EditActionIds.CopyAsText));
        Assert.False(IsAvailable(EditActionIds.CopyAsAnsi));
    }

    [Fact]
    public void WithTheSelectionPartlyOnTheCanvas_EverythingIsAvailable()
    {
        SelectMovedBy(new CellPoint(-1, 0));

        Assert.True(IsAvailable(EditActionIds.Copy));
        Assert.True(IsAvailable(EditActionIds.Cut));
        Assert.True(IsAvailable(SelectionActionIds.DeleteSelectedCells));
        Assert.True(IsAvailable(SelectionActionIds.Grow));
        Assert.True(IsAvailable(EditActionIds.CopyAsText));
    }

    [Fact]
    public void CopyAsText_FollowsTheSelection_AndCopiesTheWholeCanvasWithNothingSelected()
    {
        SelectMovedBy(new CellPoint(-3, 0));
        Assert.False(IsAvailable(EditActionIds.CopyAsText));

        SelectionEdits.Deselect(_canvas.Session);

        Assert.True(IsAvailable(EditActionIds.CopyAsText));
        Assert.True(IsAvailable(EditActionIds.CopyAsAnsi));
    }

    private bool IsAvailable(string id) => _actions[id].Command.CanExecute(null);

    // The first two cells of the top row, moved as the Move tool would, so they can go past the canvas.
    private void SelectMovedBy(CellPoint offset)
    {
        var mask = SelectionMask.Rectangle(_size, new CellRect(0, 0, 2, 1))
            .MovedWithin(_size, offset, SelectionZone.Around(_size, 0.5));
        _canvas.Session.History.Execute(new SetSelectionCommand(_canvas.Session.Selection, mask, UndoStepNames.Move));
    }
}
