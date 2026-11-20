// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Shell.TextBoxes;
using AskiiArt.App.Tests.Fakes;

namespace AskiiArt.App.Tests.Shell.TextBoxes;

public sealed class TypingHistoryTests
{
    private static readonly TimeSpan _quickly = TimeSpan.FromMilliseconds(100);

    private readonly FakeClock _clock = new();

    [Fact]
    public void TypingQuickly_IsOneStep()
    {
        var history = Type("", "a", "ab", "abc");

        Assert.Equal(new TextBoxState("", 0, 0), history.Undo());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void APauseOfASecond_StartsANewStep()
    {
        var history = Type("", "a", "ab");
        _clock.Advance(TypingHistory.PauseBetweenSteps);
        history.Record("abc");

        Assert.Equal("ab", history.Undo()?.Text);
        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void APauseJustShortOfASecond_KeepsTheStep()
    {
        var history = Type("", "a");
        _clock.Advance(TypingHistory.PauseBetweenSteps - TimeSpan.FromMilliseconds(10));
        history.Record("ab");

        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void TypingSomewhereElse_StartsANewStep()
    {
        var history = Type("", "a", "ab", "xab");

        Assert.Equal("ab", history.Undo()?.Text);
        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void SwitchingFromTypingToDeleting_StartsANewStep()
    {
        var history = Type("", "a", "ab", "a");

        Assert.Equal("ab", history.Undo()?.Text);
        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void Backspacing_IsOneStep_AndUndoingIt_SelectsWhatComesBack()
    {
        var history = Type("abc", "ab", "a", "");

        Assert.Equal(new TextBoxState("abc", 0, 3), history.Undo());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void DeletingForwards_IsOneStep()
    {
        var history = Type("abcd", "acd", "ad");

        Assert.Equal(new TextBoxState("abcd", 1, 3), history.Undo());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void APaste_IsAStepOfItsOwn_EvenStraightAfterTyping()
    {
        var history = Type("", "a", "ab", "abXYZ", "abXYZc");

        Assert.Equal("abXYZ", history.Undo()?.Text);
        Assert.Equal("ab", history.Undo()?.Text);
        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void TypingOverASelection_IsAStepOfItsOwn()
    {
        var history = Type("abc", "x");

        Assert.Equal(new TextBoxState("abc", 0, 3), history.Undo());
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void AnEmoji_CountsAsOneCharacter()
    {
        var history = Type("", "a", "a😀", "a😀b");

        Assert.Equal("", history.Undo()?.Text);
    }

    [Fact]
    public void Redo_BringsBackAStep_WithTheCaretAfterIt()
    {
        var history = Type("x", "ax", "abx");
        history.Undo();

        Assert.True(history.CanRedo);
        Assert.Equal(new TextBoxState("abx", 2, 2), history.Redo());
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void TypingAfterAnUndo_StartsANewStep_AndDropsTheRedoSteps()
    {
        var history = Type("", "a", "ab");
        history.Undo();

        _clock.Advance(_quickly);
        history.Record("c");
        _clock.Advance(_quickly);
        history.Record("cd");

        Assert.False(history.CanRedo);
        Assert.Equal("", history.Undo()?.Text);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void MoreThanTenSteps_CanAllBeUndone()
    {
        var history = new TypingHistory("", _clock);
        for (var step = 1; step <= 12; step++)
        {
            _clock.Advance(TypingHistory.PauseBetweenSteps);
            history.Record(new string('a', step));
        }

        var undone = 0;
        while (history.Undo() is not null)
            undone++;

        Assert.Equal(12, undone);
    }

    [Fact]
    public void TheOldestSteps_GoPastTheLimit()
    {
        var history = new TypingHistory("", _clock);
        for (var step = 1; step <= TypingHistory.MaximumSteps + 5; step++)
        {
            _clock.Advance(TypingHistory.PauseBetweenSteps);
            history.Record(new string('a', step));
        }

        var undone = 0;
        TextBoxState? oldest = null;
        while (history.Undo() is { } state)
        {
            undone++;
            oldest = state;
        }

        Assert.Equal(TypingHistory.MaximumSteps, undone);
        Assert.Equal(new string('a', 5), oldest?.Text);
    }

    [Fact]
    public void Reset_ForgetsEveryStep()
    {
        var history = Type("", "a", "ab");

        history.Reset("other");

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void RecordingTheSameText_ChangesNothing()
    {
        var history = new TypingHistory("abc", _clock);

        history.Record("abc");

        Assert.False(history.CanUndo);
    }

    // A history starting at the first text, which then becomes each of the others in turn, quickly.
    private TypingHistory Type(string start, params string[] texts)
    {
        var history = new TypingHistory(start, _clock);
        foreach (var text in texts)
        {
            _clock.Advance(_quickly);
            history.Record(text);
        }

        return history;
    }
}
