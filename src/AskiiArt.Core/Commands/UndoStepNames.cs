// SPDX-License-Identifier: GPL-3.0-or-later
// Written with Claude Code (Anthropic).

namespace AskiiArt.Core.Commands;

/// <summary>
/// Keys that name undo steps, for <see cref="IDocumentCommand.StepNameKey"/>. Core holds no
/// user-visible text: each key is an entry in the app's string resources.
/// </summary>
public static class UndoStepNames
{
    /// <summary>A cell edit with nothing more specific to call it.</summary>
    public const string EditCells = "UndoStepEditCells";

    /// <summary>A burst of typing, including any erasing in it.</summary>
    public const string Typing = "UndoStepTyping";

    /// <summary>Resizing the canvas.</summary>
    public const string ResizeCanvas = "UndoStepResizeCanvas";

    /// <summary>Changing the document's font.</summary>
    public const string SetFont = "UndoStepSetFont";

    /// <summary>Switching between Unicode and ASCII only.</summary>
    public const string SetCharacterSet = "UndoStepSetCharacterSet";

    /// <summary>Switching between monochrome, ANSI and RGB colour.</summary>
    public const string SetColorMode = "UndoStepSetColorMode";

    /// <summary>Adding a layer.</summary>
    public const string AddLayer = "UndoStepAddLayer";

    /// <summary>Removing a layer.</summary>
    public const string RemoveLayer = "UndoStepRemoveLayer";

    /// <summary>Moving a layer in the stack.</summary>
    public const string MoveLayer = "UndoStepMoveLayer";

    /// <summary>Changing a layer's properties, with nothing more specific to call it.</summary>
    public const string SetLayerProperties = "UndoStepSetLayerProperties";

    /// <summary>Duplicating a layer.</summary>
    public const string DuplicateLayer = "UndoStepDuplicateLayer";

    /// <summary>Merging a layer into the one below it.</summary>
    public const string MergeLayerDown = "UndoStepMergeLayerDown";

    /// <summary>Renaming a layer.</summary>
    public const string RenameLayer = "UndoStepRenameLayer";

    /// <summary>Showing or hiding a layer.</summary>
    public const string ShowOrHideLayer = "UndoStepShowOrHideLayer";

    /// <summary>Locking or unlocking a layer.</summary>
    public const string LockOrUnlockLayer = "UndoStepLockOrUnlockLayer";

    /// <summary>Changing whether a layer's spaces are see-through.</summary>
    public const string SetSpaceHandling = "UndoStepSetSpaceHandling";

    /// <summary>A stroke of the eraser.</summary>
    public const string Erase = "UndoStepErase";

    /// <summary>A stroke of the brush.</summary>
    public const string Paint = "UndoStepPaint";

    /// <summary>Selecting a rectangle with Rectangle Select.</summary>
    public const string RectangleSelect = "UndoStepRectangleSelect";

    /// <summary>Selecting an ellipse with Ellipse Select.</summary>
    public const string EllipseSelect = "UndoStepEllipseSelect";

    /// <summary>Selecting a freehand shape with the Lasso.</summary>
    public const string Lasso = "UndoStepLasso";

    /// <summary>Selecting a shape of straight sides with the Polygonal Lasso.</summary>
    public const string PolygonalLasso = "UndoStepPolygonalLasso";

    /// <summary>Selecting the whole canvas.</summary>
    public const string SelectAll = "UndoStepSelectAll";

    /// <summary>Selecting nothing.</summary>
    public const string Deselect = "UndoStepDeselect";

    /// <summary>Selecting matching cells with the Magic Wand.</summary>
    public const string MagicWand = "UndoStepMagicWand";

    /// <summary>Selecting what wasn't selected, and nothing that was.</summary>
    public const string InvertSelection = "UndoStepInvertSelection";

    /// <summary>Growing the selection by a cell all round.</summary>
    public const string GrowSelection = "UndoStepGrowSelection";

    /// <summary>Shrinking the selection by a cell all round.</summary>
    public const string ShrinkSelection = "UndoStepShrinkSelection";

    /// <summary>Selecting the active layer's cells that hold a character.</summary>
    public const string SelectCellsWithGlyphs = "UndoStepSelectCellsWithGlyphs";

    /// <summary>Selecting the active layer's cells that have a background colour.</summary>
    public const string SelectCellsWithBackground = "UndoStepSelectCellsWithBackground";

    /// <summary>Emptying the active layer's selected cells.</summary>
    public const string DeleteSelectedCells = "UndoStepDeleteSelectedCells";

    /// <summary>
    /// One drag or run of arrow presses with the Move tool, moving the selected cells, a copy of them or a paste.
    /// </summary>
    public const string Move = "UndoStepMove";

    /// <summary>Cutting the selected cells: copying them, then emptying them.</summary>
    public const string Cut = "UndoStepCut";

    /// <summary>Pasting cells, whether they float or are typed in at the Text tool's caret.</summary>
    public const string Paste = "UndoStepPaste";

    /// <summary>A line drawn with the Line tool.</summary>
    public const string Line = "UndoStepLine";

    /// <summary>A curve drawn with the Curve tool.</summary>
    public const string Curve = "UndoStepCurve";

    /// <summary>A rectangle drawn with the Rectangle tool.</summary>
    public const string Rectangle = "UndoStepRectangle";

    /// <summary>An ellipse drawn with the Ellipse tool.</summary>
    public const string Ellipse = "UndoStepEllipse";
}
