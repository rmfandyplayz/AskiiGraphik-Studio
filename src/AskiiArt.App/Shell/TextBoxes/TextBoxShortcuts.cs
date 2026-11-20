// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace AskiiArt.App.Shell.TextBoxes;

/// <summary>An edit every text box makes with a shortcut of its own.</summary>
internal enum TextBoxEdit
{
    SelectAll,
    Undo,
    Redo,
    Copy,
    Cut,
    Paste
}

/// <summary>
/// The shortcuts a window's text boxes edit with on this platform (Select All, Undo and so on), and the commands that
/// make an app action with one of them edit a text box that has the keyboard instead (ADR 0057).
/// </summary>
internal sealed class TextBoxShortcuts
{
    private readonly TopLevel _window;
    private readonly Dictionary<KeyGesture, TextBoxEdit> _edits = [];
    private TextBox? _textBox;

    public TextBoxShortcuts(TopLevel window, PlatformHotkeyConfiguration? hotkeys)
    {
        _window = window;
        if (hotkeys is not null)
        {
            Add(hotkeys.SelectAll, TextBoxEdit.SelectAll);
            Add(hotkeys.Undo, TextBoxEdit.Undo);
            Add(hotkeys.Redo, TextBoxEdit.Redo);
            Add(hotkeys.Copy, TextBoxEdit.Copy);
            Add(hotkeys.Cut, TextBoxEdit.Cut);
            Add(hotkeys.Paste, TextBoxEdit.Paste);
        }

        window.AddHandler(InputElement.GotFocusEvent, (_, _) => OnFocusMoved(), handledEventsToo: true);
        window.AddHandler(InputElement.LostFocusEvent, (_, _) => OnFocusMoved(), handledEventsToo: true);
    }

    /// <summary>Raised when the keyboard moves within the window, which may be into or out of a text box.</summary>
    public event EventHandler? FocusMoved;

    /// <summary>
    /// The text box that has the keyboard, if one does. While a menu has the keyboard, it's the text box that had it
    /// before: the menu gives it back when it closes, so the menu's items edit that text box.
    /// </summary>
    public TextBox? ActiveTextBox =>
        _textBox is not null && TopLevel.GetTopLevel(_textBox) == _window ? _textBox : null;

    /// <summary>The shortcuts of the platform the app is running on.</summary>
    public static TextBoxShortcuts ForThisPlatform(TopLevel window) =>
        new(window, Application.Current?.PlatformSettings?.HotkeyConfiguration);

    /// <summary>
    /// The command for a shortcut or menu item that runs <paramref name="command"/>. If one of
    /// <paramref name="gestures"/> is a text box's own shortcut, it's wrapped so that it makes the text box's edit
    /// while a text box has the keyboard; otherwise it's <paramref name="command"/> itself.
    /// </summary>
    public ICommand CommandFor(ICommand command, IEnumerable<KeyGesture> gestures)
    {
        foreach (var gesture in gestures)
        {
            if (_edits.TryGetValue(gesture, out var edit))
                return new TextBoxFirstCommand(command, edit, this);
        }

        return command;
    }

    /// <summary>Makes <paramref name="edit"/> in <paramref name="textBox"/>.</summary>
    public static void Make(TextBoxEdit edit, TextBox textBox)
    {
        switch (edit)
        {
            case TextBoxEdit.SelectAll:
                textBox.SelectAll();
                break;
            case TextBoxEdit.Undo:
                TypingUndo.Undo(textBox);
                break;
            case TextBoxEdit.Redo:
                TypingUndo.Redo(textBox);
                break;
            case TextBoxEdit.Copy:
                textBox.Copy();
                break;
            case TextBoxEdit.Cut:
                textBox.Cut();
                break;
            case TextBoxEdit.Paste:
                textBox.Paste();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edit), edit, null);
        }
    }

    // Clicking a menu in the window's menu bar takes the keyboard from the text box until the menu closes, and focus
    // can be nowhere for a moment as it moves. Neither means the user has left the text box.
    private void OnFocusMoved()
    {
        var focused = _window.FocusManager.GetFocusedElement();
        if (focused is TextBox textBox)
            _textBox = textBox;
        else if (focused is not (null or MenuItem or MenuBase))
            _textBox = null;

        FocusMoved?.Invoke(this, EventArgs.Empty);
    }

    // The first edit listed for a key wins, as the text box's own key handling checks them in this order.
    private void Add(IEnumerable<KeyGesture> gestures, TextBoxEdit edit)
    {
        foreach (var gesture in gestures)
            _edits.TryAdd(gesture, edit);
    }
}
