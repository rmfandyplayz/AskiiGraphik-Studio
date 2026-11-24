// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Moving;
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
    public void Wheel_ScrollsDown()
    {
        Show();
        var start = Offsets();

        _window.MouseWheel(new Point(10, 10), new Vector(0, -1));

        Assert.Equal((start.Horizontal, start.Vertical + 50), Offsets());
        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
    }

    // Separate from the test above: wheel events close together are one gesture, and the keys
    // held at its start decide it (WheelGestureTrackerTests).
    [AvaloniaFact]
    public void ShiftWheel_ScrollsSideways()
    {
        Show();
        var start = Offsets();

        _window.MouseWheel(new Point(10, 10), new Vector(0, -2), RawInputModifiers.Shift);

        Assert.Equal((start.Horizontal + 100, start.Vertical), Offsets());
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
        var start = Offsets();

        _window.MouseDown(new Point(200, 150), MouseButton.Left);
        _window.MouseMove(new Point(150, 100));
        _window.MouseUp(new Point(150, 100), MouseButton.Left);
        Assert.Equal(start, Offsets());

        _window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        _window.MouseDown(new Point(200, 150), MouseButton.Left);
        _window.MouseMove(new Point(150, 100));
        _window.MouseUp(new Point(150, 100), MouseButton.Left);
        _window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");

        Assert.Equal((start.Horizontal + 50, start.Vertical + 50), Offsets());
    }

    [AvaloniaFact]
    public void WithTheHandTool_LeftDragPans_WithAHandCursor()
    {
        _control.Tool = new HandTool();
        Show();
        Assert.NotNull(_control.Cursor);
        var start = Offsets();

        _window.MouseDown(new Point(200, 150), MouseButton.Left);
        _window.MouseMove(new Point(150, 100));
        _window.MouseUp(new Point(150, 100), MouseButton.Left);

        Assert.Equal((start.Horizontal + 50, start.Vertical + 50), Offsets());
        Assert.NotNull(_control.Cursor);

        _control.Tool = null;
        Assert.Null(_control.Cursor);
    }

    [AvaloniaFact]
    public void ABrush_OutlinesItsTipUnderThePointer_AndFollowsItsSize()
    {
        Show();
        var values = _canvas.ToolSettings.For(BrushTool.TipOptions);
        values.Set(BrushTipOptions.Proportions, "mathematical");
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        _control.Tool = _canvas.ViewModel.ActiveTool;
        var cellSize = Viewport.CellSize;

        _window.MouseMove(new Point(cellSize.Width * 3.5, cellSize.Height * 2.5));
        Assert.Equal(new CellPoint(3, 2), _control.BrushTopLeft);

        values.Set(BrushTipOptions.Size, 3);
        Assert.Equal(new CellPoint(2, 1), _control.BrushTopLeft);

        _window.MouseMove(new Point(-5, -5));
        Assert.Null(_control.BrushTopLeft);
    }

    [AvaloniaFact]
    public void AToolWithoutATip_ShowsNoOutline()
    {
        Show();
        _control.Tool = _canvas.ViewModel.ActiveTool;

        _window.MouseMove(new Point(10, 10));

        Assert.Null(_control.BrushTopLeft);
        Assert.NotNull(_control.HoveredCell);
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
    public void PointerOverTheControl_ReportsWhereItIsInCells_AndLeavingClearsIt()
    {
        Show();
        var cellSize = Viewport.CellSize;

        _window.MouseMove(new Point(cellSize.Width * 3.5, cellSize.Height * 2.25));
        Assert.Equal(new CanvasPoint(3.5, 2.25), _control.PointerCanvasPosition);

        _window.MouseMove(new Point(-5, -5));
        Assert.Null(_control.PointerCanvasPosition);
    }

    [AvaloniaFact]
    public void TheViewCentre_IsTheSpotOfTheCanvasInTheMiddleOfTheView_AndFollowsScrolling()
    {
        Show();
        var cellWidth = (double)Viewport.CellSize.Width;
        Assert.Equal(Viewport.CanvasPointAt(Viewport.UsableCentre), _control.ViewCentre);
        var before = _control.ViewCentre;

        _control.HorizontalOffset += 50;

        Assert.Equal(before.X + (50 / cellWidth), _control.ViewCentre.X, 6);
    }

    [AvaloniaFact]
    public void TheMoveTool_ShowsTheMoveCursor_OverTheCellsItWouldMove()
    {
        Show();
        _canvas.ViewModel.SelectTool(MoveTool.Id);
        _control.Tool = _canvas.ViewModel.ActiveTool;
        _control.Selection = _canvas.Session.Selection;
        SelectionEdits.Select(_canvas.Session, SelectionMask.Rectangle(_canvas.Document.CanvasSize,
            new CellRect(3, 2, 1, 1)), UndoStepNames.RectangleSelect);
        var cellSize = Viewport.CellSize;

        _window.MouseMove(new Point(cellSize.Width * 3.5, cellSize.Height * 2.5));
        Assert.NotNull(_control.Cursor);

        _window.MouseMove(new Point(cellSize.Width * 5.5, cellSize.Height * 2.5));
        Assert.Null(_control.Cursor);

        // With nothing selected a drag moves the whole layer, so the cursor shows anywhere on the canvas.
        SelectionEdits.Deselect(_canvas.Session);
        Assert.NotNull(_control.Cursor);
    }

    [AvaloniaFact]
    public void TheMoveTool_ShowsTheMoveCursor_OverSelectedCellsPastTheCanvas()
    {
        _control.CanvasMargin = 64;
        Show();
        _canvas.ViewModel.SelectTool(MoveTool.Id);
        _control.Tool = _canvas.ViewModel.ActiveTool;
        _control.Selection = _canvas.Session.Selection;
        var size = _canvas.Document.CanvasSize;
        var pastTheLeftEdge = SelectionMask.Rectangle(size, new CellRect(0, 0, 1, 1))
            .MovedWithin(size, new CellPoint(-1, 0), SelectionZone.Around(size, 1));
        _canvas.Session.History.Execute(
            new SetSelectionCommand(_canvas.Session.Selection, pastTheLeftEdge, UndoStepNames.Move));
        var cellSize = Viewport.CellSize;
        var origin = Viewport.CanvasOrigin;

        _window.MouseMove(new Point(origin.X - cellSize.Width * 0.5, origin.Y + cellSize.Height * 0.5));
        Assert.NotNull(_control.Cursor);

        _window.MouseMove(new Point(origin.X + cellSize.Width * 0.5, origin.Y + cellSize.Height * 0.5));
        Assert.Null(_control.Cursor);
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

    [AvaloniaFact]
    public void TheSelectionOutlineAndHatching_MoveWhileThereIsASelection_AndStopWithout()
    {
        _control.Selection = _canvas.Session.Selection;
        Show();
        Render();
        Assert.False(_control.IsSelectionMoving);

        SelectionEdits.SelectAll(_canvas.Session);
        Render();
        Assert.True(_control.IsSelectionMoving);

        SelectionEdits.Deselect(_canvas.Session);
        Render();
        Assert.False(_control.IsSelectionMoving);
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void TheView_ReachesASelectionPastTheZone_AndSettlesBackOnceItsGone()
    {
        Show();
        _control.Selection = _canvas.Session.Selection;
        var maximumBefore = _control.HorizontalMaximum;
        SelectFarPastTheZone();
        Assert.True(_control.HorizontalMaximum > maximumBefore);
        _control.HorizontalOffset = _control.HorizontalMaximum;
        var origin = Viewport.CanvasOrigin;

        SelectionEdits.Deselect(_canvas.Session);

        Assert.NotEqual(origin, Viewport.CanvasOrigin);
        Assert.Equal(maximumBefore, _control.HorizontalMaximum);
    }

    [AvaloniaFact]
    public void WhileAnArrowIsHeld_AndForAPauseAfter_TheViewStaysPut()
    {
        Show();
        _control.Selection = _canvas.Session.Selection;
        SelectFarPastTheZone();
        _control.HorizontalOffset = _control.HorizontalMaximum;
        var origin = Viewport.CanvasOrigin;
        _control.Focus();

        _window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        SelectionEdits.Deselect(_canvas.Session);
        _window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Render();
        Assert.Equal(origin, Viewport.CanvasOrigin);

        Thread.Sleep(MoveTool.ArrowRunPause + TimeSpan.FromMilliseconds(100));
        Render();

        Assert.NotEqual(origin, Viewport.CanvasOrigin);
    }

    [AvaloniaFact]
    public void BringIntoView_LeavesTheViewAsItIs_WhenThePasteIsFullyInView()
    {
        Show();
        var origin = Viewport.CanvasOrigin;

        _control.BringIntoView(new CellRect(0, 0, 2, 1));

        Assert.Equal(origin, Viewport.CanvasOrigin);
        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
    }

    [AvaloniaFact]
    public void BringIntoView_CentresAPasteOutOfView_KeepingAZoomItFitsAt()
    {
        Show();

        _control.BringIntoView(new CellRect(150, 50, 2, 1));

        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
        var middle = Viewport.CellAt(Viewport.UsableCentre);
        Assert.NotNull(middle);
        Assert.InRange(middle.Value.X, 150, 152);
        Assert.InRange(middle.Value.Y, 49, 51);

        // With no glide time, as with reduced motion, the view jumps there and the zoom stays free.
        Assert.False(_control.IsZoomLocked);
    }

    [AvaloniaFact]
    public void BringIntoView_KeepsAZoomAbove100Percent_WhenThePasteFitsThere()
    {
        Show();
        _control.ZoomFactor = ZoomLevels.Maximum;

        _control.BringIntoView(new CellRect(150, 50, 1, 1));

        Assert.Equal(ZoomLevels.Maximum, _control.ZoomFactor);
        Assert.Equal(new CellPoint(150, 50), Viewport.CellAt(Viewport.UsableCentre));
    }

    // In 4.11's testing, a paste made while the view was settling first jumped to where the settling would end.
    [AvaloniaFact]
    public void BringIntoView_GlidesFromWhereTheViewIs_WhileItsSettling()
    {
        _control.ViewGlideMilliseconds = 60_000;
        _control.ZoomGlideMilliseconds = 60_000;
        Show();
        _control.Selection = _canvas.Session.Selection;
        SelectFarPastTheZone();
        _control.HorizontalOffset = _control.HorizontalMaximum;
        var onScreen = _control.CanvasOriginOnScreen();
        SelectionEdits.Deselect(_canvas.Session);
        Assert.Equal(onScreen, _control.CanvasOriginOnScreen());

        _control.BringIntoView(new CellRect(150, 50, 2, 1));

        Assert.True(_control.IsZoomLocked);
        var start = _control.CanvasOriginOnScreen();
        Assert.InRange(start.X, onScreen.X - 1, onScreen.X + 1);
        Assert.InRange(start.Y, onScreen.Y - 1, onScreen.Y + 1);
    }

    [AvaloniaFact]
    public void ARunOfArrowPresses_BringsTheSelectionIntoView_OnceThePauseAfterItIsOver()
    {
        Show();
        _control.Selection = _canvas.Session.Selection;
        _control.Focus();
        var moved = new CellRect(150, 50, 3, 2);

        _window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Select(moved);
        _window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
        Render();
        Assert.False(Viewport.UsableOnCanvas.Contains(PixelsOf(moved)));

        Thread.Sleep(MoveTool.ArrowRunPause + TimeSpan.FromMilliseconds(100));
        Render();

        Assert.True(Viewport.UsableOnCanvas.Contains(PixelsOf(moved)));
    }

    [AvaloniaFact]
    public void ASelectionChangedWithNoArrowHeld_LeavesTheViewAsItIs()
    {
        Show();
        _control.Selection = _canvas.Session.Selection;
        var origin = Viewport.CanvasOrigin;

        Select(new CellRect(150, 50, 3, 2));
        Render();

        Assert.Equal(origin, Viewport.CanvasOrigin);
    }

    [AvaloniaFact]
    public void BringIntoView_ZoomsOutToTheLargestStepAtWhichAPartlyHiddenPasteFits()
    {
        Show();
        var paste = new CellRect(0, 0, 100, 10);

        _control.BringIntoView(paste);

        Assert.True(_control.ZoomFactor < ZoomLevels.ActualSize);
        Assert.True(Viewport.UsableOnCanvas.Contains(PixelsOf(paste)));

        _control.ZoomFactor = ZoomLevels.StepIn(_control.ZoomFactor);
        var bigger = PixelsOf(paste);
        Assert.True(bigger.Width > Viewport.UsableSize.Width || bigger.Height > Viewport.UsableSize.Height);
    }

    [AvaloniaFact]
    public void BringIntoView_ZoomsOutTo25Percent_WhenThePasteFitsAtNoStep()
    {
        Show();

        _control.BringIntoView(new CellRect(0, 0, 5000, 1));

        Assert.Equal(ZoomLevels.Minimum, _control.ZoomFactor);
    }

    [AvaloniaFact]
    public void WhileTheViewGlidesToAPaste_NothingElseChangesTheZoom_AndPanningEndsTheGlide()
    {
        _control.ZoomGlideMilliseconds = 60_000;
        Show();

        _control.BringIntoView(new CellRect(150, 50, 2, 1));
        Assert.True(_control.IsZoomLocked);

        _window.MouseWheel(new Point(10, 10), new Vector(0, -1), RawInputModifiers.Control);
        Render();
        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
        Assert.True(_control.IsZoomLocked);

        // The wheel would still count as zooming, as part of the same turn of it, so this pans by dragging instead.
        _window.MouseDown(new Point(200, 150), MouseButton.Middle);
        _window.MouseMove(new Point(170, 110));
        _window.MouseUp(new Point(170, 110), MouseButton.Middle);
        Assert.False(_control.IsZoomLocked);
    }

    [AvaloniaFact]
    public void TheGlideToAPaste_EndsByItself_AndFreesTheZoom()
    {
        _control.ZoomGlideMilliseconds = 50;
        Show();

        _control.BringIntoView(new CellRect(150, 50, 2, 1));
        Assert.True(_control.IsZoomLocked);

        Thread.Sleep(TimeSpan.FromMilliseconds(150));
        Render();

        Assert.False(_control.IsZoomLocked);
        Assert.Equal(ZoomLevels.ActualSize, _control.ZoomFactor);
    }

    private void Select(CellRect cells)
    {
        var mask = SelectionMask.Rectangle(_canvas.Document.CanvasSize, cells);
        _canvas.Session.History.Execute(
            new SetSelectionCommand(_canvas.Session.Selection, mask, UndoStepNames.Move));
    }

    private PixelRect PixelsOf(CellRect cells)
    {
        var cell = Viewport.CellSize;
        return new PixelRect(cells.Left * cell.Width, cells.Top * cell.Height, cells.Width * cell.Width,
            cells.Height * cell.Height);
    }

    // Two cells 50 columns past the canvas's right edge, far past the zone, as the Move tool could leave them.
    private void SelectFarPastTheZone()
    {
        var size = _canvas.Document.CanvasSize;
        var far = SelectionMask.Rectangle(size, new CellRect(0, 0, 2, 1))
            .MovedWithin(size, new CellPoint(size.Width + 50, 0), new CellRect(-1000, -1000, 3000, 3000));
        _canvas.Session.History.Execute(new SetSelectionCommand(_canvas.Session.Selection, far, UndoStepNames.Move));
    }

    private (double Horizontal, double Vertical) Offsets() => (_control.HorizontalOffset, _control.VerticalOffset);

    private void Show()
    {
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    // A few frames, so the canvas paints and any frame it asked for comes round. The headless platform only renders
    // a frame when something has changed, so the canvas is repainted each time.
    private void Render()
    {
        for (var frame = 0; frame < 3; frame++)
        {
            _control.InvalidateVisual();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
