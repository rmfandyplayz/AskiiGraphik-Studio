// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Docking;

/// <summary>What a panel tells the window, without having to know where it's hosted.</summary>
internal static class PanelEvents
{
    /// <summary>The panel has finished with the keyboard, so typing can go back to the canvas.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> DoneTypingEvent =
        RoutedEvent.Register<Avalonia.Controls.Control, RoutedEventArgs>("DoneTyping", RoutingStrategies.Bubble);
}
