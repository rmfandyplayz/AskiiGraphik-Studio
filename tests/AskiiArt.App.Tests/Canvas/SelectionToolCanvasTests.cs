// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Moving;
using AskiiArt.Core.Tools.Selecting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>The selection tools with the real canvas view: their keys, modifiers, clicks and path.</summary>
public sealed class SelectionToolCanvasTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10));
    private readonly Window _window;
    private readonly CanvasControl _control;

    public SelectionToolCanvasTests()
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

    private CanvasSize Size => _canvas.Document.CanvasSize;

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void TheMKey_ThenKeys1To4_ChooseEachSelectionTool()
    {
        PressKey(Key.M, PhysicalKey.M, "m");
        Assert.IsType<RectangleSelectTool>(Canvas.ActiveTool);

        PressKey(Key.D2, PhysicalKey.Digit2, "2");
        Assert.IsType<EllipseSelectTool>(Canvas.ActiveTool);

        PressKey(Key.D3, PhysicalKey.Digit3, "3");
        Assert.IsType<LassoTool>(Canvas.ActiveTool);

        PressKey(Key.D4, PhysicalKey.Digit4, "4");
        Assert.IsType<PolygonalLassoTool>(Canvas.ActiveTool);

        PressKey(Key.D1, PhysicalKey.Digit1, "1");
        Assert.IsType<RectangleSelectTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void AltPressedAndLetGoDuringADrag_ReshapesTheRectangleAtOnce()
    {
        Canvas.SelectTool(RectangleSelectTool.Id);
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(CentreOf(5, 5), MouseButton.Left);
        _window.MouseMove(CentreOf(6, 6));

        _window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        Assert.Equal(Rectangle(4, 4, 3, 3), Canvas.ActiveTool.Overlay.SelectionPreview?.Result);

        _window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Assert.Equal(Rectangle(5, 5, 2, 2), Canvas.ActiveTool.Overlay.SelectionPreview?.Result);

        _window.MouseUp(CentreOf(6, 6), MouseButton.Left);
        Assert.Equal(Rectangle(5, 5, 2, 2), _canvas.Session.Selection.Mask);
    }

    // A drag with nothing selected moves the whole layer and selects it, so the selection shows where it went. The
    // drag goes nearly straight down, which Ctrl turns into a diagonal.
    [AvaloniaFact]
    public void CtrlPressedAndLetGoDuringAMoveToolDrag_KeepsTheMoveToADiagonalAtOnce()
    {
        Canvas.SelectTool(MoveTool.Id);
        Dispatcher.UIThread.RunJobs();
        var (key, physicalKey, modifier) = CommandKey();
        _window.MouseDown(CentreOf(5, 5), MouseButton.Left);
        _window.MouseMove(CentreOf(6, 9));
        var movedStraight = new CellRect(1, 4, 20, 10);
        Assert.Equal(movedStraight, _canvas.Session.Selection.Mask.Bounds);

        _window.KeyPress(key, modifier, physicalKey, null);
        Assert.NotEqual(movedStraight, _canvas.Session.Selection.Mask.Bounds);

        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Assert.Equal(movedStraight, _canvas.Session.Selection.Mask.Bounds);
        _window.MouseUp(CentreOf(6, 9), MouseButton.Left);
    }

    [AvaloniaFact]
    public void ShiftHeldAtThePress_AddsToTheSelection()
    {
        Canvas.SelectTool(RectangleSelectTool.Id);
        Drag(CentreOf(0, 0), CentreOf(1, 1), RawInputModifiers.None);

        Drag(CentreOf(4, 4), CentreOf(5, 5), RawInputModifiers.Shift);

        var expected = Rectangle(0, 0, 2, 2).CombinedWith(Rectangle(4, 4, 2, 2), SelectionCombineMode.Add);
        Assert.Equal(expected, _canvas.Session.Selection.Mask);
    }

    [AvaloniaFact]
    public void TheLasso_ShowsItsPathWhileDrawing_AndSelectsWhatItGoesRound()
    {
        Canvas.SelectTool(LassoTool.Id);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CornerOf(2, 2), MouseButton.Left);
        _window.MouseMove(CornerOf(6, 2));
        _window.MouseMove(CornerOf(6, 5));
        Assert.Equal(3, Canvas.ActiveTool.Overlay.Path?.Count);

        _window.MouseUp(CornerOf(2, 5), MouseButton.Left);

        Assert.Equal(Rectangle(2, 2, 4, 3), _canvas.Session.Selection.Mask);
        Assert.Null(Canvas.ActiveTool.Overlay.Path);
    }

    [AvaloniaFact]
    public void ThePolygonalLasso_TakesCornersByClicks_AndEnterFinishes()
    {
        Canvas.SelectTool(PolygonalLassoTool.Id);
        Dispatcher.UIThread.RunJobs();

        Click(CornerOf(2, 2));
        Click(CornerOf(6, 2));
        Click(CornerOf(6, 5));
        Click(CornerOf(2, 5));
        _window.MouseMove(CentreOf(9, 9));
        Assert.Equal(5, Canvas.ActiveTool.Overlay.Path?.Count);

        _window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        _window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

        Assert.Equal(Rectangle(2, 2, 4, 3), _canvas.Session.Selection.Mask);
        Assert.Equal(1, _canvas.Session.History.UndoCount);
    }

    [AvaloniaFact]
    public void ThePolygonalLasso_FinishesOnADoubleClick()
    {
        Canvas.SelectTool(PolygonalLassoTool.Id);
        Dispatcher.UIThread.RunJobs();

        Click(CornerOf(2, 2));
        Click(CornerOf(6, 2));
        Click(CornerOf(6, 5));
        Click(CornerOf(2, 5));
        Click(CornerOf(2, 5));

        Assert.Equal(Rectangle(2, 2, 4, 3), _canvas.Session.Selection.Mask);
        Assert.False(((PolygonalLassoTool)Canvas.ActiveTool).IsDrawing);
    }

    [AvaloniaFact]
    public void ThePolygonalLasso_FinishesOnAClickOnItsFirstCornersMarker_EvenInTheNextCell()
    {
        Canvas.SelectTool(PolygonalLassoTool.Id);
        Dispatcher.UIThread.RunJobs();

        Click(CornerOf(2, 2));
        Click(CornerOf(6, 2));
        Click(CornerOf(6, 5));
        Click(CornerOf(2, 5));
        Click(CornerOf(2, 2) - new Point(3, 3));

        Assert.Equal(Rectangle(2, 2, 4, 3), _canvas.Session.Selection.Mask);
        Assert.False(((PolygonalLassoTool)Canvas.ActiveTool).IsDrawing);
    }

    private static SelectionMask Rectangle(CanvasSize size, int left, int top, int width, int height) =>
        SelectionMask.Rectangle(size, new CellRect(left, top, width, height));

    private SelectionMask Rectangle(int left, int top, int width, int height) =>
        Rectangle(Size, left, top, width, height);

    private void Drag(Point from, Point to, RawInputModifiers modifiers)
    {
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(from, MouseButton.Left, modifiers);
        _window.MouseMove(to, modifiers);
        _window.MouseUp(to, MouseButton.Left, modifiers);
    }

    // Ctrl, or Cmd where the platform's shortcuts use it.
    private static (Key Key, PhysicalKey PhysicalKey, RawInputModifiers Modifier) CommandKey()
    {
        if (Application.Current!.PlatformSettings!.HotkeyConfiguration.CommandModifiers.HasFlag(KeyModifiers.Meta))
            return (Key.LWin, PhysicalKey.MetaLeft, RawInputModifiers.Meta);

        return (Key.LeftCtrl, PhysicalKey.ControlLeft, RawInputModifiers.Control);
    }

    private void Click(Point point)
    {
        _window.MouseDown(point, MouseButton.Left);
        _window.MouseUp(point, MouseButton.Left);
    }

    private void PressKey(Key key, PhysicalKey physicalKey, string symbol)
    {
        _window.KeyPress(key, RawInputModifiers.None, physicalKey, symbol);
        _window.KeyTextInput(symbol);
        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, symbol);
        Dispatcher.UIThread.RunJobs();
    }

    private Point CentreOf(int column, int row)
    {
        var inControl = _control.Viewport!.GetCellBounds(new CellPoint(column, row)).ToRect(1).Center;
        return _control.TranslatePoint(inControl, _window)!.Value;
    }

    // The top-left corner of a cell, nudged a little in, so a shape through these points goes round whole cells.
    private Point CornerOf(int column, int row)
    {
        var bounds = _control.Viewport!.GetCellBounds(new CellPoint(column, row)).ToRect(1);
        return _control.TranslatePoint(bounds.TopLeft + new Point(0.5, 0.5), _window)!.Value;
    }
}
