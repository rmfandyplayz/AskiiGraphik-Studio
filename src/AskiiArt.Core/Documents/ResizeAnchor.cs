// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Documents;

/// <summary>
/// The edge, corner or center of the canvas that stays put when it's resized (ARCHITECTURE §5.7).
/// </summary>
public enum ResizeAnchor
{
    /// <summary>The top-left corner stays put: cells are added or cropped at the right and bottom.</summary>
    TopLeft,

    /// <summary>The top edge stays put, centered horizontally.</summary>
    Top,

    /// <summary>The top-right corner stays put.</summary>
    TopRight,

    /// <summary>The left edge stays put, centered vertically.</summary>
    Left,

    /// <summary>The center stays put: cells are added or cropped evenly on every side.</summary>
    Center,

    /// <summary>The right edge stays put, centered vertically.</summary>
    Right,

    /// <summary>The bottom-left corner stays put.</summary>
    BottomLeft,

    /// <summary>The bottom edge stays put, centered horizontally.</summary>
    Bottom,

    /// <summary>The bottom-right corner stays put.</summary>
    BottomRight
}
