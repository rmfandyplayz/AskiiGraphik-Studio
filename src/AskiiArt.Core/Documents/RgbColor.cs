// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Globalization;

namespace AskiiArt.Core.Documents;

/// <summary>A 24-bit colour.</summary>
/// <param name="Red">The red channel, 0 to 255.</param>
/// <param name="Green">The green channel, 0 to 255.</param>
/// <param name="Blue">The blue channel, 0 to 255.</param>
public readonly record struct RgbColor(byte Red, byte Green, byte Blue)
{
    /// <summary>The colour as <c>#rrggbb</c>, in lower case.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"#{Red:x2}{Green:x2}{Blue:x2}");
}
