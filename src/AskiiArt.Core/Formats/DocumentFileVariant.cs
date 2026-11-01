// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Formats;

/// <summary>The two ways a document file can store the same JSON (ARCHITECTURE §13, ADR 0021).</summary>
public enum DocumentFileVariant
{
    /// <summary>A text header and readable UTF-8 JSON, so the art shows in a text editor.</summary>
    Plain,

    /// <summary>Binary magic bytes and Brotli-compressed JSON, for smaller files.</summary>
    Compressed
}
