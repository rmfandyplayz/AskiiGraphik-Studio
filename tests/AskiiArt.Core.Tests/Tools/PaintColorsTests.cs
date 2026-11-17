// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools;

namespace AskiiArt.Core.Tests.Tools;

public sealed class PaintColorsTests
{
    [Fact]
    public void Set_ChangesBothColors_WithOneChange()
    {
        var colors = new PaintColors();
        var changes = 0;
        colors.Changed += (_, _) => changes++;

        colors.Set(CellColor.FromAnsi(AnsiColor.Red), CellColor.FromAnsi(AnsiColor.Blue));

        Assert.Equal(CellColor.FromAnsi(AnsiColor.Red), colors.Foreground);
        Assert.Equal(CellColor.FromAnsi(AnsiColor.Blue), colors.Background);
        Assert.Equal(1, changes);
    }

    private static readonly CellColor _red = TestCells.Ansi(AnsiColor.Red);
    private static readonly CellColor _orange = TestCells.Rgb(255, 136, 0);

    private readonly PaintColors _colors = new();
    private int _changes;

    public PaintColorsTests()
    {
        _colors.Changed += (_, _) => _changes++;
    }

    [Fact]
    public void BothColors_StartAsNone()
    {
        Assert.True(_colors.Foreground.IsUnset);
        Assert.True(_colors.Background.IsUnset);
    }

    [Fact]
    public void SettingAColor_RaisesChanged_AndSettingTheSameOneAgainDoesNot()
    {
        _colors.Foreground = _red;
        _colors.Foreground = _red;
        _colors.Background = _orange;

        Assert.Equal(_red, _colors.Foreground);
        Assert.Equal(_orange, _colors.Background);
        Assert.Equal(2, _changes);
    }

    [Fact]
    public void Swap_SwapsTheColors_AndRaisesChangedOnce()
    {
        _colors.Foreground = _red;
        _colors.Background = _orange;
        _changes = 0;

        _colors.Swap();

        Assert.Equal(_orange, _colors.Foreground);
        Assert.Equal(_red, _colors.Background);
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void Swap_WithTwoEqualColors_ChangesNothing()
    {
        _colors.Swap();

        Assert.Equal(0, _changes);
    }

    [Fact]
    public void Reset_SetsBothToNone()
    {
        _colors.Foreground = _red;
        _colors.Background = _orange;
        _changes = 0;

        _colors.Reset();

        Assert.True(_colors.Foreground.IsUnset);
        Assert.True(_colors.Background.IsUnset);
        Assert.Equal(1, _changes);
    }

    [Fact]
    public void TheColors_AreKeptAsChosen_AndConvertedForEachColorMode()
    {
        _colors.Foreground = _orange;
        _colors.Background = _red;

        Assert.Equal(_orange, _colors.ForegroundIn(ColorMode.Rgb));
        Assert.Equal(CellColor.FromAnsi(AnsiPalette.Vga.FindNearest(new RgbColor(255, 136, 0))),
            _colors.ForegroundIn(ColorMode.Ansi));
        Assert.True(_colors.ForegroundIn(ColorMode.Monochrome).IsUnset);
        Assert.Equal(CellColor.FromRgb(AnsiPalette.Vga[AnsiColor.Red]), _colors.BackgroundIn(ColorMode.Rgb));
        Assert.Equal(_red, _colors.BackgroundIn(ColorMode.Ansi));
        Assert.Equal(_orange, _colors.Foreground);
    }

    [Fact]
    public void TheTarget_StartsAsFG_AndRaisesTargetChangedOnlyWhenItChanges()
    {
        var targetChanges = 0;
        _colors.TargetChanged += (_, _) => targetChanges++;
        Assert.Equal(PaintColorTarget.Foreground, _colors.Target);

        _colors.Target = PaintColorTarget.Background;
        _colors.Target = PaintColorTarget.Background;

        Assert.Equal(PaintColorTarget.Background, _colors.Target);
        Assert.Equal(1, targetChanges);
        Assert.Equal(0, _changes);
    }

    [Fact]
    public void TheTargetColor_IsTheChosenOne()
    {
        _colors.Foreground = _orange;
        _colors.Background = _red;
        Assert.Equal(_orange, _colors.TargetColor);

        _colors.Target = PaintColorTarget.Background;
        Assert.Equal(_red, _colors.TargetColor);
        Assert.Equal(CellColor.FromRgb(AnsiPalette.Vga[AnsiColor.Red]), _colors.TargetColorIn(ColorMode.Rgb));

        _colors.TargetColor = CellColor.Unset;
        Assert.True(_colors.Background.IsUnset);
        Assert.Equal(_orange, _colors.Foreground);
    }
}
