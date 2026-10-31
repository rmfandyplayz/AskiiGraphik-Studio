// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// Pointer and keyboard input on the canvas control, on Avalonia's headless platform. These check
/// behaviour only (offsets, zoom, the hovered cell), never pixels or layout (ARCHITECTURE §19).
/// </summary>
public sealed class CanvasControlTests : IDisposable
{
    // 200 x 100 cells: far bigger than the window, so it scrolls both ways.
    private readonly TestCanvas _canvas = new(new CanvasSize(200, 100));
    private readonly CanvasControl _control;
    private readonly Window _window;

    public CanvasControlTests()
    {
        _control = new CanvasControl
        {
            Composite = _canvas.ViewModel.Composite,
            Font = _canvas.Font,
            Atlases = _canvas.Atlases,
            CanvasMargin = 0
        };
        _window = new Window { Width = 400, Height = 300, Content = _control };
    }

    private CanvasViewport Viewport => _control.Viewport!;

    [AvaloniaFact]
    public void ControlKeyAndWheel_ZoomsInAroundThePointer()
    {
        Show();
        var pointer = new Point(123, 77);
        var cellBefore = Viewport.CellAt(pointer);

        _window.MouseWheel(pointer, new Vector(0, 1), RawInputModifiers.Control);

        Assert.Equal(ZoomLevels.StepIn(ZoomLevels.ActualSize), _control.ZoomFactor);
        Assert.Equal(cellBefore, Viewport.CellAt(pointer));
    }

    [AvaloniaFact]
    public void ControlKeyAndWheelDown_ZoomsOut()
    {
        Show();

        _window.MouseWheel(new Point(10, 10), new Vector(0, -1), RawInputModifiers.Control);

        Assert.Equal(ZoomLevels.StepOut(ZoomLevels.ActualSize), _control.ZoomFactor);
    }

    [AvaloniaFact]
    public void Wheel_ScrollsDown_AndShiftWheelScrollsSideways()
    {
        Show();

        _window.MouseWheel(new Point(10, 10), new Vector(0, -1));
        _window.MouseWheel(new Point(10, 10), new Vector(0, -2), RawInputModifiers.Shift);

        Assert.Equal(50, _control.VerticalOffset);
        Assert.Equal(100, _control.HorizontalOffset);
        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
    }

    [AvaloniaFact]
    public void MiddleDrag_PansTheCanvas()
    {
        Show();
        _control.HorizontalOffset = 100;
        _control.VerticalOffset = 100;

        _window.MouseDown(new Point(200, 150), MouseButton.Middle);
        _window.MouseMove(new Point(170, 110));
        _window.MouseUp(new Point(170, 110), MouseButton.Middle);

        // The content follows the pointer, so the offsets grow as it moves up and left.
        Assert.Equal(130, _control.HorizontalOffset);
        Assert.Equal(140, _control.VerticalOffset);
    }

    [AvaloniaFact]
    public void SpaceAndLeftDrag_PansTheCanvas_ButLeftDragAloneDoesNot()
    {
        Show();

        _window.MouseDown(new Point(200, 150), MouseButton.Left);
        _window.MouseMove(new Point(150, 100));
        _window.MouseUp(new Point(150, 100), MouseButton.Left);
        Assert.Equal(0, _control.HorizontalOffset);

        _window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        _window.MouseDown(new Point(200, 150), MouseButton.Left);
        _window.MouseMove(new Point(150, 100));
        _window.MouseUp(new Point(150, 100), MouseButton.Left);
        _window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");

        Assert.Equal(50, _control.HorizontalOffset);
        Assert.Equal(50, _control.VerticalOffset);
    }

    [AvaloniaFact]
    public void PointerOverTheCanvas_ReportsTheHoveredCell_AndLeavingClearsIt()
    {
        Show();
        var cellSize = Viewport.CellSize;

        _window.MouseMove(new Point(cellSize.Width * 3 + 1, cellSize.Height * 2 + 1));
        Assert.Equal(new CellPoint(3, 2), _control.HoveredCell);

        _window.MouseMove(new Point(-5, -5));
        Assert.Null(_control.HoveredCell);
    }

    [AvaloniaFact]
    public void ZoomFactorSetFromOutside_ZoomsAroundTheCentre()
    {
        Show();
        _control.HorizontalOffset = 500;
        _control.VerticalOffset = 500;
        var centre = new Point(Viewport.ViewportSize.Width / 2.0, Viewport.ViewportSize.Height / 2.0);
        var cellBefore = Viewport.CellAt(centre);

        _control.ZoomFactor = 2;

        Assert.Equal(cellBefore, Viewport.CellAt(centre));
    }

    [AvaloniaFact]
    public void HighDisplayScaling_DrawsCellsAtTheDevicePixelSize()
    {
        Show();
        var cellAt100Percent = Viewport.CellSize;

        _window.SetRenderScaling(2);
        Dispatcher.UIThread.RunJobs();

        // The font is rasterized at twice the size, so cells are about twice as many pixels.
        Assert.InRange(Viewport.CellSize.Width, cellAt100Percent.Width * 2 - 1, cellAt100Percent.Width * 2 + 1);
        Assert.Equal(PixelSize.FromSize(_control.Bounds.Size, 2), Viewport.ViewportSize);
    }

    [AvaloniaFact]
    public void CanvasResize_UpdatesTheScrollRange()
    {
        Show();
        var maximumBefore = _control.HorizontalMaximum;

        new ResizeCanvasCommand(new CanvasSize(400, 100), ResizeAnchor.TopLeft)
            .Apply(_canvas.ViewModel.Composite.Document);

        Assert.True(_control.HorizontalMaximum > maximumBefore);
        Assert.Equal(new CanvasSize(400, 100), Viewport.CanvasSize);
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    private void Show()
    {
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }
}
