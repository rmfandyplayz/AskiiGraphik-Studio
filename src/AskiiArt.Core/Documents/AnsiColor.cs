// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The 16 standard ANSI colours, numbered in the order of their escape codes: text colours 30–37
/// and 90–97, backgrounds 40–47 and 100–107. Which RGB values they look like is up to the
/// <see cref="AnsiPalette"/>.
/// </summary>
public enum AnsiColor : byte
{
    /// <summary>Black.</summary>
    Black = 0,

    /// <summary>Red.</summary>
    Red = 1,

    /// <summary>Green.</summary>
    Green = 2,

    /// <summary>Yellow, which many palettes show as brown.</summary>
    Yellow = 3,

    /// <summary>Blue.</summary>
    Blue = 4,

    /// <summary>Magenta.</summary>
    Magenta = 5,

    /// <summary>Cyan.</summary>
    Cyan = 6,

    /// <summary>White, which most palettes show as light grey.</summary>
    White = 7,

    /// <summary>Bright black, which most palettes show as dark grey.</summary>
    BrightBlack = 8,

    /// <summary>Bright red.</summary>
    BrightRed = 9,

    /// <summary>Bright green.</summary>
    BrightGreen = 10,

    /// <summary>Bright yellow.</summary>
    BrightYellow = 11,

    /// <summary>Bright blue.</summary>
    BrightBlue = 12,

    /// <summary>Bright magenta.</summary>
    BrightMagenta = 13,

    /// <summary>Bright cyan.</summary>
    BrightCyan = 14,

    /// <summary>Bright white.</summary>
    BrightWhite = 15
}
