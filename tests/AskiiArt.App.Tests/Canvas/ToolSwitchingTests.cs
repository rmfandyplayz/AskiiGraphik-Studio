// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools.Eraser;
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
    public void TheTextTool_IsActiveAtFirst()
    {
        Assert.Equal(TextTool.Id, Canvas.ActiveTool.Descriptor.Id);
    }

    [AvaloniaFact]
    public void EachTool_HasAnAction_CheckedWhenItsActive()
    {
        var actions = new ToolActions(Canvas).CreateActions().ToDictionary(action => action.Id);
        var text = actions[ToolActionIds.Text];
        var eraser = actions[ToolActionIds.Eraser];
        Assert.True(text.CheckedState!.IsChecked);
        Assert.False(eraser.CheckedState!.IsChecked);

        eraser.Command.Execute(null);

        Assert.Equal(EraserTool.Id, Canvas.ActiveTool.Descriptor.Id);
        Assert.False(text.CheckedState!.IsChecked);
        Assert.True(eraser.CheckedState!.IsChecked);
    }

    [AvaloniaFact]
    public void ChoosingTheActiveToolAgain_KeepsItsState()
    {
        var actions = new ToolActions(Canvas).CreateActions().ToDictionary(action => action.Id);
        var notified = 0;
        actions[ToolActionIds.Text].CheckedState!.Changed += (_, _) => notified++;
        var tool = Canvas.ActiveTool;

        actions[ToolActionIds.Text].Command.Execute(null);

        Assert.Same(tool, Canvas.ActiveTool);
        Assert.Equal(1, notified);
    }

    [AvaloniaFact]
    public void SwitchingTools_RemovesTheCaret()
    {
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
    public void ALetterWithAModifier_DoesntChooseATool()
    {
        _window.KeyPress(Key.E, RawInputModifiers.Control, PhysicalKey.E, "e");

        Assert.Equal(TextTool.Id, Canvas.ActiveTool.Descriptor.Id);
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

    private void Click(CellPoint cell)
    {
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(CentreOf(cell), MouseButton.Left);
        _window.MouseUp(CentreOf(cell), MouseButton.Left);
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
