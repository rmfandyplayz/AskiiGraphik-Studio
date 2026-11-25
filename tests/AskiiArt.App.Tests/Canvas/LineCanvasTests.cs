// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;
using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Lines;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>The Shapes group's Line tool through the real canvas view (ADR 0079): its letter, a drag and Esc.</summary>
public sealed class LineCanvasTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10), colorMode: ColorMode.Ansi);
    private readonly Window _window;
    private readonly CanvasControl _control;

    public LineCanvasTests()
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
    public void TheUKey_ChoosesTheLineTool_AndKey1ChoosesItAgain()
    {
        PressKey(Key.U, PhysicalKey.U, "u");
        Assert.IsType<LineTool>(Canvas.ActiveTool);

        PressKey(Key.D1, PhysicalKey.Digit1, "1");
        Assert.IsType<LineTool>(Canvas.ActiveTool);
    }

    [AvaloniaFact]
    public void ADrag_ShowsTheLineAsItGoes_AndLeavesItAsOneStep()
    {
        Canvas.SelectTool(LineTool.Id);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CentreOf(new CellPoint(1, 2)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(6, 2)));
        Assert.Equal(new Rune('-'), Layer.GetCell(new CellPoint(6, 2)).Glyph);

        _window.MouseMove(CentreOf(new CellPoint(4, 2)));
        _window.MouseUp(CentreOf(new CellPoint(4, 2)), MouseButton.Left);

        for (var x = 1; x <= 4; x++)
            Assert.Equal(new Rune('-'), Layer.GetCell(new CellPoint(x, 2)).Glyph);

        Assert.True(Layer.GetCell(new CellPoint(5, 2)).IsEmpty);
        Assert.Equal(1, _canvas.Session.History.UndoCount);
        Assert.Equal(UndoStepNames.Line, _canvas.Session.History.GetStepNameKey(0));
    }

    [AvaloniaFact]
    public void EscDuringADrag_TakesTheLineBack_AndKeepsTheLineTool()
    {
        Canvas.SelectTool(LineTool.Id);
        Dispatcher.UIThread.RunJobs();

        _window.MouseDown(CentreOf(new CellPoint(1, 2)), MouseButton.Left);
        _window.MouseMove(CentreOf(new CellPoint(6, 2)));
        _window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        _window.MouseUp(CentreOf(new CellPoint(6, 2)), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Layer.GetCell(new CellPoint(3, 2)).IsEmpty);
        Assert.Equal(0, _canvas.Session.History.UndoCount);
        Assert.IsType<LineTool>(Canvas.ActiveTool);
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
