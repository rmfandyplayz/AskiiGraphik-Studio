// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Hand;
using Avalonia.Headless.XUnit;

namespace AskiiArt.App.Tests.ToolOptions;

/// <summary>The Tool Options toolbar's items: the chosen tool's options, and changing them (ADR 0047).</summary>
public sealed class ToolOptionsViewModelTests : IDisposable
{
    private readonly TestCanvas _canvas = new(new CanvasSize(8, 4));
    private readonly ToolOptionsViewModel _viewModel;

    public ToolOptionsViewModelTests()
    {
        _viewModel = _canvas.CreateToolOptionsViewModel();
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _canvas.Dispose();
    }

    [AvaloniaFact]
    public void ATool_WithoutOptions_ShowsNone()
    {
        _canvas.ViewModel.SelectTool(HandTool.Id);

        Assert.Empty(_viewModel.Items);
        Assert.False(_viewModel.HasOptions);
    }

    [AvaloniaFact]
    public void TheBrush_ShowsItsSizeShapeAndProportions_AtTheirDefaults()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);

        Assert.True(_viewModel.HasOptions);
        Assert.Equal(["Size", "Shape", "Proportions"], _viewModel.Items.Select(item => item.Name));

        var size = Assert.IsType<NumberToolOptionItem>(_viewModel.Items[0]);
        Assert.Equal((1, 100, 1), (size.Minimum, size.Maximum, size.Value));

        var shape = Assert.IsType<ChoiceToolOptionItem>(_viewModel.Items[1]);
        Assert.Equal(["Square", "Circle"], shape.Choices.Select(choice => choice.Name));
        Assert.Equal([false, true], shape.Choices.Select(choice => choice.IsChosen));

        var proportions = Assert.IsType<ChoiceToolOptionItem>(_viewModel.Items[2]);
        Assert.Equal(["Perceptual", "Mathematical"], proportions.Choices.Select(choice => choice.Name));
        Assert.Equal([true, false], proportions.Choices.Select(choice => choice.IsChosen));
    }

    [AvaloniaFact]
    public void ChangingTheSize_ChangesTheBrushesTip()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);

        Size.Value = 5;

        Assert.Equal(5, _canvas.ToolSettings.For(BrushTool.OptionGroup).Get(BrushTipOptions.Size));
        Assert.Equal(5, _canvas.ViewModel.ActiveTool.Overlay.Footprint?.Width);
    }

    [AvaloniaFact]
    public void ChoosingAChoice_UnchoosesTheOthers_AndSaysSo()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var shape = (ChoiceToolOptionItem)_viewModel.Items[1];
        var circleChanges = 0;
        shape.Choices[1].PropertyChanged += (_, _) => circleChanges++;

        shape.Choices[0].IsChosen = true;

        Assert.Equal([true, false], shape.Choices.Select(choice => choice.IsChosen));
        Assert.Equal(1, circleChanges);
    }

    [AvaloniaFact]
    public void UnchoosingTheChoice_KeepsIt()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var shape = (ChoiceToolOptionItem)_viewModel.Items[1];

        shape.Choices[1].IsChosen = false;

        Assert.True(shape.Choices[1].IsChosen);
    }

    [AvaloniaFact]
    public void TheBracketKeysOnTheCanvas_ShowOnTheToolbar()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var changed = new List<string?>();
        Size.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _canvas.ViewModel.ActiveTool.TextEntered("]");

        Assert.Equal(2, Size.Value);
        Assert.Equal([nameof(NumberToolOptionItem.Value)], changed);
    }

    [AvaloniaFact]
    public void TheBrushAndTheEraser_ShowTheirOwnValues()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Size.Value = 9;
        var brushItems = _viewModel.Items;

        _canvas.ViewModel.SelectTool(EraserTool.Id);
        Assert.NotSame(brushItems, _viewModel.Items);
        Assert.Equal(1, Size.Value);

        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Assert.Equal(9, Size.Value);
    }

    [AvaloniaFact]
    public void ReplacingTheDocument_KeepsTheItems()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var items = _viewModel.Items;
        var changes = new List<string?>();
        ((INotifyPropertyChanged)_viewModel).PropertyChanged += (_, e) => changes.Add(e.PropertyName);

        _canvas.ActiveDocument.Replace(new AskiiArt.Core.Editing.EditingSession(
            Document.Create(new CanvasSize(3, 3), "Layer", CharacterSet.Unicode)));

        Assert.Same(items, _viewModel.Items);
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void APerceptualBrush_IsFewerCellsTallThanWide_InTheDocumentsFont()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Size.Value = 10;

        var perceptual = _canvas.ViewModel.ActiveTool.Overlay.Footprint!;
        Assert.Equal(10, perceptual.Width);
        Assert.InRange(perceptual.Height, 3, 7);

        ((ChoiceToolOptionItem)_viewModel.Items[2]).Choices[1].IsChosen = true;
        var mathematical = _canvas.ViewModel.ActiveTool.Overlay.Footprint!;
        Assert.Equal((10, 10), (mathematical.Width, mathematical.Height));
    }

    private NumberToolOptionItem Size => (NumberToolOptionItem)_viewModel.Items[0];
}
