// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Ramps;

/// <summary>Ids of the ramp actions.</summary>
internal static class RampActionIds
{
    /// <summary>
    /// Debug builds only: replaces the document with one listing its font's characters from lightest to darkest, as
    /// measured, and painting each built-in ramp, to check glyph density (step 3.1).
    /// </summary>
    public const string OpenDensitySample = "askiiart.debug.openDensitySample";
}
