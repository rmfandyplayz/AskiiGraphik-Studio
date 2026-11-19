// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Libraries;

/// <summary>What's wrong with a file that was read as a ramp file or a character set file (ADRs 0052, 0053).</summary>
public enum LibraryFileProblem
{
    /// <summary>Nothing: it was read.</summary>
    None,

    /// <summary>It isn't that kind of file at all.</summary>
    WrongKind,

    /// <summary>It's from a newer version of the app, which this one can't read.</summary>
    NewerVersion,

    /// <summary>It's the right kind of file, but something in it can't be read.</summary>
    Damaged
}
