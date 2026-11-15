// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;
using AskiiArt.App.Painting;
using AskiiArt.App.Resources;
using AskiiArt.App.Tests.Fakes;
using AskiiArt.Core.Commands;
using AskiiArt.Core.Documents;
using AskiiArt.Core.Editing;
using AskiiArt.Core.Tools;
using Avalonia.Media;

namespace AskiiArt.App.Tests.Painting;

/// <summary>What the Brush Options, Color and Swatches windows show and change (ADR 0042).</summary>
public sealed class PaintColorsViewModelTests : IDisposable
{
    private static readonly CellColor _red = CellColor.FromAnsi(AnsiColor.Red);
    private static readonly RgbColor _orange = new(255, 136, 0);

    private readonly TestCanvas _canvas = new(new CanvasSize(4, 1), colorMode: ColorMode.Rgb);
    private readonly PaintColorsViewModel _viewModel;

    public PaintColorsViewModelTests()
    {
        _viewModel = _canvas.CreatePaintColorsViewModel();
    }

    private PaintColors Colors => _canvas.Colors;

    public void Dispose() => _canvas.Dispose();

    [Fact]
    public void TheWindowsChangeTheFgColor_UntilTheBgButtonIsChosen()
    {
        Assert.True(_viewModel.IsForegroundTarget);
        Assert.Equal(Strings.PaintColorTargetForeground, _viewModel.TargetText);

        _viewModel.IsBackgroundTarget = true;
        _viewModel.IsForegroundTarget = false;

        Assert.Equal(PaintColorTarget.Background, _viewModel.Target);
        Assert.False(_viewModel.IsForegroundTarget);
        Assert.Equal(Strings.PaintColorTargetBackground, _viewModel.TargetText);
    }

    [Fact]
    public void ASwatch_SetsTheColorTheWindowsChange()
    {
        _viewModel.Target = PaintColorTarget.Background;

        _viewModel.Swatches[(int)AnsiColor.Red].Choose.Execute(null);

        Assert.Equal(_red, Colors.Background);
        Assert.True(Colors.Foreground.IsUnset);
        Assert.Equal(16, _viewModel.Swatches.Count);
        Assert.Equal(Strings.AnsiColorRed, _viewModel.Swatches[(int)AnsiColor.Red].Name);
    }

    [Fact]
    public void None_SetsTheColorTheWindowsChangeToNone()
    {
        Colors.Foreground = _red;

        _viewModel.ChooseNoneCommand.Execute(null);

        Assert.True(Colors.Foreground.IsUnset);
    }

    [Fact]
    public void PickingAColor_SetsItAsRgb_IgnoringAlpha()
    {
        _viewModel.PickColor(Color.FromArgb(128, 255, 136, 0));

        Assert.Equal(CellColor.FromRgb(_orange), Colors.Foreground);
        Assert.Equal(Color.FromRgb(255, 136, 0), _viewModel.PickerColor);
    }

    [Fact]
    public void ThePicker_KeepsItsLastColor_WhileTheColorIsNone()
    {
        _viewModel.PickColor(Color.FromRgb(255, 136, 0));

        Colors.Reset();

        Assert.Equal(Color.FromRgb(255, 136, 0), _viewModel.PickerColor);
    }

    [Fact]
    public void TheButtons_ShowTheColorsAsTheDocumentWouldHoldThem()
    {
        Colors.Foreground = CellColor.FromRgb(_orange);
        Colors.Background = _red;

        Assert.Equal(Describe(Strings.PaintColorForegroundFormat, "#FF8800"), _viewModel.ForegroundText);
        var redHex = AnsiPalette.Vga[AnsiColor.Red].ToString().ToUpperInvariant();
        Assert.Equal(Describe(Strings.PaintColorBackgroundFormat, redHex), _viewModel.BackgroundText);

        SetColorMode(ColorMode.Ansi);

        var nearest = AnsiPalette.Vga.FindNearest(_orange);
        Assert.Equal(Describe(Strings.PaintColorForegroundFormat, AnsiColorNames.Get(nearest)),
            _viewModel.ForegroundText);
        Assert.Equal(Describe(Strings.PaintColorBackgroundFormat, Strings.AnsiColorRed), _viewModel.BackgroundText);
        var shown = AnsiPalette.Vga[nearest];
        Assert.Equal(Color.FromRgb(shown.Red, shown.Green, shown.Blue),
            ((ISolidColorBrush)_viewModel.ForegroundBrush!).Color);
    }

    [Fact]
    public void TheTooltips_NameTheColors_AndFollowThem()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Colors.Foreground = _red;

        Assert.StartsWith(_viewModel.ForegroundText, _viewModel.ForegroundTip, StringComparison.Ordinal);
        Assert.StartsWith(_viewModel.BackgroundText, _viewModel.BackgroundTip, StringComparison.Ordinal);
        Assert.Contains(nameof(PaintColorsViewModel.ForegroundTip), changed);
    }

    [Fact]
    public void NoneShowsNoColor()
    {
        Assert.Null(_viewModel.ForegroundBrush);
        Assert.Equal(Describe(Strings.PaintColorForegroundFormat, Strings.PaintColorNone), _viewModel.ForegroundText);
    }

    [Fact]
    public void WhatTheWindowsOffer_FollowsTheColorMode_AndUndo()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        Assert.True(_viewModel.CanUsePicker);

        SetColorMode(ColorMode.Monochrome);

        Assert.True(_viewModel.IsMonochrome);
        Assert.False(_viewModel.CanChooseSwatches);
        Assert.False(_viewModel.CanUsePicker);
        Assert.Contains(nameof(PaintColorsViewModel.CanUsePicker), changed);

        _canvas.Session.History.Undo();

        Assert.True(_viewModel.CanUsePicker);
        Assert.True(_viewModel.CanChooseSwatches);
    }

    [Fact]
    public void ItFollowsANewDocument()
    {
        _canvas.ActiveDocument.Replace(new EditingSession(Document.Create(new CanvasSize(2, 2), "Layer",
            colorMode: ColorMode.Ansi)));

        Assert.False(_viewModel.CanUsePicker);
        Assert.True(_viewModel.CanChooseSwatches);

        SetColorMode(ColorMode.Rgb);

        Assert.True(_viewModel.CanUsePicker);
    }

    [Fact]
    public void Swapping_UpdatesBothButtons()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Colors.Foreground = _red;
        Colors.Swap();

        Assert.Contains(nameof(PaintColorsViewModel.ForegroundText), changed);
        Assert.Contains(nameof(PaintColorsViewModel.BackgroundBrush), changed);
        Assert.NotNull(_viewModel.BackgroundBrush);
        Assert.Null(_viewModel.ForegroundBrush);
    }

    [Fact]
    public void AChoiceMadeElsewhere_SuchAsByTheBrush_IsFollowed()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Colors.Target = PaintColorTarget.Background;

        Assert.True(_viewModel.IsBackgroundTarget);
        Assert.Contains(nameof(PaintColorsViewModel.IsBackgroundTarget), changed);
        Assert.Contains(nameof(PaintColorsViewModel.IsForegroundTarget), changed);
        Assert.Contains(nameof(PaintColorsViewModel.TargetText), changed);
        Assert.Contains(nameof(PaintColorsViewModel.PickerColor), changed);
    }

    private static string Describe(string format, string name) =>
        string.Format(CultureInfo.CurrentCulture, format, name);

    private void SetColorMode(ColorMode colorMode) =>
        _canvas.Session.History.Execute(new SetColorModeCommand(colorMode));
}
