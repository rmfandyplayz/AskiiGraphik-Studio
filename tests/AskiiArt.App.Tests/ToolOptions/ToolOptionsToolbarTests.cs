// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Options;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.ToolOptions;

/// <summary>The Tool Options toolbar: a slider for a number, buttons for a choice, and "No options".</summary>
public sealed class ToolOptionsToolbarTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(8, 4));
    private readonly ToolOptionsViewModel _viewModel;
    private readonly ToolOptionsToolbar _toolbar;
    private readonly Window _window;

    public ToolOptionsToolbarTests()
    {
        _viewModel = _canvas.CreateToolOptionsViewModel();
        _toolbar = new ToolOptionsToolbar { DataContext = _viewModel };
        _window = new Window { Content = _toolbar };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        _window.Close();
        _viewModel.Dispose();
        _canvas.Dispose();
    }

    private ToolOptionValues BrushValues => _canvas.ToolSettings.For(BrushTool.OptionGroup);

    [AvaloniaFact]
    public void ATool_WithoutOptions_SaysThereAreNone()
    {
        _canvas.ViewModel.SelectTool(HandTool.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(VisibleTexts(), text => text == Strings.ToolOptionsNone);
        Assert.Empty(Visible<Slider>());
    }

    [AvaloniaFact]
    public void TheBrush_ShowsASlider_AndAButtonForEachChoice()
    {
        ChooseTheBrush();

        Assert.Single(Visible<Slider>());
        Assert.Equal(["Square", "Circle", "Perceptual", "Mathematical"],
            Visible<RadioButton>().Select(button => button.Content));
        Assert.Equal(["Circle", "Perceptual"],
            Visible<RadioButton>().Where(button => button.IsChecked == true).Select(button => button.Content));
        Assert.DoesNotContain(VisibleTexts(), text => text == Strings.ToolOptionsNone);
    }

    [AvaloniaFact]
    public void MovingTheSlider_SetsTheSize_AndShowsIt()
    {
        ChooseTheBrush();

        Visible<Slider>().Single().Value = 7;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(7, BrushValues.Get(BrushTipOptions.Size));
        Assert.Contains(VisibleTexts(), text => text == "7");
    }

    [AvaloniaFact]
    public void TheSlider_FollowsTheBracketKeys()
    {
        ChooseTheBrush();

        _canvas.ViewModel.ActiveTool.TextEntered("]");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, Visible<Slider>().Single().Value);
    }

    [AvaloniaFact]
    public void ClickingAChoice_ChoosesIt_AndLetsTheOtherUp()
    {
        ChooseTheBrush();
        var square = Visible<RadioButton>().First(button => Equals(button.Content, "Square"));
        var circle = Visible<RadioButton>().First(button => Equals(button.Content, "Circle"));

        square.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("square", BrushValues.Get(BrushTipOptions.Shape));
        Assert.False(circle.IsChecked);
    }

    [AvaloniaFact]
    public void NothingOnIt_TakesTheKeyboard()
    {
        ChooseTheBrush();

        Assert.All(Visible<Slider>(), slider => Assert.False(slider.Focusable));
        Assert.All(Visible<RadioButton>(), button => Assert.False(button.Focusable));
    }

    [AvaloniaFact]
    public void AVerticalToolbar_StacksTheOptionsDownwards()
    {
        ChooseTheBrush();

        _toolbar.Orientation = Orientation.Vertical;
        Dispatcher.UIThread.RunJobs();

        var options = _toolbar.FindControl<ItemsControl>("Options")!;
        Assert.Equal(Orientation.Vertical, Assert.IsType<StackPanel>(options.ItemsPanelRoot).Orientation);
    }

    private void ChooseTheBrush()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Dispatcher.UIThread.RunJobs();
    }

    private IEnumerable<T> Visible<T>() where T : Control =>
        _toolbar.GetVisualDescendants().OfType<T>().Where(control => control.IsEffectivelyVisible);

    private IEnumerable<string?> VisibleTexts() => Visible<TextBlock>().Select(text => text.Text);
}
