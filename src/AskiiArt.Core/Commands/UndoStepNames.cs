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
}
