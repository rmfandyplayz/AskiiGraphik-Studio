// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;

namespace AskiiArt.Core.Tools;

/// <summary>Modifier keys held during tool input, the same on every platform.</summary>
[Flags]
public enum ToolModifiers
{
    /// <summary>No modifier keys.</summary>
    None = 0,

    /// <summary>Ctrl on Windows and Linux, Cmd on macOS.</summary>
    Command = 1,

    /// <summary>Shift.</summary>
    Shift = 2,

    /// <summary>Alt on Windows and Linux, Option on macOS.</summary>
    Alt = 4
}

/// <summary>
/// The key commands a tool can respond to. The App's keymap has already translated the platform's
/// keys into these (on a Mac, Cmd+Left arrives as <see cref="Home"/>), so tools never deal with
/// platform conventions. Typed characters arrive separately, as text.
/// </summary>
public enum ToolKey
{
    /// <summary>One cell left (the left arrow key).</summary>
    Left,

    /// <summary>One cell right.</summary>
    Right,

    /// <summary>One cell up.</summary>
    Up,

    /// <summary>One cell down.</summary>
    Down,

    /// <summary>A few cells left (Ctrl+Left on Windows and Linux, Option+Left on macOS).</summary>
    JumpLeft,

    /// <summary>A few cells right.</summary>
    JumpRight,

    /// <summary>A few cells up.</summary>
    JumpUp,

    /// <summary>A few cells down.</summary>
    JumpDown,

    /// <summary>Many cells left (Alt+Left on Windows and Linux, Cmd+Option+Left on macOS).</summary>
    LongJumpLeft,

    /// <summary>Many cells right.</summary>
    LongJumpRight,

    /// <summary>Many cells up.</summary>
    LongJumpUp,

    /// <summary>Many cells down.</summary>
    LongJumpDown,

    /// <summary>The start of the row (Home; also Cmd+Left on macOS).</summary>
    Home,

    /// <summary>The end of the row (End; also Cmd+Right on macOS).</summary>
    End,

    /// <summary>The top of the column (Ctrl+Home on Windows and Linux, Cmd+Up on macOS).</summary>
    Top,

    /// <summary>The bottom of the column (Ctrl+End on Windows and Linux, Cmd+Down on macOS).</summary>
    Bottom,

    /// <summary>Backspace (Delete on a Mac keyboard).</summary>
    Backspace,

    /// <summary>Delete, which erases forwards (Fn+Delete on a Mac laptop).</summary>
    Delete,

    /// <summary>Enter or Return.</summary>
    Enter,

    /// <summary>Esc, which cancels.</summary>
    Escape
}

/// <summary>
/// A pointer event in canvas terms (ARCHITECTURE §8.1). The App translates the UI framework's
/// events into these, so tools never see pixels.
/// </summary>
/// <param name="Cell">The cell under the pointer. It may lie outside the canvas during a drag.</param>
/// <param name="SubCellX">Where across the cell the pointer is, from 0 (left edge) to 1 (right edge).</param>
/// <param name="SubCellY">Where down the cell the pointer is, from 0 (top edge) to 1 (bottom edge).</param>
/// <param name="Modifiers">The modifier keys held.</param>
/// <param name="Pressure">Pen pressure from 0 to 1; 1 for a mouse. Reserved for drawing tablets.</param>
public readonly record struct ToolPointerInput(
    CellPoint Cell,
    double SubCellX,
    double SubCellY,
    ToolModifiers Modifiers,
    float Pressure = 1);

/// <summary>
/// What the canvas draws on top of the document for the active tool. It isn't part of the
/// document. Brush and shape previews join it in later milestones.
/// </summary>
/// <param name="Caret">The text caret's cell, or null when there's no caret.</param>
public sealed record ToolOverlay(CellPoint? Caret)
{
    /// <summary>Nothing to draw.</summary>
    public static ToolOverlay None { get; } = new(Caret: null);
}
