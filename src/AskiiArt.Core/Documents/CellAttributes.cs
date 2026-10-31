// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Text attributes of a cell, such as bold or underline. Reserved (ARCHITECTURE §5.3, §18): in v1
/// every cell has <see cref="None"/>.
/// </summary>
[Flags]
public enum CellAttributes : byte
{
    /// <summary>No attributes.</summary>
    None = 0
}
