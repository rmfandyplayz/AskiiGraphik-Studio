// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.TextBoxes;

/// <summary>
/// Gives a text box undo steps that group typing (<see cref="TypingHistory"/>, ADR 0058), in place of Avalonia's own,
/// which keeps ten. A style in App.axaml turns it on for every text box, and turns Avalonia's off.
/// </summary>
internal sealed class TypingUndo
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<TypingUndo, TextBox, bool>("IsEnabled");

    private static readonly AttachedProperty<TypingUndo?> _instanceProperty =
        AvaloniaProperty.RegisterAttached<TypingUndo, TextBox, TypingUndo?>("Instance");

    private readonly TextBox _textBox;
    private readonly TypingHistory _history;
    private bool _isShowingAStep;

    static TypingUndo() => IsEnabledProperty.Changed.AddClassHandler<TextBox>(OnIsEnabledChanged);

    private TypingUndo(TextBox textBox)
    {
        _textBox = textBox;
        _history = new TypingHistory(textBox.Text ?? string.Empty, TimeProvider.System);
    }

    public static bool GetIsEnabled(TextBox textBox) => textBox.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(TextBox textBox, bool value) => textBox.SetValue(IsEnabledProperty, value);

    /// <summary>Undoes the last step of typing in <paramref name="textBox"/>, if there is one.</summary>
    public static void Undo(TextBox textBox)
    {
        if (textBox.GetValue(_instanceProperty) is { } typingUndo)
            typingUndo.Show(typingUndo._history.Undo());
        else
            textBox.Undo();
    }

    /// <summary>Redoes the last step of typing undone in <paramref name="textBox"/>, if there is one.</summary>
    public static void Redo(TextBox textBox)
    {
        if (textBox.GetValue(_instanceProperty) is { } typingUndo)
            typingUndo.Show(typingUndo._history.Redo());
        else
            textBox.Redo();
    }

    private static void OnIsEnabledChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs change)
    {
        if (textBox.GetValue(_instanceProperty) is { } previous)
        {
            previous.Detach();
            textBox.ClearValue(_instanceProperty);
        }

        if (!change.GetNewValue<bool>())
            return;

        var typingUndo = new TypingUndo(textBox);
        typingUndo.Attach();
        textBox.SetValue(_instanceProperty, typingUndo);
    }

    private void Attach()
    {
        _textBox.PropertyChanged += OnPropertyChanged;
        _textBox.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void Detach()
    {
        _textBox.PropertyChanged -= OnPropertyChanged;
        _textBox.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    // The property rather than the TextChanged event, which Avalonia raises later, after the text has rendered.
    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _isShowingAStep)
            return;

        // Only what the user types makes a step. Text that arrives while the box doesn't have the keyboard, such as
        // the next ramp's characters, starts its history afresh.
        var text = _textBox.Text ?? string.Empty;
        if (_textBox is { IsFocused: true, IsReadOnly: false })
            _history.Record(text);
        else
            _history.Reset(text);
    }

    // Tunnelling, so it comes before the text box's own handling of the keys. In the main window, the Undo and Redo
    // shortcuts reach the text box through the app's actions instead (TextBoxFirstCommand).
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Application.Current?.PlatformSettings?.HotkeyConfiguration is not { } hotkeys)
            return;

        if (hotkeys.Undo.Any(gesture => gesture.Matches(e)))
        {
            Show(_history.Undo());
            e.Handled = true;
        }
        else if (hotkeys.Redo.Any(gesture => gesture.Matches(e)))
        {
            Show(_history.Redo());
            e.Handled = true;
        }
    }

    private void Show(TextBoxState? step)
    {
        if (step is not { } shown)
            return;

        _isShowingAStep = true;
        try
        {
            // The caret first: moving it clears the selection.
            _textBox.SetCurrentValue(TextBox.TextProperty, shown.Text);
            _textBox.SetCurrentValue(TextBox.CaretIndexProperty, shown.SelectionEnd);
            _textBox.SetCurrentValue(TextBox.SelectionStartProperty, shown.SelectionStart);
            _textBox.SetCurrentValue(TextBox.SelectionEndProperty, shown.SelectionEnd);
        }
        finally
        {
            _isShowingAStep = false;
        }
    }
}
