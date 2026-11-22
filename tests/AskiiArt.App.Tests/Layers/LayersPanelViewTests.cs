// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Layers;
using AskiiArt.App.Tests.Fakes;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Layers;

/// <summary>Renaming a layer in the real Layers panel.</summary>
public sealed class LayersPanelViewTests : IDisposable
{
    private readonly TestCanvas _canvas = new();
    private readonly LayersPanelViewModel _panel;
    private readonly Window _window;

    public LayersPanelViewTests()
    {
        _panel = new LayersPanelViewModel(_canvas.ActiveDocument);
        _panel.AddLayer();
        _panel.AddLayer();

        // The middle row, so Up and Down both have a row to go to.
        _panel.ActiveRow = 1;
        _window = new Window { Width = 400, Height = 300, Content = new LayersPanelView { DataContext = _panel } };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        _canvas.Dispose();
    }

    // Up and Down have no other line to go to, Left is at the start and Right at the end.
    [AvaloniaTheory]
    [InlineData(Key.Up, PhysicalKey.ArrowUp)]
    [InlineData(Key.Down, PhysicalKey.ArrowDown)]
    [InlineData(Key.Left, PhysicalKey.ArrowLeft)]
    [InlineData(Key.Right, PhysicalKey.ArrowRight)]
    public void AnArrowTheNameBoxCantUse_DoesNothing_AndTheRenameGoesOn(Key key, PhysicalKey physicalKey)
    {
        var box = StartRenaming();
        box.CaretIndex = key == Key.Left ? 0 : box.Text!.Length;

        Press(key, physicalKey);

        Assert.True(_panel.Rows[1].IsRenaming);
        Assert.Equal(1, _panel.ActiveRow);
        Assert.True(box.IsFocused);
    }

    [AvaloniaFact]
    public void AnArrowTheNameBoxCanUse_StillMovesTheCaret()
    {
        var box = StartRenaming();
        box.CaretIndex = 2;

        Press(Key.Left, PhysicalKey.ArrowLeft);

        Assert.Equal(1, box.CaretIndex);
        Assert.True(_panel.Rows[1].IsRenaming);
    }

    private TextBox StartRenaming()
    {
        _panel.RenameActiveLayer();
        Dispatcher.UIThread.RunJobs();
        var box = _window.GetVisualDescendants().OfType<TextBox>().Single(box => box.IsVisible);
        box.ClearSelection();
        return box;
    }

    private void Press(Key key, PhysicalKey physicalKey)
    {
        _window.KeyPress(key, RawInputModifiers.None, physicalKey, null);
        _window.KeyRelease(key, RawInputModifiers.None, physicalKey, null);
        Dispatcher.UIThread.RunJobs();
    }
}
