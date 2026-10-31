// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Fonts;

/// <summary>Where a <see cref="LoadedFont"/> came from.</summary>
public enum FontOrigin
{
    /// <summary>One of the <see cref="BundledFonts"/>.</summary>
    Bundled = 0,

    /// <summary>A font installed on the user's computer.</summary>
    System = 1,

    /// <summary>A TTF or OTF file the user picked.</summary>
    File = 2
}
