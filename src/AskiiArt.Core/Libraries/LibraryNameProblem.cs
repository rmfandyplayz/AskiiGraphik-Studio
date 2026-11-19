// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Libraries;

/// <summary>Why a name can't be a user ramp's or a user character set's (ADRs 0052, 0053).</summary>
public enum LibraryNameProblem
{
    /// <summary>Nothing: the name can be used.</summary>
    None,

    /// <summary>The name is empty or only spaces.</summary>
    Blank,

    /// <summary>Another item of the library has the name, or one that differs only in case.</summary>
    Taken
}
