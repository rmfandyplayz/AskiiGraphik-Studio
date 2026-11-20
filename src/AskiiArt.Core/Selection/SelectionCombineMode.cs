// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Selection;

/// <summary>How a selection tool's shape combines with the selection there is (ARCHITECTURE §10, ADR 0060).</summary>
public enum SelectionCombineMode
{
    /// <summary>The shape becomes the selection.</summary>
    Replace,

    /// <summary>The shape's cells are selected too.</summary>
    Add,

    /// <summary>The shape's cells are no longer selected.</summary>
    Subtract,

    /// <summary>Only the cells both the shape and the selection have stay selected.</summary>
    Intersect
}
