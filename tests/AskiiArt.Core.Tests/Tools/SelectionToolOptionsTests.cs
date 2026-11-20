// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Selection;
using AskiiArt.Core.Tools;
using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.Selecting;

namespace AskiiArt.Core.Tests.Tools;

public sealed class SelectionToolOptionsTests
{
    [Theory]
    [InlineData(ToolModifiers.None, SelectionCombineMode.Subtract)]
    [InlineData(ToolModifiers.Command, SelectionCombineMode.Subtract)]
    [InlineData(ToolModifiers.Shift, SelectionCombineMode.Add)]
    [InlineData(ToolModifiers.Alt, SelectionCombineMode.Subtract)]
    [InlineData(ToolModifiers.Shift | ToolModifiers.Alt, SelectionCombineMode.Intersect)]
    public void HeldKeys_PickTheMode_OverTheChosenOne(ToolModifiers modifiers, SelectionCombineMode expected)
    {
        Assert.Equal(expected, SelectionToolOptions.ModeFor(modifiers, SelectionCombineMode.Subtract));
    }

    [Theory]
    [InlineData("replace", SelectionCombineMode.Replace)]
    [InlineData("add", SelectionCombineMode.Add)]
    [InlineData("subtract", SelectionCombineMode.Subtract)]
    [InlineData("intersect", SelectionCombineMode.Intersect)]
    public void ReadMode_ReadsTheChosenMode(string value, SelectionCombineMode expected)
    {
        var values = new ToolOptionValues(SelectionToolOptions.Group);
        values.Set(SelectionToolOptions.Mode, value);

        Assert.Equal(expected, SelectionToolOptions.ReadMode(values));
    }

    [Fact]
    public void TheMode_StartsAsReplace_AndIsShownAsButtons()
    {
        var values = new ToolOptionValues(SelectionToolOptions.Group);

        Assert.Equal(SelectionCombineMode.Replace, SelectionToolOptions.ReadMode(values));
        Assert.True(SelectionToolOptions.Mode.IsShownAsButtons);
    }
}
