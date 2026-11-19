// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;
using AskiiArt.Core.Libraries;

namespace AskiiArt.Core.Ramps;

/// <summary>
/// A file holding one ramp, to share it (ARCHITECTURE §7, ADR 0052): readable JSON naming the format and its version,
/// then the ramp's name, what its lightest level paints, and its characters, lightest first.
/// </summary>
public static class RampFile
{
    /// <summary>The extension ramp files have. It may change with the product's name, so it's kept here.</summary>
    public const string Extension = ".asciiramp";

    /// <summary>The newest format version, which this app writes.</summary>
    public const int CurrentVersion = 1;

    private const string FormatName = "askiiart.ramp";

    /// <summary>The text of a ramp file holding <paramref name="ramp"/>, named <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public static string Write(string name, Ramp ramp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(ramp);

        return LibraryJson.WriteFile(FormatName, CurrentVersion, json => RampJson.Write(json, name.Trim(), ramp));
    }

    /// <summary>Reads the ramp in <paramref name="text"/>, a ramp file's contents.</summary>
    /// <param name="text">The file's text.</param>
    /// <param name="ramp">The ramp and its name, when it could be read.</param>
    /// <param name="problem"><see cref="LibraryFileProblem.None"/>, or why it couldn't be read.</param>
    /// <returns>Whether it could be read.</returns>
    public static bool TryRead(string text, [NotNullWhen(true)] out NamedRamp? ramp, out LibraryFileProblem problem)
    {
        ramp = null;
        problem = LibraryJson.ReadFile(text, FormatName, CurrentVersion, out var json);
        if (problem != LibraryFileProblem.None)
            return false;

        if (!RampJson.TryRead(json, out ramp))
            problem = LibraryFileProblem.Damaged;

        return ramp is not null;
    }
}
