// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Painting;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.Painting;

/// <summary>The Brush Options toolbar: the two swatches, and choosing which colour the windows change.</summary>
public sealed class BrushOptionsToolbarTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1), colorMode: ColorMode.Ansi);
    private readonly PaintColorsViewModel _viewModel;
    private readonly BrushOptionsToolbar _toolbar;
    private readonly BrushOptionsToolbar _dragCopy;
    private readonly Window _window;

    public BrushOptionsToolbarTests()
    {
        _viewModel = _canvas.CreatePaintColorsViewModel();
        _toolbar = new BrushOptionsToolbar { DataContext = _viewModel };
        _dragCopy = new BrushOptionsToolbar { DataContext = _viewModel };
        _window = new Window { Content = new StackPanel { Children = { _toolbar, _dragCopy } } };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void TheFgSwatch_StartsChosen_AndBothShowNone()
    {
        Assert.True(Button(_toolbar, "ForegroundButton").IsChecked);
        Assert.False(Button(_toolbar, "BackgroundButton").IsChecked);
        Assert.True(Swatch(_toolbar, "ForegroundButton").IsShowingNone);
        Assert.True(Swatch(_toolbar, "BackgroundButton").IsShowingNone);
    }

    [AvaloniaFact]
    public void ChoosingTheBgSwatch_MakesItTheOneTheWindowsChange_InEveryCopy()
    {
        Button(_toolbar, "BackgroundButton").IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(PaintColorTarget.Background, _viewModel.Target);
        foreach (var toolbar in new[] { _toolbar, _dragCopy })
        {
            Assert.False(Button(toolbar, "ForegroundButton").IsChecked);
            Assert.True(Button(toolbar, "BackgroundButton").IsChecked);
        }
    }

    [AvaloniaFact]
    public void TheSwatches_ShowTheColors()
    {
        _canvas.Colors.Background = CellColor.FromAnsi(AnsiColor.Blue);
        Dispatcher.UIThread.RunJobs();

        Assert.True(Swatch(_toolbar, "ForegroundButton").IsShowingNone);
        var background = Swatch(_toolbar, "BackgroundButton");
        Assert.False(background.IsShowingNone);
        Assert.Equal(_viewModel.BackgroundBrush, background.Fill);
    }

    [AvaloniaFact]
    public void TheSwatches_NeverTakeTheKeyboard()
    {
        Assert.False(Button(_toolbar, "ForegroundButton").Focusable);
        Assert.False(Button(_toolbar, "BackgroundButton").Focusable);
    }

    [AvaloniaFact]
    public void AVerticalToolbar_RunsItDownwards()
    {
        _toolbar.Orientation = Orientation.Vertical;
        Dispatcher.UIThread.RunJobs();

        var foreground = Button(_toolbar, "ForegroundButton").Bounds;
        var background = Button(_toolbar, "BackgroundButton").Bounds;
        Assert.True(background.Top > foreground.Bottom);
    }

    private static RadioButton Button(BrushOptionsToolbar toolbar, string name) =>
        toolbar.FindControl<RadioButton>(name)!;

    private static ColorSwatch Swatch(BrushOptionsToolbar toolbar, string buttonName) =>
        Button(toolbar, buttonName).GetVisualDescendants().OfType<ColorSwatch>().Single();
}
