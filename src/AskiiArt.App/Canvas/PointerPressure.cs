// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using Avalonia.Input;

namespace AskiiArt.App.Canvas;

/// <summary>
/// How hard a pointer presses, for the tools (ADR 0054). Only a pen's pressure counts: Avalonia reports a made-up 0.5
/// for a mouse, which would paint everything at half strength, so a mouse or touch presses fully.
/// </summary>
internal static class PointerPressure
{
    /// <summary>
    /// What a mouse presses with while the Debug menu has it act as a pen, so pressure can be tried without a tablet.
    /// </summary>
    public const float MouseActingAsPen = 0.5f;

    /// <summary>The pressure a tool gets from a pointer of type <paramref name="type"/>.</summary>
    /// <param name="type">The kind of pointer.</param>
    /// <param name="reported">The pressure the pointer reported.</param>
    /// <param name="isMouseActingAsPen">Whether the Debug menu has the mouse act as a pen.</param>
    public static float Of(PointerType type, float reported, bool isMouseActingAsPen) => type switch
    {
        PointerType.Pen => float.IsNaN(reported) ? 1 : Math.Clamp(reported, 0, 1),
        PointerType.Mouse when isMouseActingAsPen => MouseActingAsPen,
        _ => 1
    };
}
