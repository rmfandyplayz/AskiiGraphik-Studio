// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tests.Tools.Footprints;

public sealed class BrushTipOptionsTests
{
    private readonly ToolOptionValues _values = new(BrushTipOptions.CreateGroup("test.brush"));

    [Fact]
    public void ATipStarts_OneCellWide_Circular_AndPerceptual()
    {
        Assert.Equal(new BrushTip(1, BrushShape.Circle, BrushProportions.Perceptual), BrushTipOptions.ReadTip(_values));
    }

    [Fact]
    public void TheChoices_AreReadIntoTheTip()
    {
        _values.Set(BrushTipOptions.Size, 7);
        _values.Set(BrushTipOptions.Shape, "square");
        _values.Set(BrushTipOptions.Proportions, "mathematical");

        Assert.Equal(new BrushTip(7, BrushShape.Square, BrushProportions.Mathematical),
            BrushTipOptions.ReadTip(_values));
    }

    [Fact]
    public void TheSizeKeys_StepByOne_AndStopAtTheEnds()
    {
        Assert.True(BrushTipOptions.TryUseSizeKey(_values, "["));
        Assert.Equal(1, _values.Get(BrushTipOptions.Size));

        Assert.True(BrushTipOptions.TryUseSizeKey(_values, "]"));
        Assert.Equal(2, _values.Get(BrushTipOptions.Size));

        _values.Set(BrushTipOptions.Size, 100);
        Assert.True(BrushTipOptions.TryUseSizeKey(_values, "]"));
        Assert.Equal(100, _values.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void OtherText_IsntASizeKey()
    {
        Assert.False(BrushTipOptions.TryUseSizeKey(_values, "a"));
        Assert.False(BrushTipOptions.TryUseSizeKey(_values, "[]"));
        Assert.Equal(1, _values.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void TheBrushAndTheEraser_KeepSeparateSizes()
    {
        var settings = new ToolSettings();
        var brush = settings.For(AskiiArt.Core.Tools.Brush.BrushTool.OptionGroup);
        var eraser = settings.For(AskiiArt.Core.Tools.Eraser.EraserTool.OptionGroup);

        brush.Set(BrushTipOptions.Size, 9);

        Assert.Equal(1, eraser.Get(BrushTipOptions.Size));
    }

    [Fact]
    public void ASource_KeepsItsFootprint_UntilTheTipOrTheCellShapeChanges()
    {
        var cellWidthToHeight = 0.5;
        var source = new BrushTipSource(_values, () => cellWidthToHeight);
        var first = source.Footprint;

        Assert.Same(first, source.Footprint);

        _values.Set(BrushTipOptions.Size, 4);
        var bigger = source.Footprint;
        Assert.Equal((4, 2), (bigger.Width, bigger.Height));

        cellWidthToHeight = 1;
        Assert.Equal((4, 4), (source.Footprint.Width, source.Footprint.Height));
    }
}
