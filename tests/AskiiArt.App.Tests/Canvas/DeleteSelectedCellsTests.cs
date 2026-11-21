// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Actions;
using AskiiArt.App.Canvas;
using AskiiArt.App.Selection;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// Emptying the selected cells with the Delete and Backspace keys and the Edit menu's Delete, with the real canvas view
/// (ADR 0065).
/// </summary>
public sealed class DeleteSelectedCellsTests : IDisposable
{
    private static readonly Cell _ink = Cell.FromGlyph(new Rune('x'));

    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10));
    private readonly Window _window;
    private readonly CanvasControl _control;
    private readonly AppAction _delete;

    public DeleteSelectedCellsTests()
    {
        var view = new CanvasView { DataContext = _canvas.ViewModel };
        _window = new Window { Width = 800, Height = 600, Content = view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
        _control = view.FindControl<CanvasControl>("Canvas")!;
        view.FocusCanvas();
        Dispatcher.UIThread.RunJobs();
        _delete = new SelectionActions(_canvas.ActiveDocument).CreateActions()
            .Single(action => action.Id == SelectionActionIds.DeleteSelectedCells);

        // Four cells of ink in the top row, the middle two of them selected.
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        for (var x = 0; x < 4; x++)
            cells.Set(new CellPoint(x, 0), _ink);

        History.Execute(cells.Build(UndoStepNames.Typing)!);
        SelectionEdits.Select(Session, SelectionMask.Rectangle(_canvas.Document.CanvasSize, new CellRect(1, 0, 2, 1)),
            UndoStepNames.RectangleSelect);
    }

    private EditingSession Session => _canvas.Session;

    private CommandHistory History => Session.History;

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    [AvaloniaTheory]
    [InlineData(Key.Delete, PhysicalKey.Delete)]
    [InlineData(Key.Back, PhysicalKey.Backspace)]
    public void TheKey_EmptiesTheSelectedCells_WhenTheToolTakesNoText(Key key, PhysicalKey physicalKey)
    {
        Assert.IsType<HandTool>(_canvas.ViewModel.ActiveTool);
        var selection = Session.Selection.Mask;

        Press(key, physicalKey);

        Assert.Equal([true, false, false, true], InkInTopRow());
        Assert.Same(selection, Session.Selection.Mask);
        Assert.Equal(UndoStepNames.DeleteSelectedCells, History.GetStepNameKey(History.UndoCount - 1));
    }

    [AvaloniaTheory]
    [InlineData(Key.Delete, PhysicalKey.Delete)]
    [InlineData(Key.Back, PhysicalKey.Backspace)]
    public void WhileTheTextToolsCaretShows_TheKeyEditsTheText_NotTheSelection(Key key, PhysicalKey physicalKey)
    {
        _canvas.ViewModel.SelectTool(TextTool.Id);
        Click(new CellPoint(2, 0));
        Assert.IsType<TextTool>(_canvas.ViewModel.ActiveTool);

        Press(key, physicalKey);

        // The Text tool erased one cell by the caret, inside the selection, rather than every selected cell.
        Assert.Equal(1, InkInTopRow().Count(hasInk => !hasInk));
        Assert.Equal(UndoStepNames.Typing, History.GetStepNameKey(History.UndoCount - 1));
    }

    [AvaloniaFact]
    public void TheMenuItem_EmptiesTheSelectedCells_EvenWhileTheTextToolsCaretShows()
    {
        Click(new CellPoint(2, 0));

        Assert.True(_delete.Command.CanExecute(null));
        _delete.Command.Execute(null);

        Assert.Equal([true, false, false, true], InkInTopRow());
    }

    [AvaloniaFact]
    public void WithNothingSelected_TheKeyDoesNothing_AndTheMenuItemIsGreyedOut()
    {
        _canvas.ViewModel.TrySwitchToHandTool();
        var changes = 0;
        _delete.Command.CanExecuteChanged += (_, _) => changes++;

        SelectionEdits.Deselect(Session);
        var steps = History.UndoCount;
        Press(Key.Delete, PhysicalKey.Delete);

        Assert.True(changes > 0);
        Assert.False(_delete.Command.CanExecute(null));
        Assert.Equal([true, true, true, true], InkInTopRow());
        Assert.Equal(steps, History.UndoCount);
    }

    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void OnALockedOrHiddenLayer_TheKeyDoesNothing_AndTheMenuItemIsGreyedOut(bool isLocked, bool isHidden)
    {
        _canvas.ViewModel.TrySwitchToHandTool();
        Assert.True(_delete.Command.CanExecute(null));
        var changes = 0;
        _delete.Command.CanExecuteChanged += (_, _) => changes++;

        History.Execute(new SetLayerPropertiesCommand(Layer.Id,
            Layer.Properties with { IsLocked = isLocked, IsVisible = !isHidden }));
        var steps = History.UndoCount;
        Press(Key.Delete, PhysicalKey.Delete);

        Assert.True(changes > 0);
        Assert.False(_delete.Command.CanExecute(null));
        Assert.Equal([true, true, true, true], InkInTopRow());
        Assert.Equal(steps, History.UndoCount);
    }

    [AvaloniaFact]
    public void TheMenuItem_FollowsANewDocument()
    {
        var replacement = new EditingSession(Document.Create(new CanvasSize(20, 10), "Layer"));
        _canvas.ActiveDocument.Replace(replacement);
        Assert.False(_delete.Command.CanExecute(null));
        var changes = 0;
        _delete.Command.CanExecuteChanged += (_, _) => changes++;

        SelectionEdits.SelectAll(replacement);

        Assert.True(changes > 0);
        Assert.True(_delete.Command.CanExecute(null));
    }

    private bool[] InkInTopRow() =>
        [.. Enumerable.Range(0, 4).Select(x => !Layer.GetCell(new CellPoint(x, 0)).IsEmpty)];

    private void Click(CellPoint cell)
    {
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(CentreOf(cell), MouseButton.Left);
        _window.MouseUp(CentreOf(cell), MouseButton.Left);
    }

    private void Press(Key key, PhysicalKey physicalKey)
    {
        _window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Dispatcher.UIThread.RunJobs();
    }

    private Point CentreOf(CellPoint cell)
    {
        var inControl = _control.Viewport!.GetCellBounds(cell).ToRect(1).Center;
        return _control.TranslatePoint(inControl, _window)!.Value;
    }
}
