// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Reflection;

namespace AskiiArt.App.Hosting;

/// <summary>
/// The app's version, as shown to the user and written to the log.
/// </summary>
internal static class AppVersion
{
    private const int ShortCommitHashLength = 7;

    /// <summary>
    /// The full informational version, for example "0.1.0+fc89da6e…" when the SDK knows the
    /// git commit it built from.
    /// </summary>
    public static string Informational { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    /// <summary>
    /// A short form for the About dialog, for example "0.1.0 (fc89da6)". The commit hash tells
    /// a bug report exactly which build it came from.
    /// </summary>
    public static string ForDisplay { get; } = FormatForDisplay(Informational);

    public static string FormatForDisplay(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plusIndex < 0)
            return informationalVersion;

        var version = informationalVersion[..plusIndex];
        var commitHash = informationalVersion[(plusIndex + 1)..];
        if (commitHash.Length > ShortCommitHashLength)
            commitHash = commitHash[..ShortCommitHashLength];

        return $"{version} ({commitHash})";
    }
}
