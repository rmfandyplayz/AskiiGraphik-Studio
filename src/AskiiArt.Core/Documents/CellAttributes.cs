// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// Text attributes of a cell, such as bold or underline. Reserved (ARCHITECTURE §5.3, §18): every
/// cell has <see cref="None"/>. The owner left attributes out of the base app in M2b, but the hook
/// stays so a plugin could add them (ADR 0036).
/// </summary>
[Flags]
public enum CellAttributes : byte
{
    /// <summary>No attributes.</summary>
    None = 0
}
