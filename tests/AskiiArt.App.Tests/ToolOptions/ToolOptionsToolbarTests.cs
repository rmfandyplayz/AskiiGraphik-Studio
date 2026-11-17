// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Options;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AskiiArt.App.Tests.ToolOptions;

/// <summary>
/// The Tool Options toolbar: a slider for a number, buttons or a dropdown for a choice, a warning, and "No options".
/// </summary>
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
        // Wide enough that the Brush's options all fit on screen, so a click can reach them.
        _window = new Window { Width = 2400, Height = 200, Content = _toolbar };
        _window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose()
    {
        _window.Close();
        _viewModel.Dispose();
        _canvas.Dispose();
    }

    private ToolOptionValues BrushValues => _canvas.ToolSettings.For(BrushTool.TipOptions);

    [AvaloniaFact]
    public void ATool_WithoutOptions_SaysThereAreNone()
    {
        _canvas.ViewModel.SelectTool(HandTool.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(VisibleTexts(), text => text == Strings.ToolOptionsNone);
        Assert.Empty(Visible<Slider>());
    }

    [AvaloniaFact]
    public void TheFGBrush_ShowsASlider_AndAButtonForEachChoice()
    {
        ChooseTheFGBrush();

        Assert.Single(Visible<Slider>());
        Assert.Equal(["Square", "Circle", "Perceptual", "Mathematical"],
            Visible<RadioButton>().Select(button => button.Content));
        Assert.Equal(["Circle", "Perceptual"],
            Visible<RadioButton>().Where(button => button.IsChecked == true).Select(button => button.Content));
        Assert.Empty(Visible<ComboBox>());
        Assert.DoesNotContain(VisibleTexts(), text => text == Strings.ToolOptionsNone);
    }

    [AvaloniaFact]
    public void TheBrush_ShowsPercentages_AndDropdownsForTheRampAndTheBlendMode()
    {
        ChooseTheBrush();

        Assert.Equal(3, Visible<Slider>().Count());
        Assert.Contains(VisibleTexts(), text => text == "100%");
        Assert.Contains(VisibleTexts(), text => text == "50%");
        Assert.Equal(2, Visible<ComboBox>().Count());
        Assert.Contains(VisibleTexts(), text => text == Strings.RampStandard);
        Assert.DoesNotContain(VisibleTexts(), text => text == BuiltInRamps.Standard.Ramp.ToString());
    }

    [AvaloniaFact]
    public void ClickingADropdown_OpensIt_ThoughItDoesntTakeTheKeyboard()
    {
        ChooseTheBrush();
        var ramp = Visible<ComboBox>().First();
        var centre = ramp.TranslatePoint(new Point(ramp.Bounds.Width / 2, ramp.Bounds.Height / 2), _window)!.Value;

        _window.MouseDown(centre, MouseButton.Left);
        _window.MouseUp(centre, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.True(ramp.IsDropDownOpen);
        Assert.False(ramp.IsFocused);
        ramp.IsDropDownOpen = false;
    }

    [AvaloniaFact]
    public void TheOpenRampDropdown_ListsEachRampsCharacters()
    {
        ChooseTheBrush();
        var ramp = Visible<ComboBox>().First();

        ramp.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var texts = Enumerable.Range(0, ramp.ItemCount)
            .SelectMany(index => ramp.ContainerFromIndex(index)!.GetVisualDescendants().OfType<TextBlock>())
            .Select(text => text.Text)
            .ToArray();
        Assert.All(BuiltInRamps.All, builtIn => Assert.Contains(builtIn.Ramp.ToString(), texts));
        ramp.IsDropDownOpen = false;
    }

    [AvaloniaFact]
    public void TheBlendModesStillToCome_AreGreyedOutInTheDropdown()
    {
        ChooseTheBrush();
        var blendMode = Visible<ComboBox>().Last();

        blendMode.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var items = Enumerable.Range(0, blendMode.ItemCount)
            .Select(index => Assert.IsType<ComboBoxItem>(blendMode.ContainerFromIndex(index)))
            .ToArray();
        Assert.Equal([true, false, false], items.Select(item => item.IsEnabled));
        Assert.All(items, item => Assert.False(item.Focusable));
        blendMode.IsDropDownOpen = false;
    }

    [AvaloniaFact]
    public void ARampTheDocumentCantHold_ShowsAWarning()
    {
        ChooseTheBrush();
        Assert.DoesNotContain(VisibleTexts(), text => text == Strings.ToolWarningRampDoesNotFit);

        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));
        _canvas.ToolSettings.For(RampBrushOptions.Group).Set(RampBrushOptions.Ramp, BuiltInRamps.Blocks.Id);
        Dispatcher.UIThread.RunJobs();

        Assert.Contains(VisibleTexts(), text => text == Strings.ToolWarningRampDoesNotFit);
    }

    [AvaloniaFact]
    public void MovingTheSlider_SetsTheSize_AndShowsIt()
    {
        ChooseTheBrush();

        Visible<Slider>().First().Value = 7;
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

        Assert.Equal(2, Visible<Slider>().First().Value);
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
        Assert.All(Visible<ComboBox>(), dropdown => Assert.False(dropdown.Focusable));
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

    private void ChooseTheFGBrush()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        Dispatcher.UIThread.RunJobs();
    }

    private IEnumerable<T> Visible<T>() where T : Control =>
        _toolbar.GetVisualDescendants().OfType<T>().Where(control => control.IsEffectivelyVisible);

    private IEnumerable<string?> VisibleTexts() => Visible<TextBlock>().Select(text => text.Text);
}
