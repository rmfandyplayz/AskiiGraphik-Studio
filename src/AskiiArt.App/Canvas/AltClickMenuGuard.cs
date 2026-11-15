// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Input;
using Avalonia.Interactivity;

namespace AskiiArt.App.Canvas;

/// <summary>
/// Keeps an Alt+click on the canvas from opening the menu bar when Alt is let go (ADR 0043). On Windows and Linux,
/// Avalonia's access key handler opens the menu when Alt is pressed and released on its own. It knows Alt was used as
/// a modifier when another key went down in between, but not when there was a click. So an Alt+click reports a key
/// going down that is no key at all (<see cref="Key.None"/>, with no text): the handler takes it as it takes Alt+F,
/// and nothing else responds to it. A <c>MainWindowTests</c> test fails if Avalonia changes how it handles this.
/// </summary>
internal static class AltClickMenuGuard
{
    public static void TreatAltAsUsed(Interactive source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = source,
            Key = Key.None,
            KeyModifiers = KeyModifiers.Alt,
        });
    }
}
