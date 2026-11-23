// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia;

namespace AskiiArt.App.Canvas;

/// <summary>
/// The canvas view gliding from where it is to where it settles (ADR 0072): the canvas's top-left corner moves from
/// <see cref="From"/> to <see cref="To"/> over <see cref="Duration"/>, quickly at first and slowing to a stop.
/// </summary>
internal readonly record struct ViewGlide(PixelPoint From, PixelPoint To, TimeSpan Start, TimeSpan Duration)
{
    /// <summary>True once the glide has reached <see cref="To"/> at <paramref name="now"/>.</summary>
    public bool IsOverAt(TimeSpan now) => now - Start >= Duration;

    /// <summary>Where the canvas's top-left corner is at <paramref name="now"/>.</summary>
    public PixelPoint OriginAt(TimeSpan now)
    {
        if (IsOverAt(now))
            return To;

        var progress = Math.Clamp((now - Start) / Duration, 0, 1);
        var eased = 1 - Math.Pow(1 - progress, 3);
        return new PixelPoint((int)Math.Round(From.X + (To.X - From.X) * eased),
            (int)Math.Round(From.Y + (To.Y - From.Y) * eased));
    }
}
