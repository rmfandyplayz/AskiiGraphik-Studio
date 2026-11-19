// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Ramps;

/// <summary>Ids of the ramp actions.</summary>
internal static class RampActionIds
{
    /// <summary>Adds a new ramp in the Ramps window.</summary>
    public const string New = "ramp.new";

    /// <summary>Adds a copy of the ramp chosen in the Ramps window.</summary>
    public const string Duplicate = "ramp.duplicate";

    /// <summary>Deletes the ramp chosen in the Ramps window, once the user confirms.</summary>
    public const string Delete = "ramp.delete";

    /// <summary>Puts the chosen ramp's characters in order of darkness in the document's font.</summary>
    public const string SortByDarkness = "ramp.sortByDarkness";

    /// <summary>Adds the ramps in ramp files the user picks.</summary>
    public const string Import = "ramp.import";

    /// <summary>Saves the chosen ramp to a ramp file.</summary>
    public const string Export = "ramp.export";

    /// <summary>
    /// Debug builds only: replaces the document with one listing its font's characters from lightest to darkest, as
    /// measured, and painting each built-in ramp, to check glyph density (step 3.1).
    /// </summary>
    public const string OpenDensitySample = "askiiart.debug.openDensitySample";
}
