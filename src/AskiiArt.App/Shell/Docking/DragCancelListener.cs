// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AskiiArt.App.Shell.Docking;

/// <summary>While a drag lasts, Esc or the app losing focus cancels it (docs/DOCKING.md §5).</summary>
/// <param name="host">Something in the window that the drag is in.</param>
/// <param name="cancel">Cancels the drag.</param>
internal sealed class DragCancelListener(Visual host, Action cancel)
{
    private TopLevel? _listeningTo;

    /// <summary>Starts listening, for a drag that's starting.</summary>
    public void Start()
    {
        _listeningTo = TopLevel.GetTopLevel(host);
        _listeningTo?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        if (_listeningTo is WindowBase window)
            window.Deactivated += OnWindowDeactivated;
    }

    /// <summary>Stops listening, for a drag that's over.</summary>
    public void Stop()
    {
        _listeningTo?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);

        if (_listeningTo is WindowBase window)
            window.Deactivated -= OnWindowDeactivated;

        _listeningTo = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        e.Handled = true;
        cancel();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => cancel();
}
