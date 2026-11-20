// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;

namespace AskiiArt.App.Tests.Shell.TextBoxes;

// A text box in a window of its own, with none of the main window's shortcuts, as in a dialog. The app's style gives
// every text box its typing undo.
public sealed class TypingUndoTests : IDisposable
{
    private readonly TextBox _textBox = new();
    private readonly Button _button = new();
    private readonly Window _window;

    public TypingUndoTests()
    {
        _window = new Window { Content = new StackPanel { Children = { _textBox, _button } } };
        _window.Show();
        _textBox.Focus();
        Dispatcher.UIThread.RunJobs();
    }

    public void Dispose() => _window.Close();

    [AvaloniaFact]
    public void TheUndoKey_UndoesARunOfTyping_InOneStep_AndTheRedoKeyBringsItBack()
    {
        Type("a", "b", "c");

        Press(Key.Z);
        Assert.Equal("", _textBox.Text);

        Press(Key.Y);
        Assert.Equal("abc", _textBox.Text);
        Assert.Equal(3, _textBox.CaretIndex);
    }

    [AvaloniaFact]
    public void TwelveSteps_CanAllBeUndone()
    {
        // Two characters at once is a step of its own, like a paste.
        for (var step = 0; step < 12; step++)
            Type("ab");

        for (var undo = 0; undo < 12; undo++)
            Press(Key.Z);

        Assert.Equal("", _textBox.Text);
    }

    [AvaloniaFact]
    public void TextThatArrives_WhileTheBoxDoesntHaveTheKeyboard_CantBeUndone()
    {
        Type("a");
        _button.Focus();
        _textBox.Text = "set by the app";
        _textBox.Focus();
        Dispatcher.UIThread.RunJobs();

        Press(Key.Z);

        Assert.Equal("set by the app", _textBox.Text);
    }

    [AvaloniaFact]
    public void Typing_CanStillBeUndone_AfterTheKeyboardLeavesAndComesBack()
    {
        Type("a", "b");
        _button.Focus();
        _textBox.Focus();
        Dispatcher.UIThread.RunJobs();

        Press(Key.Z);

        Assert.Equal("", _textBox.Text);
    }

    private void Type(params string[] characters)
    {
        foreach (var character in characters)
            _window.KeyTextInput(character);

        Dispatcher.UIThread.RunJobs();
    }

    private void Press(Key key)
    {
        _window.KeyPress(key, RawInputModifiers.Control, PhysicalKey.None, null);
        _window.KeyRelease(key, RawInputModifiers.Control, PhysicalKey.None, null);
        Dispatcher.UIThread.RunJobs();
    }
}
