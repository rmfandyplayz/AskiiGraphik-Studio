// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Ramps;

/// <summary>A ramp the app ships with (ADR 0045).</summary>
/// <param name="Id">A stable id, for settings and files to refer to it by.</param>
/// <param name="NameKey">The App's resource key for its name, since Core has no UI text.</param>
/// <param name="Ramp">The ramp itself.</param>
public sealed record BuiltInRamp(string Id, string NameKey, Ramp Ramp);

/// <summary>
/// The ramps the app ships with, agreed with the owner in M3's planning (ADR 0045). Each paints an empty cell at
/// intensity 0. Standard keeps its classic order. The others were ordered by measuring the four bundled fonts and
/// averaging, so they suit every bundled font, the default (JetBrains Mono) best.
/// </summary>
public static class BuiltInRamps
{
    /// <summary>The classic short ramp, in printable ASCII.</summary>
    public static BuiltInRamp Standard { get; } =
        new("askiiart.ramp.standard", "RampStandard", Ramp.FromText(".:-=+*#%@"));

    /// <summary>
    /// Twenty letters and digits, a little finer than <see cref="Standard"/>. They're in order of darkness in
    /// JetBrains Mono, and nearly so in the other bundled fonts.
    /// </summary>
    public static BuiltInRamp StandardAlphanumerical { get; } =
        new("askiiart.ramp.standardAlphanumerical", "RampStandardAlphanumerical",
            Ramp.FromText("vcxYuoVhX9PdGD0RQBNW"));

    /// <summary>
    /// A long ramp in printable ASCII, for smooth gradients: the characters of the well-known 70-level ramp,
    /// reordered by measuring the bundled fonts.
    /// </summary>
    public static BuiltInRamp Fine { get; } =
        new("askiiart.ramp.fine", "RampFine",
            Ramp.FromText(@"`.'-,_:""~;!^<>|/\+()?vr*LcxJliY1t[z{]}unjofCIZahkXwUqdpbmO#%&80QB$M@W"));

    /// <summary>The shade and full block characters. Unicode only.</summary>
    public static BuiltInRamp Blocks { get; } =
        new("askiiart.ramp.blocks", "RampBlocks", Ramp.FromText("░▒▓█"));

    /// <summary>Braille patterns from one dot to all eight. Unicode only.</summary>
    public static BuiltInRamp Braille { get; } =
        new("askiiart.ramp.braille", "RampBraille",
            Ramp.FromText("⠁⠃⠇⡇⣇⣧⣷⣿"));

    /// <summary>Every built-in ramp, in the order the app lists them.</summary>
    public static IReadOnlyList<BuiltInRamp> All { get; } = [Standard, StandardAlphanumerical, Fine, Blocks, Braille];

    /// <summary>The ramp a new brush starts with.</summary>
    public static BuiltInRamp Default => Standard;
}
