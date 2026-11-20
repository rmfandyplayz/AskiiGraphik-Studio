// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Keeps an Alt+click on the canvas, or Alt pressed during a drag on it, from opening the menu bar, or leaving its
/// letters underlined, when Alt is let go (ADR 0043). One belongs to each canvas.
/// </summary>
/// <remarks>
/// On Windows and Linux, Avalonia's access key handler underlines the menu bar's letters while Alt is down, and opens
/// the menu when Alt is pressed and released on its own. It knows Alt was used as a modifier when another key went
/// down in between, but not when there was a click. So an Alt+click reports a key going down that is no key at all
/// (<see cref="Key.None"/>, with no text): the handler takes it as it takes Alt+F, and nothing else responds to it.
/// The handler hides the underlines on a click, but Windows repeats a held key, and each repeat of Alt shows them
/// again, so they're hidden after each one until Alt goes up. <c>MainWindowTests</c> fails if Avalonia changes any of
/// this.
/// </remarks>
internal sealed class AltClickMenuGuard
{
    private bool _isAltHeldAfterClick;

    public void OnPointerPressed(Interactive source, KeyModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (modifiers.HasFlag(KeyModifiers.Alt))
            TellTheHandlerAltWasUsed(source);
    }

    // Alt pressed during a drag changes the drag, as a selection's Alt does, so it counts as an Alt+click does. The
    // access key handler has already seen the key: it listens on the way down to the canvas.
    public void OnKeyDown(Interactive source, Key key, bool isDragging)
    {
        if (!IsAlt(key))
            return;

        if (isDragging && !_isAltHeldAfterClick)
            TellTheHandlerAltWasUsed(source);

        if (_isAltHeldAfterClick)
            HideUnderlines(source);
    }

    public void OnKeyUp(Interactive source, Key key)
    {
        if (!_isAltHeldAfterClick || !IsAlt(key))
            return;

        _isAltHeldAfterClick = false;
        HideUnderlines(source);
    }

    // Alt may go up somewhere else, which leaves the underlines to the access key handler.
    public void OnLostFocus() => _isAltHeldAfterClick = false;

    private static bool IsAlt(Key key) => key is Key.LeftAlt or Key.RightAlt;

    private void TellTheHandlerAltWasUsed(Interactive source)
    {
        _isAltHeldAfterClick = true;
        source.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = source,
            Key = Key.None,
            KeyModifiers = KeyModifiers.Alt,
        });
    }

    private static void HideUnderlines(Interactive source)
    {
        ArgumentNullException.ThrowIfNull(source);
        TopLevel.GetTopLevel(source)?.SetValue(AccessText.ShowAccessKeyProperty, false);
    }
}
