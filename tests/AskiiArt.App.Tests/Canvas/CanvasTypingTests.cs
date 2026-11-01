// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Canvas;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Canvas;

/// <summary>
/// The canvas passing clicks, keys and typed text to the text tool, on Avalonia's headless
/// platform. These check document changes only, never pixels (ARCHITECTURE §19).
/// </summary>
public sealed class CanvasTypingTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(20, 10));
    private readonly CanvasControl _control;
    private readonly Window _window;

    public CanvasTypingTests()
    {
        _control = new CanvasControl
        {
            Composite = _canvas.ViewModel.Composite,
            Font = _canvas.Font,
            Atlases = _canvas.Atlases,
            Tool = _canvas.ViewModel.ActiveTool,
            CanvasMargin = 0
        };
        _window = new Window { Width = 400, Height = 300, Content = _control };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    private Layer Layer => _canvas.Document.ActiveFrame.Layers[0];

    [AvaloniaFact]
    public void ClickingACellThenTyping_WritesThereAndMovesRight()
    {
        Click(new CellPoint(2, 1));

        _window.KeyTextInput("hi");

        Assert.Equal("h", GlyphAt(2, 1));
        Assert.Equal("i", GlyphAt(3, 1));
    }

    [AvaloniaFact]
    public void ArrowKeysAndBackspace_GoToTheTool()
    {
        Click(new CellPoint(2, 1));
        _window.KeyTextInput("ab");

        Press(Key.Left, PhysicalKey.ArrowLeft);
        Press(Key.Back, PhysicalKey.Backspace);
        _window.KeyTextInput("x");

        // Left moved onto "b", Backspace erased it, and "x" went into its cell.
        Assert.Equal("a", GlyphAt(2, 1));
        Assert.Equal("x", GlyphAt(3, 1));
    }

    [AvaloniaFact]
    public void WithTheTextTool_SpaceTypesASpace_AndSpaceDragDoesNotPan()
    {
        Click(new CellPoint(0, 0));

        _window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        _window.KeyTextInput(" ");
        var start = CentreOf(new CellPoint(5, 5));
        _window.MouseDown(start, MouseButton.Left);
        _window.MouseMove(start - new Point(50, 50));
        _window.MouseUp(start - new Point(50, 50), MouseButton.Left);
        _window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");

        Assert.True(Layer.GetCell(new CellPoint(0, 0)).IsOccupied);
        Assert.Equal(" ", GlyphAt(0, 0));
        Assert.Equal(0, _control.HorizontalOffset);
        Assert.Equal(0, _control.VerticalOffset);
    }

    [AvaloniaFact]
    public void JumpKeys_MoveTheCaretSeveralCells_AndShiftArrowIsLeftAlone()
    {
        // The Windows and Linux keys, whatever the test machine: Ctrl+Right jumps, Alt+Down jumps further.
        _control.Keymap = ToolKeymap.CreateDefault(isMacOS: false);
        Click(new CellPoint(0, 0));

        Press(Key.Right, PhysicalKey.ArrowRight, RawInputModifiers.Control);
        Press(Key.Down, PhysicalKey.ArrowDown, RawInputModifiers.Alt);
        Press(Key.Left, PhysicalKey.ArrowLeft, RawInputModifiers.Shift);

        Assert.Equal(new CellPoint(3, 9), _canvas.ViewModel.ActiveTool.Overlay.Caret);
    }

    [AvaloniaFact]
    public void TypingWithoutAClick_ChangesNothing()
    {
        _control.Focus();

        _window.KeyTextInput("a");

        Assert.False(_canvas.Session.History.CanUndo);
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    private string GlyphAt(int x, int y) => Layer.GetCell(new CellPoint(x, y)).Glyph.ToString();

    private void Click(CellPoint cell)
    {
        var point = CentreOf(cell);
        _window.MouseDown(point, MouseButton.Left);
        _window.MouseUp(point, MouseButton.Left);
    }

    private void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        _window.KeyPress(key, modifiers, physicalKey, null);
        _window.KeyRelease(key, modifiers, physicalKey, null);
    }

    // The window's scaling is 1, so device pixels and logical pixels are the same.
    private Point CentreOf(CellPoint cell) => _control.Viewport!.GetCellBounds(cell).ToRect(1).Center;
}
