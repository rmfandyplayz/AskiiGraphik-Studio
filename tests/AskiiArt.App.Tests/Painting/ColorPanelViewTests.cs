// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Painting;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Documents;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Painting;

/// <summary>The Color window's picker, which is set from code so that showing a colour never chooses it.</summary>
public sealed class ColorPanelViewTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1), colorMode: ColorMode.Rgb);
    private readonly Window _window;
    private readonly ColorView _picker;

    public ColorPanelViewTests()
    {
        var view = new ColorPanelView { DataContext = _canvas.CreatePaintColorsViewModel() };
        _window = new Window { Width = 600, Height = 600, Content = view };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
        _picker = view.FindControl<ColorView>("Picker")!;
    }

    public void Dispose()
    {
        _window.Close();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void Showing_DoesntChooseTheColorThePickerShows()
    {
        Assert.True(_canvas.Colors.Foreground.IsUnset);
        Assert.True(_canvas.Colors.Background.IsUnset);
    }

    [AvaloniaFact]
    public void MovingThePicker_ChoosesItsColor()
    {
        _picker.Color = Color.FromRgb(10, 20, 30);

        Assert.Equal(CellColor.FromRgb(new RgbColor(10, 20, 30)), _canvas.Colors.Foreground);
    }

    [AvaloniaFact]
    public void AColorChosenElsewhere_MovesThePicker()
    {
        _canvas.Colors.Foreground = CellColor.FromAnsi(AnsiColor.Red);

        var red = AnsiPalette.Vga[AnsiColor.Red];
        Assert.Equal(Color.FromRgb(red.Red, red.Green, red.Blue), _picker.Color);
        Assert.Equal(CellColor.FromAnsi(AnsiColor.Red), _canvas.Colors.Foreground);
    }

    [AvaloniaFact]
    public void AClosedWindow_StopsFollowingTheColors()
    {
        var before = _picker.Color;
        _window.Content = null;
        Dispatcher.UIThread.RunJobs();

        _canvas.Colors.Foreground = CellColor.FromRgb(new RgbColor(1, 2, 3));

        Assert.Equal(before, _picker.Color);
    }
}
