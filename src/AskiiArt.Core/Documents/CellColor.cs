// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using System.Diagnostics.CodeAnalysis;

namespace AskiiArt.Core.Documents;

/// <summary>
/// A cell's foreground or background color. Reserved for the color update (ARCHITECTURE §18):
/// in v1 every color is <see cref="Unset"/>, and there is deliberately no way to make another.
/// </summary>
/// <remarks>
/// The struct holds no data yet. When color becomes real it gains a packed value, which grows
/// <see cref="Cell"/> but stays within its 16-byte target.
/// </remarks>
public readonly record struct CellColor
{
    /// <summary>No color: the cell uses the document's default colors.</summary>
    public static CellColor Unset => default;

    /// <summary>True when no color is set. Always true in v1.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Reserved per-color data (ARCHITECTURE §18).")]
    public bool IsUnset => true;
}
