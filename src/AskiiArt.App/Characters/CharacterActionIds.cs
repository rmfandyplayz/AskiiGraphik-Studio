// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.App.Characters;

/// <summary>Ids of the Characters window's actions (ADR 0053).</summary>
internal static class CharacterActionIds
{
    /// <summary>Adds a copy of the character set chosen in the Characters window.</summary>
    public const string Duplicate = "characterSet.duplicate";

    /// <summary>Deletes the character set chosen in the Characters window, once the user confirms.</summary>
    public const string Delete = "characterSet.delete";

    /// <summary>Adds the character sets in character set files the user picks.</summary>
    public const string Import = "characterSet.import";

    /// <summary>Saves the chosen character set to a character set file.</summary>
    public const string Export = "characterSet.export";
}
