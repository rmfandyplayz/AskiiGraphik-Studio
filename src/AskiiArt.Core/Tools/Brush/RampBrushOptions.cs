// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.Brush;

/// <summary>
/// The Brush's own options (ADR 0049), besides the tip it shares with the rest of its group: the ramp it paints, how
/// dark it paints, how soft its edge is, and how it mixes with what's there.
/// </summary>
public static class RampBrushOptions
{
    /// <summary>The App's resource key for the warning shown while the ramp can't be painted in the document.</summary>
    public const string RampDoesNotFitWarningKey = "ToolWarningRampDoesNotFit";

    /// <summary>The value of <see cref="BlendMode"/>'s only mode so far.</summary>
    public const string BlendModeBlend = "blend";

    /// <summary>The ramp, one of the built-in ones (ADR 0045), with its characters shown beside its name.</summary>
    public static ChoiceToolOption Ramp { get; } = new("ramp", "ToolOptionRamp",
        [
            .. BuiltInRamps.All.Select(ramp =>
                new ToolOptionChoice(ramp.Id, ramp.NameKey) { Detail = ramp.Ramp.ToString() })
        ],
        BuiltInRamps.Default.Id);

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
    public static ToolOptionGroup Group { get; } = new("askiiart.brush.ramp", [Ramp, Intensity, Softness, BlendMode]);

    /// <summary>The ramp <paramref name="values"/> choose.</summary>
    public static BuiltInRamp ReadRamp(ToolOptionValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var id = values.Get(Ramp);
        return BuiltInRamps.All.First(ramp => ramp.Id == id);
    }

    /// <summary>
    /// Whether the ramp <paramref name="values"/> choose can be painted in <paramref name="document"/>: whether its
    /// character set holds every character of the ramp.
    /// </summary>
    public static bool CanPaintIn(ToolOptionValues values, Document document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ReadRamp(values).Ramp.FitsIn(document.CharacterSet);
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/>, or null when the Brush can paint
    /// in it.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return CanPaintIn(settings.For(Group), document) ? null : RampDoesNotFitWarningKey;
    }
}
