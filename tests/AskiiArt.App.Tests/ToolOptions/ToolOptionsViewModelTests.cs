// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.ComponentModel;
using System.Text;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.App.ToolOptions;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Brush;
using AskiiArt.Core.Tools.Eraser;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Hand;
using AskiiArt.Core.Tools.Lines;
using AskiiArt.Core.Tools.RampStrokes;
using AskiiArt.Core.Tools.Selecting;
using AskiiArt.Core.Tools.Shapes;
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
    public void TheFGBrush_ShowsItsSizeShapeAndProportions_AtTheirDefaults()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);

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

        Assert.Equal(5, _canvas.ToolSettings.For(BrushTool.TipOptions).Get(BrushTipOptions.Size));
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
        Assert.Equal([nameof(NumberToolOptionItem.Value), nameof(NumberToolOptionItem.ValueText)], changed);
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

    [AvaloniaFact]
    public void TheBrush_AlsoShowsItsRampIntensitySoftnessAndBlendMode()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);

        Assert.Equal(["Size", "Shape", "Proportions", "Ramp", "Intensity", "Softness", "Blend mode"],
            _viewModel.Items.Select(item => item.Name));
        Assert.Equal(["100%", "50%"], _viewModel.Items.OfType<NumberToolOptionItem>().Skip(1)
            .Select(item => item.ValueText));
        Assert.Equal("1", Size.ValueText);
    }

    [AvaloniaFact]
    public void TheRamp_IsADropdown_ListingEachRampsCharacters()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);

        var ramp = Choice("Ramp");

        Assert.True(ramp.IsDropDown);
        Assert.Equal(BuiltInRamps.All.Select(builtIn => builtIn.Ramp.ToString()), ramp.Choices.Select(c => c.Detail));
        Assert.Equal(Strings.RampStandard, ramp.SelectedChoice?.Name);
        Assert.False(Choice("Shape").IsDropDown);
    }

    [AvaloniaFact]
    public void TheSelectionTools_ShowTheirFourModesAsButtons()
    {
        _canvas.ViewModel.SelectTool(LassoTool.Id);

        var mode = Choice(Strings.ToolOptionSelectionMode);

        Assert.False(mode.IsDropDown);
        Assert.Equal(
            [
                Strings.ToolOptionSelectionModeReplace, Strings.ToolOptionSelectionModeAdd,
                Strings.ToolOptionSelectionModeSubtract, Strings.ToolOptionSelectionModeIntersect
            ],
            mode.Choices.Select(choice => choice.Name));
        Assert.True(mode.Choices[0].IsChosen);
    }

    [AvaloniaFact]
    public void ChoosingFromTheDropdown_ChoosesIt()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var ramp = Choice("Ramp");

        ramp.SelectedChoice = ramp.Choices[^1];

        var settings = _canvas.ToolSettings.For(RampOptions.Group);
        Assert.Equal(BuiltInRamps.All[^1].Id, settings.Get(RampOptions.Ramp));
        Assert.Same(ramp.Choices[^1], ramp.SelectedChoice);
    }

    [AvaloniaFact]
    public void TheBlendModesStillToCome_AreListedButCantBeChosen()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var blendMode = Choice("Blend mode");
        var changes = 0;
        blendMode.PropertyChanged += (_, _) => changes++;

        blendMode.SelectedChoice = blendMode.Choices[1];

        Assert.Equal(["Blend", "Replace", "Darken"], blendMode.Choices.Select(choice => choice.Name));
        Assert.Equal([true, false, false], blendMode.Choices.Select(choice => choice.IsAvailable));
        Assert.Equal("Blend", blendMode.SelectedChoice?.Name);
        Assert.Equal(1, changes);
    }

    [AvaloniaFact]
    public void TheFGAndBGBrushes_ShareTheirItems()
    {
        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        var items = _viewModel.Items;

        _canvas.ViewModel.SelectTool(ColorBrushTool.BackgroundId);
        Assert.Same(items, _viewModel.Items);

        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Assert.NotSame(items, _viewModel.Items);
    }

    [AvaloniaFact]
    public void ARampAnAsciiDocumentCantHold_IsWarnedAbout_UntilItsChangedOrTheDocumentIs()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));
        Assert.False(_viewModel.HasWarning);

        _canvas.ToolSettings.For(RampOptions.Group).Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);
        Assert.Equal(Strings.ToolWarningRampDoesNotFit, _viewModel.Warning);

        _canvas.ViewModel.SelectTool(ColorBrushTool.ForegroundId);
        Assert.False(_viewModel.HasWarning);

        _canvas.ViewModel.SelectTool(BrushTool.Id);
        _canvas.Session.History.Undo();
        Assert.False(_viewModel.HasWarning);
    }

    [AvaloniaFact]
    public void ANewDocument_IsWarnedAboutToo()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        _canvas.ToolSettings.For(RampOptions.Group).Set(RampOptions.Ramp, BuiltInRamps.Blocks.Id);
        Assert.False(_viewModel.HasWarning);

        _canvas.ActiveDocument.Replace(new AskiiArt.Core.Editing.EditingSession(
            Document.Create(new CanvasSize(3, 3), "Layer", CharacterSet.Ascii)));
        Assert.True(_viewModel.HasWarning);

        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Unicode));
        Assert.False(_viewModel.HasWarning);
    }

    [AvaloniaFact]
    public void TheSoftEraser_ShowsTheErasersTip_TheRamp_ThenItsStrengthAndSoftness()
    {
        _canvas.ViewModel.SelectTool(SoftEraserTool.Id);

        Assert.Equal(["Size", "Shape", "Proportions", "Ramp", "Strength", "Softness"],
            _viewModel.Items.Select(item => item.Name));
        Assert.Equal(["100%", "50%"], _viewModel.Items.OfType<NumberToolOptionItem>().Skip(1)
            .Select(item => item.ValueText));
    }

    [AvaloniaFact]
    public void TheErasers_ShareTheirTip_AndTheSoftEraserSharesTheBrushsRamp()
    {
        _canvas.ViewModel.SelectTool(EraserTool.Id);
        Size.Value = 7;
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        Choice("Ramp").SelectedChoice = Choice("Ramp").Choices[^1];

        _canvas.ViewModel.SelectTool(SoftEraserTool.Id);
        Assert.Equal(7, Size.Value);
        Assert.Same(Choice("Ramp").Choices[^1], Choice("Ramp").SelectedChoice);

        _canvas.ViewModel.SelectTool(EraserTool.BackgroundId);
        Assert.Equal(["Size", "Shape", "Proportions"], _viewModel.Items.Select(item => item.Name));
        Assert.Equal(7, Size.Value);
    }

    [AvaloniaFact]
    public void TheBgEraser_IsWarnedAbout_InADocumentWithoutColors()
    {
        _canvas.ViewModel.SelectTool(EraserTool.BackgroundId);
        Assert.Equal(Strings.ToolWarningNoBackgrounds, _viewModel.Warning);

        _canvas.Session.History.Execute(new SetColorModeCommand(ColorMode.Ansi));
        Assert.False(_viewModel.HasWarning);

        _canvas.Session.History.Undo();
        Assert.True(_viewModel.HasWarning);

        _canvas.ViewModel.SelectTool(EraserTool.Id);
        Assert.False(_viewModel.HasWarning);
    }

    [AvaloniaFact]
    public void TheRampDropdown_FollowsTheUsersRamps()
    {
        _canvas.ViewModel.SelectTool(BrushTool.Id);
        var mine = _canvas.ToolSettings.Ramps.Add("Mine", Ramp.FromText("ab"));

        var last = Choice(Strings.ToolOptionRamp).Choices[^1];
        Assert.Equal(("Mine", "ab"), (last.Name, last.Detail));
        Assert.Equal(BuiltInRamps.All.Count + 1, Choice(Strings.ToolOptionRamp).Choices.Count);

        _canvas.ToolSettings.Ramps.Rename(mine.Id, "Renamed");
        _canvas.ToolSettings.Ramps.Change(mine.Id, Ramp.FromText("xyz"));
        last = Choice(Strings.ToolOptionRamp).Choices[^1];
        Assert.Equal(("Renamed", "xyz"), (last.Name, last.Detail));

        last.IsChosen = true;
        Assert.Equal(mine.Id, _canvas.ToolSettings.For(RampOptions.Group).Get(RampOptions.Ramp));

        _canvas.ToolSettings.Ramps.Delete(mine.Id);
        var choices = Choice(Strings.ToolOptionRamp).Choices;
        Assert.Equal(BuiltInRamps.All.Count, choices.Count);
        Assert.True(choices.Single(choice => choice.Name == Strings.RampStandard).IsChosen);
    }

    [AvaloniaFact]
    public void TheCharacterBrush_ShowsTheBrushesTip_ThenItsCharacter()
    {
        _canvas.ViewModel.SelectTool(CharacterBrushTool.Id);

        Assert.Equal(["Size", "Shape", "Proportions", Strings.ToolOptionCharacter],
            _viewModel.Items.Select(item => item.Name));
        var character = Assert.IsType<GlyphToolOptionItem>(_viewModel.Items[3]);
        Assert.Equal("#", character.GlyphText);
        Assert.Contains("0023", character.Tooltip, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void TheCharacterBox_ShowsTheCharacter_WhenItChanges()
    {
        _canvas.ViewModel.SelectTool(CharacterBrushTool.Id);
        var character = Assert.IsType<GlyphToolOptionItem>(_viewModel.Items[3]);
        var changed = new List<string?>();
        character.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        _canvas.ToolSettings.For(CharacterBrushOptions.Group)
            .Set(CharacterBrushOptions.Character, new Rune('@'));

        Assert.Equal("@", character.GlyphText);
        Assert.Contains(nameof(GlyphToolOptionItem.GlyphText), changed);
    }

    [AvaloniaFact]
    public void ClickingTheCharacterBox_ShowsTheCharactersWindow()
    {
        var shown = 0;
        using var viewModel = new ToolOptionsViewModel(_canvas.ViewModel, () => shown++);
        _canvas.ViewModel.SelectTool(CharacterBrushTool.Id);
        var character = Assert.IsType<GlyphToolOptionItem>(viewModel.Items[3]);

        character.ShowPickerCommand.Execute(null);

        Assert.Equal(1, shown);
        Assert.False(Assert.IsType<GlyphToolOptionItem>(_viewModel.Items[3]).ShowPickerCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public void ACharacterAnAsciiDocumentCantHold_IsWarnedAbout()
    {
        _canvas.ViewModel.SelectTool(CharacterBrushTool.Id);
        _canvas.ToolSettings.For(CharacterBrushOptions.Group)
            .Set(CharacterBrushOptions.Character, new Rune(0x2588));
        Assert.False(_viewModel.HasWarning);

        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));
        Assert.Equal(Strings.ToolWarningCharacterDoesNotFit, _viewModel.Warning);

        _canvas.ToolSettings.For(CharacterBrushOptions.Group)
            .Set(CharacterBrushOptions.Character, new Rune('#'));
        Assert.False(_viewModel.HasWarning);
    }

    [AvaloniaFact]
    public void TheRectangle_ShowsItsStyleFillRoundedCornersAndCharacter_AndTheEllipseAllButRoundedCorners()
    {
        _canvas.ViewModel.SelectTool(BoxShapeTool.RectangleId);
        Assert.Equal(["Style", "Include joins", "Fill", "Rounded corners", "Character"],
            _viewModel.Items.Select(item => item.Name));
        Assert.Equal(["None", "Character", "Background"], Choice("Fill").Choices.Select(choice => choice.Name));
        Assert.True(Choice("Fill").IsDropDown);

        Assert.Equal(
            ["Standard", "Standard by step", "Precise", "Box", "Heavy box", "Double box", "Single character"],
            Choice("Style").Choices.Select(choice => choice.Name));

        _canvas.ViewModel.SelectTool(BoxShapeTool.EllipseId);
        Assert.Equal(["Style", "Fill", "Character"], _viewModel.Items.Select(item => item.Name));
        Assert.Equal(4, Choice("Style").Choices.Count);
    }

    [AvaloniaFact]
    public void TheLineTool_ShowsItsStyleWithoutTheBoxStyles_IncludeJoinsAsAToggle_AndTheCharacter()
    {
        _canvas.ViewModel.SelectTool(LineTool.Id);

        Assert.Equal(["Style", "Include joins", "Character"], _viewModel.Items.Select(item => item.Name));
        Assert.Equal(["Standard", "Standard by step", "Precise", "Single character"],
            Choice("Style").Choices.Select(choice => choice.Name));
        var joins = Assert.IsType<ToggleToolOptionItem>(_viewModel.Items[1]);
        Assert.False(joins.IsOn);

        joins.IsOn = true;

        Assert.True(_canvas.ToolSettings.For(LineStyleOptions.JoinsGroup).Get(LineStyleOptions.IncludeJoins));
    }

    [AvaloniaFact]
    public void ABoxStyle_InAnAsciiDocument_IsWarnedAbout()
    {
        _canvas.ViewModel.SelectTool(BoxShapeTool.RectangleId);
        _canvas.ToolSettings.For(ShapeOptions.RectangleStyleGroup).Set(ShapeOptions.RectangleStyle, "heavyBox");
        Assert.False(_viewModel.HasWarning);

        _canvas.Session.History.Execute(new SetCharacterSetCommand(CharacterSet.Ascii));

        Assert.Equal(Strings.ToolWarningBoxLinesNeedUnicode, _viewModel.Warning);
    }

    [AvaloniaFact]
    public void TickingRoundedCorners_TurnsThemOn_AndTheBoxFollowsTheValue()
    {
        _canvas.ViewModel.SelectTool(BoxShapeTool.RectangleId);
        var corners = Assert.IsType<ToggleToolOptionItem>(_viewModel.Items[3]);
        var values = _canvas.ToolSettings.For(ShapeOptions.RectangleGroup);

        corners.IsOn = true;
        Assert.True(values.Get(ShapeOptions.RoundedCorners));

        var changes = new List<string?>();
        corners.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        values.Set(ShapeOptions.RoundedCorners, false);

        Assert.False(corners.IsOn);
        Assert.Contains(nameof(ToggleToolOptionItem.IsOn), changes);
    }

    [AvaloniaFact]
    public void FillingWithTheBackground_InAMonochromeDocument_IsWarnedAbout()
    {
        _canvas.ViewModel.SelectTool(BoxShapeTool.EllipseId);
        Assert.False(_viewModel.HasWarning);

        _canvas.ToolSettings.For(ShapeOptions.FillGroup).Set(ShapeOptions.Fill, "background");

        Assert.Equal(Strings.ToolWarningNoBackgrounds, _viewModel.Warning);
    }

    private NumberToolOptionItem Size => (NumberToolOptionItem)_viewModel.Items[0];

    private ChoiceToolOptionItem Choice(string name) =>
        _viewModel.Items.OfType<ChoiceToolOptionItem>().Single(item => item.Name == name);
}
