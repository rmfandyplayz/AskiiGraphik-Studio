// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tools.Eraser;

/// <summary>
/// The Soft Eraser's own options (ADR 0050): how far it lightens and how soft its edge is. Its tip is the Eraser
/// group's, and its ramp is the one it shares with the Brush (<see cref="RampOptions"/>).
/// </summary>
public static class SoftEraserOptions
{
    /// <summary>
    /// How far one stroke lightens the middle of the tip, as a percentage of the way to empty. At 100% the middle
    /// empties.
    /// </summary>
    public static NumberToolOption Strength { get; } =
        new("strength", "ToolOptionStrength", 0, 100, 100) { Unit = ToolOptionUnit.Percent };

    /// <summary>How much of the tip's radius fades towards its edge, as a percentage.</summary>
    public static NumberToolOption Softness { get; } =
        new("softness", "ToolOptionSoftness", 0, 100, 50) { Unit = ToolOptionUnit.Percent };

    /// <summary>The Soft Eraser's own options, in the order the Tool Options toolbar shows them.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.eraser.soft", [Strength, Softness]);
}
