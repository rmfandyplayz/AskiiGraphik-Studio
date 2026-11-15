// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Actions;
using AskiiArt.App.Painting;
using AskiiArt.Core.Tools;

namespace AskiiArt.App.Tests.Painting;

public sealed class BrushActionsTests
{
    private readonly PaintColors _colors = new();
    private readonly AppAction _foreground;
    private readonly AppAction _background;

    public BrushActionsTests()
    {
        var actions = new BrushActions(_colors).CreateActions().ToDictionary(action => action.Id);
        _foreground = actions[BrushActionIds.ForegroundBrush];
        _background = actions[BrushActionIds.BackgroundBrush];
    }

    [Fact]
    public void TheFGBrush_IsChosenAtFirst()
    {
        Assert.True(_foreground.CheckedState!.IsChecked);
        Assert.False(_background.CheckedState!.IsChecked);
    }

    [Fact]
    public void ChoosingTheBGBrush_ChoosesTheBGColor_AndUnticksTheFGBrush()
    {
        var notified = 0;
        _foreground.CheckedState!.Changed += (_, _) => notified++;

        _background.Command.Execute(null);

        Assert.Equal(PaintColorTarget.Background, _colors.Target);
        Assert.True(_background.CheckedState!.IsChecked);
        Assert.False(_foreground.CheckedState.IsChecked);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void AChoiceMadeElsewhere_IsFollowed()
    {
        var notified = 0;
        _background.CheckedState!.Changed += (_, _) => notified++;

        _colors.Target = PaintColorTarget.Background;

        Assert.True(_background.CheckedState.IsChecked);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void ChoosingTheChosenBrushAgain_TellsItsButton()
    {
        var notified = 0;
        _foreground.CheckedState!.Changed += (_, _) => notified++;

        _foreground.Command.Execute(null);

        Assert.True(_foreground.CheckedState.IsChecked);
        Assert.Equal(1, notified);
    }
}
