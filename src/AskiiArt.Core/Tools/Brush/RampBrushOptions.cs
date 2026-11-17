// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Tools.Options;
using AskiiArt.Core.Tools.RampStrokes;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The Brush's own options (ADR 0049): how dark it paints, how soft its edge is, and how it mixes with what's there.
/// Its tip is its group's, and its ramp is the one it shares with the Soft Eraser (<see cref="RampOptions"/>).
/// </summary>
public static class RampBrushOptions
{
    /// <summary>The value of <see cref="BlendMode"/>'s only mode so far.</summary>
    public const string BlendModeBlend = "blend";

    /// <summary>How dark the middle of the tip paints, as a percentage of the ramp's darkest level.</summary>
    public static NumberToolOption Intensity { get; } =
        new("intensity", "ToolOptionIntensity", 0, 100, 100) { Unit = ToolOptionUnit.Percent };

    /// <summary>How much of the tip's radius fades towards its edge, as a percentage.</summary>
    public static NumberToolOption Softness { get; } =
        new("softness", "ToolOptionSoftness", 0, 100, 50) { Unit = ToolOptionUnit.Percent };

    /// <summary>
    /// How the brush mixes with what's there. Only Blend paints so far: Replace and Darken are shown as coming, at
    /// the owner's request.
    /// </summary>
    public static ChoiceToolOption BlendMode { get; } = new("blendMode", "ToolOptionBlendMode",
        [
            new(BlendModeBlend, "ToolOptionBlendModeBlend"),
            new("replace", "ToolOptionBlendModeReplace") { IsAvailable = false },
            new("darken", "ToolOptionBlendModeDarken") { IsAvailable = false }
        ],
        BlendModeBlend);

    /// <summary>The Brush's own options, in the order the Tool Options toolbar shows them.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.brush.ramp", [Intensity, Softness, BlendMode]);
}
