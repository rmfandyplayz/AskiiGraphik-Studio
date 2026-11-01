// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json.Nodes;

namespace AskiiArt.Core.Formats;

/// <summary>
/// Turns a file's JSON from one format version into the next, so old files keep opening
/// (ARCHITECTURE §13, ADR 0021).
/// </summary>
internal interface IFormatMigration
{
    /// <summary>The version this migration reads. It leaves the JSON at <c>FromVersion + 1</c>.</summary>
    int FromVersion { get; }

    /// <summary>Changes <paramref name="root"/> in place.</summary>
    void Migrate(JsonObject root);
}

/// <summary>
/// The chain of migrations that brings any older file up to <see cref="DocumentFileFormat.CurrentVersion"/>.
/// </summary>
internal static class FormatMigrations
{
    /// <summary>
    /// Every migration, oldest first. Version 1 is the first format, so there are none yet. When
    /// the format changes, bump <see cref="DocumentFileFormat.CurrentVersion"/> and add the
    /// migration from the previous version here; a test fails until both are done.
    /// </summary>
    public static IReadOnlyList<IFormatMigration> All { get; } = [];

    /// <summary>Runs the migrations from <paramref name="fromVersion"/> up to <paramref name="toVersion"/>.</summary>
    /// <exception cref="InvalidOperationException">A version in between has no migration.</exception>
    public static void Apply(JsonObject root, int fromVersion, int toVersion,
        IReadOnlyList<IFormatMigration> migrations)
    {
        for (var version = fromVersion; version < toVersion; version++)
        {
            var from = version;
            var migration = migrations.SingleOrDefault(candidate => candidate.FromVersion == from)
                            ?? throw new InvalidOperationException($"There's no migration from format version {from}.");
            migration.Migrate(root);
        }
    }
}
