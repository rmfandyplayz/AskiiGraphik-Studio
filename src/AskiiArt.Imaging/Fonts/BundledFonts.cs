// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Imaging.Fonts;

/// <summary>The fonts that ship with the app (ARCHITECTURE §11).</summary>
public static class BundledFonts
{
    /// <summary>JetBrains Mono, the default font.</summary>
    public static BundledFont JetBrainsMono { get; } =
        new("JetBrains Mono", "JetBrainsMono", "JetBrainsMono-Regular.ttf", "OFL-1.1", "OFL.txt");

    /// <summary>Roboto Mono.</summary>
    public static BundledFont RobotoMono { get; } =
        new("Roboto Mono", "RobotoMono", "RobotoMono-Regular.ttf", "OFL-1.1", "OFL.txt");

    /// <summary>IBM Plex Mono.</summary>
    public static BundledFont IbmPlexMono { get; } =
        new("IBM Plex Mono", "IBMPlexMono", "IBMPlexMono-Regular.ttf", "OFL-1.1", "LICENSE.txt");

    /// <summary>Fira Code. Its ligatures never apply, because each cell is drawn as one glyph.</summary>
    public static BundledFont FiraCode { get; } =
        new("Fira Code", "FiraCode", "FiraCode-Regular.ttf", "OFL-1.1", "LICENSE.txt");

    /// <summary>The font new documents use.</summary>
    public static BundledFont Default => JetBrainsMono;

    /// <summary>Every bundled font, default first.</summary>
    public static IReadOnlyList<BundledFont> All { get; } = [JetBrainsMono, RobotoMono, IbmPlexMono, FiraCode];
}
