// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

using AskiiArt.Core.Documents;
using AskiiArt.Core.Tools.Footprints;
using AskiiArt.Core.Tools.Selecting;

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
/// <param name="Pressure">
/// Pen pressure from 0 to 1; 1 for a mouse or touch. The Brush and the Soft Eraser cover cells more strongly the
/// harder a pen presses (ADR 0054).
/// </param>
/// <param name="ClickCount">
/// When the button goes down, how many clicks in a row this is: 2 for the second click of a double-click.
/// </param>
/// <param name="MarkerReach">
/// How far the pointer can be from one of the tool's markers, such as the Polygonal Lasso's first corner, and still be
/// on it, at the current zoom. None by default, so nothing is on a marker.
/// </param>
/// <param name="CellWidthOnScreen">
/// How wide a cell is on screen at the current zoom, in logical pixels, for a tool whose drag works in screen
/// distances, such as the Magic Wand's tolerance drag. 0 by default, so such a drag changes nothing.
/// </param>
public readonly record struct ToolPointerInput(
    CellPoint Cell,
    double SubCellX,
    double SubCellY,
    ToolModifiers Modifiers,
    float Pressure = 1,
    int ClickCount = 1,
    ToolMarkerReach MarkerReach = default,
    double CellWidthOnScreen = 0)
{
    /// <summary>Where the pointer is on the canvas, in cells with fractions.</summary>
    public CanvasPoint Position => new(Cell.X + SubCellX, Cell.Y + SubCellY);
}

/// <summary>
/// What the canvas draws on top of the document for the active tool. It isn't part of the
/// document. Shape previews join it in later milestones.
/// </summary>
/// <param name="Caret">The text caret's cell, or null when there's no caret.</param>
/// <param name="Footprint">
/// The brush tip's footprint, which the canvas outlines under the pointer, or null for a tool without one.
/// </param>
/// <param name="SelectionPreview">
/// What a selection tool's gesture would do if it ended now, which the canvas shows in place of the session's
/// selection until the gesture ends (ADR 0061), or null when there's no such gesture.
/// </param>
/// <param name="Path">
/// A line through these points, such as the path a lasso has drawn so far, or null for none. It isn't closed.
/// </param>
/// <param name="ShowsPathClosing">
/// True to show faintly how the path would close: a line from its last point back to its first.
/// </param>
/// <param name="Markers">Dots to draw, such as the Polygonal Lasso's corners, or null for none.</param>
public sealed record ToolOverlay(
    CellPoint? Caret,
    BrushFootprint? Footprint = null,
    SelectionPreview? SelectionPreview = null,
    IReadOnlyList<CanvasPoint>? Path = null,
    bool ShowsPathClosing = false,
    IReadOnlyList<ToolMarker>? Markers = null)
{
    /// <summary>Nothing to draw.</summary>
    public static ToolOverlay None { get; } = new(Caret: null);
}

/// <summary>What a tool's marker is, which sets how the canvas draws it (<see cref="ToolMarker"/>).</summary>
public enum ToolMarkerKind
{
    /// <summary>A corner of a shape being drawn, such as one of the Polygonal Lasso's.</summary>
    Corner,

    /// <summary>A point that finishes the shape when clicked, such as the Polygonal Lasso's first corner.</summary>
    Finish,

    /// <summary>A finishing point with the pointer on it, so that a click now finishes the shape.</summary>
    FinishUnderPointer
}

/// <summary>
/// A dot the canvas draws for the active tool, at a point on the canvas in cells. Its size is the App's, and stays the
/// same on screen whatever the zoom (ADR 0061).
/// </summary>
/// <param name="Position">Where its middle is, in cells with fractions.</param>
/// <param name="Kind">What it is.</param>
public readonly record struct ToolMarker(CanvasPoint Position, ToolMarkerKind Kind);

/// <summary>
/// How far the pointer can be from a tool's marker and still be on it, in cells across and down: the marker's radius
/// on screen at the current zoom, which the canvas works out, since markers keep their size on screen (ADR 0061).
/// </summary>
/// <param name="Across">The radius across, in cells.</param>
/// <param name="Down">The radius down, in cells.</param>
public readonly record struct ToolMarkerReach(double Across, double Down)
{
    /// <summary>
    /// True when <paramref name="point"/> is on a marker at <paramref name="marker"/>. With no reach, no point is.
    /// </summary>
    public bool Covers(CanvasPoint marker, CanvasPoint point)
    {
        if (Across <= 0 || Down <= 0)
            return false;

        var across = (point.X - marker.X) / Across;
        var down = (point.Y - marker.Y) / Down;
        return across * across + down * down <= 1;
    }
}
