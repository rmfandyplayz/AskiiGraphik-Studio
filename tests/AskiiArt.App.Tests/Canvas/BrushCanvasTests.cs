// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>The Brush through the real canvas view: its letter, its keys, painting and picking.</summary>
public sealed class BrushCanvasTests : IDisposable
{
    private static readonly CellColor _red = CellColor.FromAnsi(AnsiColor.Red);
    private static readonly CellColor _blue = CellColor.FromAnsi(AnsiColor.Blue);

    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10), colorMode: ColorMode.Ansi);
    private readonly Window _window;
    private readonly CanvasControl _control;

    public BrushCanvasTests()
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

    private PaintColors Colors => _canvas.Colors;

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void TheBKey_ChoosesTheBrush()
    {
        PressKey(Key.B, PhysicalKey.B, "b");

        Assert.IsType<BrushTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void Keys1And2_ChooseTheBrushes_WhileTheBrushIsChosen()
    {
        ChooseTheBrush();

        PressKey(Key.D2, PhysicalKey.Digit2, "2");
        Assert.Equal(PaintColorTarget.Background, Colors.Target);

        PressKey(Key.D1, PhysicalKey.Digit1, "1");
        Assert.Equal(PaintColorTarget.Foreground, Colors.Target);
    }

    [AvaloniaFact]
    public void Key2_TypesA2_WithTheTextTool()
    {
        Click(new CellPoint(2, 1));

        PressKey(Key.D2, PhysicalKey.Digit2, "2");

        Assert.IsType<TextTool>(Canvas.ActiveTool);
        Assert.Equal(PaintColorTarget.Foreground, Colors.Target);
        Assert.Equal(new Rune('2'), Layer.GetCell(new CellPoint(2, 1)).Glyph);
    }

    [AvaloniaFact]
    public void DraggingWithTheBGBrush_PaintsBackgroundsThroughTheCanvas_AsOneStep()
    {
        ChooseTheBrush();
        Colors.Target = PaintColorTarget.Background;
        Colors.Background = _blue;

        _window.MouseDown(CentreOf(new CellPoint(0, 1)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(3, 1)));
        _window.MouseUp(CentreOf(new CellPoint(3, 1)), MouseButton.Left);

        for (var x = 0; x < 4; x++)
            Assert.Equal(Cell.FromBackground(_blue), Layer.GetCell(new CellPoint(x, 1)));

        Assert.True(Layer.GetCell(new CellPoint(4, 1)).IsEmpty);
        Assert.Equal(1, _canvas.Session.History.UndoCount);
    }

    [AvaloniaFact]
    public void AnAltClick_PicksTheColorUp_AndPaintsNothing()
    {
        Put(new CellPoint(5, 2), Cell.FromGlyph(new Rune('x'), _red, CellColor.Unset));
        ChooseTheBrush();

        _window.MouseDown(CentreOf(new CellPoint(5, 2)), MouseButton.Left, RawInputModifiers.Alt);
        _window.MouseUp(CentreOf(new CellPoint(5, 2)), MouseButton.Left, RawInputModifiers.Alt);

        Assert.Equal(_red, Colors.Foreground);
        Assert.Equal(1, _canvas.Session.History.UndoCount);
    }

    [AvaloniaFact]
    public void ARightClick_PaintsNothing()
    {
        Put(new CellPoint(1, 1), Cell.FromGlyph(new Rune('x')));
        ChooseTheBrush();
        Colors.Foreground = _red;

        _window.MouseDown(CentreOf(new CellPoint(1, 1)), MouseButton.Right);
        _window.MouseUp(CentreOf(new CellPoint(1, 1)), MouseButton.Right);

        Assert.True(Layer.GetCell(new CellPoint(1, 1)).Foreground.IsUnset);
    }

    private void Put(CellPoint point, Cell cell)
    {
        var cells = new CellEditBuilder(_canvas.Document, Layer.Id);
        cells.Set(point, cell);
        _canvas.Session.History.Execute(cells.Build()!);
    }

    private void ChooseTheBrush()
    {
        Canvas.SelectTool(BrushTool.Id);
        Dispatcher.UIThread.RunJobs();
    }

    private void Click(CellPoint cell)
    {
        Dispatcher.UIThread.RunJobs();
        _window.MouseDown(CentreOf(cell), MouseButton.Left);
        _window.MouseUp(CentreOf(cell), MouseButton.Left);
    }

    // As a keyboard does: the key goes down, then the text it types arrives.
    private void PressKey(Key key, PhysicalKey physicalKey, string symbol)
    {
        _window.KeyPress(key, RawInputModifiers.None, physicalKey, symbol);
        _window.KeyTextInput(symbol);
        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, symbol);
        Dispatcher.UIThread.RunJobs();
    }

    private Point CentreOf(CellPoint cell)
    {
        var inControl = _control.Viewport!.GetCellBounds(cell).ToRect(1).Center;
        return _control.TranslatePoint(inControl, _window)!.Value;
    }
}
