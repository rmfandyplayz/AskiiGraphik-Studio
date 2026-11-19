// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text;

namespace AskiiArt.Core.Ramps;

/// <summary>What's wrong with some text as a ramp's glyphs (<see cref="Ramp.Check"/>).</summary>
public enum RampTextProblem
{
    /// <summary>Nothing: the text makes a ramp.</summary>
    None,

    /// <summary>There are no characters.</summary>
    Empty,

    /// <summary>There's a space, which can only be the zero level (<see cref="RampZeroLevel.Space"/>).</summary>
    Space,

    /// <summary>A character can't fill a cell, such as a tab or a line break.</summary>
    Unusable,

    /// <summary>A character is there more than once.</summary>
    Repeated
}

/// <summary>Whether some text can make a ramp, and if not, why.</summary>
/// <param name="Problem">The first thing wrong, or <see cref="RampTextProblem.None"/>.</param>
/// <param name="Glyph">The character it's about, for a problem with one character.</param>
public readonly record struct RampTextCheck(RampTextProblem Problem, Rune Glyph)
{
    /// <summary>Whether the text makes a ramp.</summary>
    public bool IsValid => Problem == RampTextProblem.None;
}
