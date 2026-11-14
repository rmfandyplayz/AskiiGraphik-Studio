// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Text.Json.Nodes;

namespace AskiiArt.Core.Formats;

/// <summary>
/// Version 2 added colour (ADR 0037): a colour mode for the document, and optional colour runs
/// for each layer. Every older file is monochrome, so only the mode has to be added.
/// </summary>
/// <remarks>
/// A migration describes one moment of the format, so it spells out version 2's names itself
/// rather than sharing the writer's, which may change later.
/// </remarks>
internal sealed class AddColorModeMigration : IFormatMigration
{
    /// <inheritdoc/>
    public int FromVersion => 1;

    /// <inheritdoc/>
    public void Migrate(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        root["colorMode"] = "monochrome";
    }
}
