// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.App.Resources;
using AskiiArt.Core.Documents;

namespace AskiiArt.App.Painting;

/// <summary>The localized names of the 16 ANSI colours.</summary>
internal static class AnsiColorNames
{
    public static string Get(AnsiColor color) => color switch
    {
        AnsiColor.Black => Strings.AnsiColorBlack,
        AnsiColor.Red => Strings.AnsiColorRed,
        AnsiColor.Green => Strings.AnsiColorGreen,
        AnsiColor.Yellow => Strings.AnsiColorYellow,
        AnsiColor.Blue => Strings.AnsiColorBlue,
        AnsiColor.Magenta => Strings.AnsiColorMagenta,
        AnsiColor.Cyan => Strings.AnsiColorCyan,
        AnsiColor.White => Strings.AnsiColorWhite,
        AnsiColor.BrightBlack => Strings.AnsiColorBrightBlack,
        AnsiColor.BrightRed => Strings.AnsiColorBrightRed,
        AnsiColor.BrightGreen => Strings.AnsiColorBrightGreen,
        AnsiColor.BrightYellow => Strings.AnsiColorBrightYellow,
        AnsiColor.BrightBlue => Strings.AnsiColorBrightBlue,
        AnsiColor.BrightMagenta => Strings.AnsiColorBrightMagenta,
        AnsiColor.BrightCyan => Strings.AnsiColorBrightCyan,
        AnsiColor.BrightWhite => Strings.AnsiColorBrightWhite,
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
    };
}
