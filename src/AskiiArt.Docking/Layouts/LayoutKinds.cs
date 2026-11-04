// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Docking.Layouts;

/// <summary>
/// Which edge of the app window a side is on. Windows only dock left and right for now, but nothing outside
/// <see cref="Solving.LayoutSolver"/>'s one conversion to rectangles assumes that, so a horizontal dock could
/// be added here one day.
/// </summary>
public enum SideEdge
{
    /// <summary>The left edge.</summary>
    Left,

    /// <summary>The right edge.</summary>
    Right,
}

/// <summary>What a side holds. A side never holds both.</summary>
public enum SideKind
{
    /// <summary>A translucent stack, with margins and gaps.</summary>
    Widgets,

    /// <summary>Opaque columns of tabbed windows, with no margins or gaps.</summary>
    Dock,
}

/// <summary>One of the 8 places a toolbar can sit in the canvas region.</summary>
public enum ToolbarAnchor
{
    /// <summary>The top-left corner.</summary>
    TopLeft,

    /// <summary>The middle of the top edge.</summary>
    TopMiddle,

    /// <summary>The top-right corner.</summary>
    TopRight,

    /// <summary>The middle of the right edge.</summary>
    RightMiddle,

    /// <summary>The bottom-right corner.</summary>
    BottomRight,

    /// <summary>The middle of the bottom edge.</summary>
    BottomMiddle,

    /// <summary>The bottom-left corner.</summary>
    BottomLeft,

    /// <summary>The middle of the left edge.</summary>
    LeftMiddle,
}

/// <summary>Which way a toolbar's buttons run.</summary>
public enum ToolbarOrientation
{
    /// <summary>Left to right.</summary>
    Horizontal,

    /// <summary>Top to bottom.</summary>
    Vertical,
}

/// <summary>Facts about the anchors.</summary>
public static class ToolbarAnchors
{
    /// <summary>Every anchor.</summary>
    public static IReadOnlyList<ToolbarAnchor> All { get; } = Enum.GetValues<ToolbarAnchor>();

    /// <summary>
    /// The direction an edge-middle anchor forces, because a toolbar there follows its edge. Null for a corner,
    /// where the user chooses.
    /// </summary>
    public static ToolbarOrientation? RequiredOrientation(ToolbarAnchor anchor) => anchor switch
    {
        ToolbarAnchor.TopMiddle or ToolbarAnchor.BottomMiddle => ToolbarOrientation.Horizontal,
        ToolbarAnchor.LeftMiddle or ToolbarAnchor.RightMiddle => ToolbarOrientation.Vertical,
        _ => null,
    };
}
