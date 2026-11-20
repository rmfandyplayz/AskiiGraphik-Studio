// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Shell.TextBoxes;

/// <summary>
/// What a text box shows after an undo or redo: its text, and the selected range (equal ends for a caret).
/// </summary>
internal readonly record struct TextBoxState(string Text, int SelectionStart, int SelectionEnd);

/// <summary>
/// The undo steps of one text box (ADR 0058). Typing goes into one step until the user pauses, types somewhere else, or
/// switches between typing and deleting; deleting works the same way. Any other change, such as a paste, or typing
/// over a selection, is a step of its own.
/// </summary>
internal sealed class TypingHistory
{
    /// <summary>The most steps kept. Older ones are dropped.</summary>
    public const int MaximumSteps = 1000;

    private readonly TimeProvider _clock;

    // _texts[0] is the text before the first step, and _texts[i] the text after step i.
    private readonly List<string> _texts = [];
    private int _position;
    private TextChange? _lastChange;
    private long _lastChangeTimestamp;

    public TypingHistory(string text, TimeProvider clock)
    {
        _clock = clock;
        Reset(text);
    }

    /// <summary>A pause at least this long between two changes starts a new step.</summary>
    public static TimeSpan PauseBetweenSteps { get; } = TimeSpan.FromSeconds(1);

    public bool CanUndo => _position > 0;

    public bool CanRedo => _position < _texts.Count - 1;

    /// <summary>Forgets every step, for text the user didn't type, such as another ramp's characters.</summary>
    public void Reset(string text)
    {
        _texts.Clear();
        _texts.Add(text);
        _position = 0;
        _lastChange = null;
    }

    /// <summary>Records the text after the user changed it, adding it to the last step or starting a new one.</summary>
    public void Record(string text)
    {
        var current = _texts[_position];
        if (text == current)
            return;

        _texts.RemoveRange(_position + 1, _texts.Count - _position - 1);
        var change = TextChange.Between(current, text);
        var now = _clock.GetTimestamp();

        if (ContinuesTheLastStep(change, now))
            _texts[_position] = text;
        else
            StartAStep(text);

        _lastChange = change;
        _lastChangeTimestamp = now;
    }

    /// <summary>
    /// Goes back a step, or returns null when there's none. The characters the step had removed come back selected.
    /// </summary>
    public TextBoxState? Undo()
    {
        if (!CanUndo)
            return null;

        var after = _texts[_position];
        _position--;
        _lastChange = null;

        var before = _texts[_position];
        var restored = TextChange.Between(after, before);
        return new TextBoxState(before, restored.Position, restored.Position + restored.Inserted);
    }

    /// <summary>
    /// Goes forward a step, or returns null when there's none. The caret goes after what the step typed.
    /// </summary>
    public TextBoxState? Redo()
    {
        if (!CanRedo)
            return null;

        var before = _texts[_position];
        _position++;
        _lastChange = null;

        var after = _texts[_position];
        var redone = TextChange.Between(before, after);
        var caret = redone.Position + redone.Inserted;
        return new TextBoxState(after, caret, caret);
    }

    private bool ContinuesTheLastStep(TextChange change, long now)
    {
        if (_position == 0 || _lastChange is not { } last || !change.FollowsOn(last))
            return false;

        return _clock.GetElapsedTime(_lastChangeTimestamp, now) < PauseBetweenSteps;
    }

    private void StartAStep(string text)
    {
        _texts.Add(text);
        _position++;

        var excess = _texts.Count - 1 - MaximumSteps;
        if (excess <= 0)
            return;

        _texts.RemoveRange(0, excess);
        _position -= excess;
    }
}
