// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Canvas.Rendering;

/// <summary>
/// A running time that stands still while told to and then carries on from where it stopped, so hatching that stops
/// during a gesture doesn't jump when it moves again (ADR 0061). It starts running, from zero.
/// </summary>
internal sealed class PausableClock
{
    private TimeSpan _elapsed;
    private TimeSpan _lastRead;
    private bool _isRunning = true;

    /// <summary>
    /// How long the clock has run by <paramref name="now"/>, on another clock that never stops, and then whether it
    /// runs on from there.
    /// </summary>
    public TimeSpan Elapsed(TimeSpan now, bool keepsRunning)
    {
        if (now > _lastRead)
        {
            if (_isRunning)
                _elapsed += now - _lastRead;

            _lastRead = now;
        }

        _isRunning = keepsRunning;
        return _elapsed;
    }
}
