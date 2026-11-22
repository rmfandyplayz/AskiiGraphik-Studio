// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Resources;
using AskiiArt.App.Shell.Menus;
using AskiiArt.App.Shell.Toolbars;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>Choosing a tool by action or letter key, with the real canvas view.</summary>
public sealed class ToolSwitchingTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10));
    private readonly Window _window;
    private readonly CanvasControl _control;

    public ToolSwitchingTests()
    {
        var view = new CanvasView { DataContext = _canvas.ViewModel };
        _window = new Window { Width = 800, Height = 600, Content = view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
        _control = view.FindControl<CanvasControl>("Canvas")!;
        view.FocusCanvas();
        Dispatcher.UIThread.RunJobs();
    }

    private CanvasViewModel Canvas => _canvas.ViewModel;

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void TheHandTool_IsActiveAtFirst()
    {
        Assert.Equal(HandTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void EachTool_HasAnAction_CheckedWhenItsActive()
    {
        var actions = new ToolActions(Canvas).CreateActions().ToDictionary(action => action.Id);
        var hand = actions[ToolActionIds.Hand];
        var eraser = actions[ToolActionIds.Eraser];
        Assert.True(hand.CheckedState!.IsChecked);
        Assert.False(eraser.CheckedState!.IsChecked);

        eraser.Command.Execute(null);

        Assert.Equal(EraserTool.Id, Canvas.ActiveTool.Descriptor.Id);
        Assert.False(hand.CheckedState!.IsChecked);
        Assert.True(eraser.CheckedState!.IsChecked);
    }

    [AvaloniaFact]
    public void EachToolGroup_HasAnAction_CheckedWhileOneOfItsToolsIsActive()
    {
        var actions = new ToolActions(Canvas).CreateActions().ToDictionary(action => action.Id);
        var brushGroup = actions[ToolGroupActionIds.Brush];
        var brushGroupChanges = 0;
        brushGroup.CheckedState!.Changed += (_, _) => brushGroupChanges++;

        Canvas.SelectTool(ColorBrushTool.BackgroundId);
        Assert.True(brushGroup.CheckedState.IsChecked);
        Assert.Equal(1, brushGroupChanges);

        actions[ToolGroupActionIds.Hand].Command.Execute(null);
        Assert.False(brushGroup.CheckedState.IsChecked);

        brushGroup.Command.Execute(null);
        Assert.Equal(ColorBrushTool.BackgroundId, Canvas.ActiveTool.Descriptor.Id);
        Assert.Equal(
            [
                ToolGroupActionIds.Hand, ToolGroupActionIds.Move, ToolGroupActionIds.Select, ToolGroupActionIds.Text,
                ToolGroupActionIds.Brush, ToolGroupActionIds.Eraser
            ],
            actions.Keys.Where(id => id.StartsWith("toolGroup.", StringComparison.Ordinal)));
    }

    [AvaloniaFact]
    public void TheToolsToolbar_HasAButtonForEachToolGroup_InOrder()
    {
        var groupActions = new ToolActions(Canvas).CreateActions()
            .Select(action => action.Id)
            .Where(id => id.StartsWith("toolGroup.", StringComparison.Ordinal));

        Assert.Equal(groupActions, ToolbarLayout.CreateTools().ActionIds());
    }

    [AvaloniaFact]
    public void TheToolsMenu_HasAnItemForEachTool_InOrder()
    {
        var toolActions = new ToolActions(Canvas).CreateActions()
            .Select(action => action.Id)
            .Where(id => id.StartsWith("tool.", StringComparison.Ordinal));
        var toolsMenu = MenuLayout.CreateDefault(isMacOS: false, includeDebugMenu: false).WindowMenus
            .Single(menu => menu.LabelKey == nameof(Strings.MenuTools));

        Assert.Equal(toolActions, toolsMenu.Entries.OfType<ActionMenuEntry>().Select(entry => entry.ActionId));
    }

    [AvaloniaFact]
    public void ChoosingAGroup_ChoosesItsFirstTool_UntilAnotherIsChosen()
    {
        Canvas.SelectToolGroup(BuiltInToolGroups.Brush.Id);
        Assert.Equal(BrushTool.Id, Canvas.ActiveTool.Descriptor.Id);
        Assert.Same(BuiltInToolGroups.Brush, Canvas.SelectedGroup);

        Canvas.SelectTool(ColorBrushTool.ForegroundId);
        Canvas.SelectTool(HandTool.Id);
        Canvas.SelectToolGroup(BuiltInToolGroups.Brush.Id);

        Assert.Equal(ColorBrushTool.ForegroundId, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void ChoosingAnotherToolOfTheGroup_DoesntSayTheGroupChanged()
    {
        Canvas.SelectTool(BrushTool.Id);
        var changes = new List<string?>();
        Canvas.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        Canvas.SelectTool(ColorBrushTool.ForegroundId);

        Assert.DoesNotContain(nameof(CanvasViewModel.SelectedGroup), changes);
        Assert.Contains(nameof(CanvasViewModel.ActiveTool), changes);
    }

    [AvaloniaFact]
    public void ChoosingTheActiveToolAgain_KeepsItsState()
    {
        var actions = new ToolActions(Canvas).CreateActions().ToDictionary(action => action.Id);
        var notified = 0;
        actions[ToolActionIds.Hand].CheckedState!.Changed += (_, _) => notified++;
        var tool = Canvas.ActiveTool;

        actions[ToolActionIds.Hand].Command.Execute(null);

        Assert.Same(tool, Canvas.ActiveTool);
        Assert.Equal(1, notified);
    }

    [AvaloniaFact]
    public void SwitchingTools_RemovesTheCaret()
    {
        Canvas.SelectTool(TextTool.Id);
        Click(new CellPoint(2, 1));
        var text = Assert.IsType<TextTool>(Canvas.ActiveTool);
        Assert.NotNull(text.Caret);

        Canvas.SelectTool(EraserTool.Id);

        Assert.Null(text.Caret);
    }

    [AvaloniaFact]
    public void AChosenTool_StaysChosen_WhenAnotherDocumentReplacesThisOne()
    {
        Canvas.SelectTool(EraserTool.Id);

        var session = new EditingSession(Document.Create(new CanvasSize(5, 5), "Layer"));
        _canvas.ActiveDocument.Replace(session);

        Assert.IsType<EraserTool>(Canvas.ActiveTool);
        Dispatcher.UIThread.RunJobs();
        Assert.Same(Canvas.ActiveTool, _control.Tool);
    }

    [AvaloniaFact]
    public void LetterKeys_ChooseTools_WhileNoCaretIsPlaced()
    {
        PressLetter(Key.E, PhysicalKey.E, "e");
        Assert.Equal(EraserTool.Id, Canvas.ActiveTool.Descriptor.Id);

        PressLetter(Key.T, PhysicalKey.T, "t");
        Assert.Equal(TextTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void LetterKeys_TypeText_WhileACaretIsPlaced_AndChooseToolsAfterEsc()
    {
        Canvas.SelectTool(TextTool.Id);
        Click(new CellPoint(2, 1));

        PressLetter(Key.E, PhysicalKey.E, "e");
        _window.KeyTextInput("e");

        Assert.Equal(TextTool.Id, Canvas.ActiveTool.Descriptor.Id);
        Assert.False(Layer.GetCell(new CellPoint(2, 1)).IsEmpty);

        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        PressLetter(Key.E, PhysicalKey.E, "e");

        Assert.Equal(EraserTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void Esc_SwitchesToTheHandTool_StraightFromTyping()
    {
        Canvas.SelectTool(TextTool.Id);
        Click(new CellPoint(2, 1));
        var text = Assert.IsType<TextTool>(Canvas.ActiveTool);

        PressEscape();

        Assert.IsType<HandTool>(Canvas.ActiveTool);
        Assert.Null(text.Caret);
        Assert.Same(Canvas.ActiveTool, _control.Tool);
    }

    [AvaloniaFact]
    public void Esc_DuringAnEraserStroke_OnlyCancelsIt_AndTheNextEscSwitchesToTheHandTool()
    {
        var history = _canvas.Session.History;
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        cells.Set(new CellPoint(1, 1), Cell.FromGlyph(new Rune('a')));
        history.Execute(cells.Build()!);
        Canvas.SelectTool(EraserTool.Id);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CentreOf(new CellPoint(1, 1)), MouseButton.Left);
        PressEscape();

        Assert.IsType<EraserTool>(Canvas.ActiveTool);
        Assert.True(Layer.GetCell(new CellPoint(1, 1)).HasGlyph);

        _window.MouseUp(CentreOf(new CellPoint(1, 1)), MouseButton.Left);
        PressEscape();

        Assert.IsType<HandTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void Esc_WithTheHandTool_KeepsIt()
    {
        Canvas.SelectTool(HandTool.Id);
        var hand = Canvas.ActiveTool;

        PressEscape();

        Assert.Same(hand, Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void V_ChoosesTheMoveTool_AndHChoosesNothing()
    {
        PressLetter(Key.V, PhysicalKey.V, "v");
        Assert.IsType<MoveTool>(Canvas.ActiveTool);

        PressLetter(Key.H, PhysicalKey.H, "h");
        Assert.IsType<MoveTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void Esc_WithTheMoveTool_PutsADragBack_AndOtherwiseSwitchesToTheHandTool_LeavingTheCellsMoved()
    {
        Canvas.SelectTool(MoveTool.Id);
        Dispatcher.UIThread.RunJobs();
        SelectionEdits.Select(_canvas.Session,
            SelectionMask.Rectangle(_canvas.Document.CanvasSize, new CellRect(0, 0, 1, 1)),
            UndoStepNames.RectangleSelect);
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        cells.Set(new CellPoint(0, 0), Cell.FromGlyph(new Rune('a')));
        _canvas.Session.History.Execute(cells.Build()!);
        var move = Assert.IsType<MoveTool>(Canvas.ActiveTool);
        move.KeyPressed(ToolKey.Right, ToolModifiers.None);

        _window.MouseDown(CentreOf(new CellPoint(1, 0)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(3, 0)));
        Assert.Equal(new Rune('a'), Layer.GetCell(new CellPoint(3, 0)).Glyph);
        PressEscape();
        _window.MouseUp(CentreOf(new CellPoint(3, 0)), MouseButton.Left);
        Assert.Equal(new Rune('a'), Layer.GetCell(new CellPoint(1, 0)).Glyph);
        Assert.Same(move, Canvas.ActiveTool);

        PressEscape();
        Assert.IsType<HandTool>(Canvas.ActiveTool);
        Assert.Equal(new Rune('a'), Layer.GetCell(new CellPoint(1, 0)).Glyph);
    }

    [AvaloniaFact]
    public void TheMKey_ChoosesRectangleSelect_WhichSelectsByDragging_AsOneStep()
    {
        PressLetter(Key.M, PhysicalKey.M, "m");
        Assert.IsType<RectangleSelectTool>(Canvas.ActiveTool);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CentreOf(new CellPoint(1, 1)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(3, 2)));
        Assert.True(_canvas.Session.Selection.IsEmpty);
        _window.MouseUp(CentreOf(new CellPoint(3, 2)), MouseButton.Left);

        var expected = SelectionMask.Rectangle(_canvas.Document.CanvasSize, CellRect.FromEdges(1, 1, 4, 3));
        Assert.Equal(expected, _canvas.Session.Selection.Mask);
        Assert.Equal(1, _canvas.Session.History.UndoCount);
        Assert.Same(_canvas.Session.Selection, _control.Selection);
    }

    [AvaloniaFact]
    public void TheWKey_ChoosesTheMagicWand_AndSoDoMThen5()
    {
        PressLetter(Key.W, PhysicalKey.W, "w");

        Assert.IsType<MagicWandTool>(Canvas.ActiveTool);

        PressLetter(Key.V, PhysicalKey.V, "v");
        PressLetter(Key.M, PhysicalKey.M, "m");
        Assert.IsType<MagicWandTool>(Canvas.ActiveTool);

        Canvas.SelectTool(RectangleSelectTool.Id);
        _window.KeyPress(Key.D5, RawInputModifiers.None, PhysicalKey.Digit5, "5");
        _window.KeyTextInput("5");
        _window.KeyRelease(Key.D5, RawInputModifiers.None, PhysicalKey.Digit5, "5");
        Dispatcher.UIThread.RunJobs();

        Assert.IsType<MagicWandTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void DraggingSidewaysWithTheMagicWand_RaisesTheTolerance_ByOneEveryTwoPixels()
    {
        var tolerance = _canvas.ToolSettings.For(MagicWandOptions.Group);
        tolerance.Set(MagicWandOptions.Tolerance, 0);
        Canvas.SelectTool(MagicWandTool.Id);
        Dispatcher.UIThread.RunJobs();
        var cellWidth = _control.Viewport!.CellSize.Width;

        _window.MouseDown(CentreOf(new CellPoint(1, 1)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(2, 1)));

        var expected = Math.Min(100, (int)(cellWidth / MagicWandTool.PixelsPerToleranceStep));
        Assert.Equal(expected, tolerance.Get(MagicWandOptions.Tolerance));

        _window.MouseUp(CentreOf(new CellPoint(2, 1)), MouseButton.Left);

        Assert.Equal(SelectionMask.All(_canvas.Document.CanvasSize), _canvas.Session.Selection.Mask);
        Assert.Equal(expected, tolerance.Get(MagicWandOptions.Tolerance));
    }

    [AvaloniaFact]
    public void Esc_DuringASelectionDrag_OnlyCancelsIt_AndTheNextEscSwitchesToTheHandTool()
    {
        Canvas.SelectTool(RectangleSelectTool.Id);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CentreOf(new CellPoint(1, 1)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(3, 2)));
        PressEscape();
        _window.MouseUp(CentreOf(new CellPoint(3, 2)), MouseButton.Left);

        Assert.IsType<RectangleSelectTool>(Canvas.ActiveTool);
        Assert.True(_canvas.Session.Selection.IsEmpty);

        PressEscape();

        Assert.IsType<HandTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void TheCanvas_FollowsTheSelectionOfANewDocument()
    {
        SelectionEdits.SelectAll(_canvas.Session);
        var next = new EditingSession(Document.Create(new CanvasSize(5, 5), "Layer"));

        _canvas.ActiveDocument.Replace(next);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(next.Selection, _control.Selection);
        Assert.True(_control.Selection!.IsEmpty);
    }

    [AvaloniaFact]
    public void X_SwapsTheColors_AndD_SetsBothToNone()
    {
        var red = CellColor.FromAnsi(AnsiColor.Red);
        _canvas.Colors.Foreground = red;

        PressLetter(Key.X, PhysicalKey.X, "x");

        Assert.True(_canvas.Colors.Foreground.IsUnset);
        Assert.Equal(red, _canvas.Colors.Background);

        PressLetter(Key.D, PhysicalKey.D, "d");

        Assert.True(_canvas.Colors.Background.IsUnset);
    }

    [AvaloniaFact]
    public void XAndD_AreTyped_WhileACaretIsPlaced()
    {
        var red = CellColor.FromAnsi(AnsiColor.Red);
        _canvas.Colors.Foreground = red;
        Canvas.SelectTool(TextTool.Id);
        Click(new CellPoint(2, 1));

        PressLetter(Key.X, PhysicalKey.X, "x");
        PressLetter(Key.D, PhysicalKey.D, "d");

        Assert.Equal(red, _canvas.Colors.Foreground);
        Assert.Equal(TextTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void ALetterWithAModifier_DoesntChooseATool()
    {
        _window.KeyPress(Key.E, RawInputModifiers.Control, PhysicalKey.E, "e");

        Assert.Equal(HandTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void DraggingWithTheEraser_ErasesThroughTheCanvas_AsOneStep()
    {
        Canvas.SelectTool(EraserTool.Id);
        Dispatcher.UIThread.RunJobs();
        var history = _canvas.Session.History;
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        for (var x = 0; x < 6; x++)
            cells.Set(new CellPoint(x, 1), Cell.FromGlyph(new Rune('a')));

        history.Execute(cells.Build()!);
        var stepsBefore = history.UndoCount;

        _window.MouseDown(CentreOf(new CellPoint(0, 1)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(4, 1)));
        _window.MouseUp(CentreOf(new CellPoint(4, 1)), MouseButton.Left);

        for (var x = 0; x < 5; x++)
            Assert.True(Layer.GetCell(new CellPoint(x, 1)).IsEmpty);

        Assert.False(Layer.GetCell(new CellPoint(5, 1)).IsEmpty);
        Assert.Equal(stepsBefore + 1, history.UndoCount);
    }

    [AvaloniaFact]
    public void DraggingAndNudgingWithTheMoveTool_MovesTheSelectedCellsThroughTheCanvas_AStepEach()
    {
        Canvas.SelectTool(MoveTool.Id);
        Dispatcher.UIThread.RunJobs();
        var history = _canvas.Session.History;
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        cells.Set(new CellPoint(1, 1), Cell.FromGlyph(new Rune('a')));
        cells.Set(new CellPoint(3, 1), Cell.FromGlyph(new Rune('b')));
        history.Execute(cells.Build()!);
        SelectionEdits.Select(_canvas.Session,
            SelectionMask.Rectangle(_canvas.Document.CanvasSize, new CellRect(1, 1, 1, 1)),
            UndoStepNames.RectangleSelect);
        var stepsBefore = history.UndoCount;

        _window.MouseDown(CentreOf(new CellPoint(8, 5)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(10, 5)));
        _window.MouseUp(CentreOf(new CellPoint(10, 5)), MouseButton.Left);
        _window.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.ArrowRight, null);

        Assert.True(Layer.GetCell(new CellPoint(1, 1)).IsEmpty);
        Assert.Equal(new Rune('a'), Layer.GetCell(new CellPoint(13, 1)).Glyph);
        Assert.Equal(new Rune('b'), Layer.GetCell(new CellPoint(3, 1)).Glyph);
        Assert.True(_canvas.Session.Selection.Mask.Contains(new CellPoint(13, 1)));
        Assert.Equal(stepsBefore + 2, history.UndoCount);
    }

    [AvaloniaFact]
    public void LettingGoOfAnArrow_ReachesTheMoveTool_SoAPressAfterAPauseIsANewStep()
    {
        Canvas.SelectTool(MoveTool.Id);
        Dispatcher.UIThread.RunJobs();
        var history = _canvas.Session.History;
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        cells.Set(new CellPoint(1, 1), Cell.FromGlyph(new Rune('a')));
        history.Execute(cells.Build()!);
        var stepsBefore = history.UndoCount;

        // The Move tool reads the time from the system clock in the App, so the pause has to be a real one.
        _window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        _window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Thread.Sleep(TimeSpan.FromSeconds(0.45));
        _window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        _window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);

        Assert.Equal(new Rune('a'), Layer.GetCell(new CellPoint(3, 1)).Glyph);
        Assert.Equal(stepsBefore + 2, history.UndoCount);
    }

    private void Click(CellPoint cell)
    {
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(CentreOf(cell), MouseButton.Left);
        _window.MouseUp(CentreOf(cell), MouseButton.Left);
    }

    private void PressEscape()
    {
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        Dispatcher.UIThread.RunJobs();
    }

    private void PressLetter(Key key, PhysicalKey physicalKey, string symbol)
    {
        _window.KeyPress(key, RawInputModifiers.None, physicalKey, symbol);
        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, symbol);
        Dispatcher.UIThread.RunJobs();
    }

    private Point CentreOf(CellPoint cell)
    {
        var inControl = _control.Viewport!.GetCellBounds(cell).ToRect(1).Center;
        return _control.TranslatePoint(inControl, _window)!.Value;
    }
}
