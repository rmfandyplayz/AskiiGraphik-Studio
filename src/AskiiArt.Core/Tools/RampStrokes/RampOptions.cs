// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Ramps;
using AskiiArt.Core.Tools.Options;

namespace AskiiArt.Core.Tools.RampStrokes;

/// <summary>
/// The ramp the Brush paints and the Soft Eraser lightens with (ADR 0050). It's one option group of its own, which
/// both tools list, so choosing a ramp on either chooses it for both: erasing along another ramp than the one painted
/// with would jump between characters that don't match.
/// </summary>
public static class RampOptions
{
    /// <summary>The App's resource key for the warning shown while the ramp can't be used in the document.</summary>
    public const string RampDoesNotFitWarningKey = "ToolWarningRampDoesNotFit";

    /// <summary>The id of the user's ramps as a source of choices (<see cref="ToolSettings"/>).</summary>
    public const string UserRampsSourceId = "askiiart.userRamps";

    /// <summary>
    /// The ramp: one of the built-in ones (ADR 0045), then one of the user's (ADR 0052), with its characters shown
    /// beside its name.
    /// </summary>
    public static ChoiceToolOption Ramp { get; } = new("ramp", "ToolOptionRamp",
        [
            .. BuiltInRamps.All.Select(ramp =>
                new ToolOptionChoice(ramp.Id, ramp.NameKey) { Detail = ramp.Ramp.ToString() })
        ],
        BuiltInRamps.Default.Id)
    {
        MoreChoicesFrom = UserRampsSourceId
    };

    /// <summary>The group holding just the ramp.</summary>
    public static ToolOptionGroup Group { get; } = new("askiiart.ramp", [Ramp]);

    /// <summary>The chosen ramp, as <paramref name="settings"/> hold it.</summary>
    public static RampSource SourceIn(ToolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new RampSource(settings.For(Group), settings.Ramps);
    }

    /// <summary>
    /// The warning the Tool Options toolbar shows for <paramref name="document"/>, or null when the ramp can be used
    /// in it.
    /// </summary>
    public static string? WarningFor(ToolSettings settings, Document document) =>
        SourceIn(settings).FitsIn(document) ? null : RampDoesNotFitWarningKey;
}
